using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using GameFramework;
using GameFramework.Event;
using GameFramework.Network;
using ProtoBuf;
using ProtoBuf.Meta;
using UnityGameFramework.Runtime;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace StarForce
{
    
    public class NetworkChannelHelper : INetworkChannelHelper

    {
        private static System.Text.ASCIIEncoding asciiEncoding = new ASCIIEncoding();
        
        private readonly Dictionary<int, Type> m_ServerToClientPacketTypes = new Dictionary<int, Type>();
        private INetworkChannel m_NetworkChannel = null;

        /// <summary>
        /// 获取消息包头长度。
        /// </summary>
        public int PacketHeaderLength
        {
            get
            {
                return 10; //包头10个字节
            }
        }

        /// <summary>
        /// 初始化网络频道辅助器。
        /// </summary>
        /// <param name="networkChannel">网络频道。</param>
        public void Initialize (INetworkChannel networkChannel)
        {

            m_NetworkChannel = networkChannel;

            // 反射注册包和包处理函数。
            Type packetBaseType = typeof(SCPacketBase);
            Type packetHandlerBaseType = typeof(PacketHandlerBase);
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] types = assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                if (!types[i].IsClass || types[i].IsAbstract)
                {
                    continue;
                }

                if (types[i].BaseType == packetBaseType)
                {
                    PacketBase packetBase = (PacketBase)Activator.CreateInstance(types[i]);
                    Type packetType = GetServerToClientPacketType(packetBase.Id);
                    if (packetType != null)
                    {
                        Log.Warning("Already exist packet type '{0}', check '{1}' or '{2}'?.", packetBase.Id.ToString(), packetType.Name, packetBase.GetType().Name);
                        continue;
                    }

                    m_ServerToClientPacketTypes.Add(packetBase.Id, types[i]);
                }
                else if (types[i].BaseType == packetHandlerBaseType)
                {
                    IPacketHandler packetHandler = (IPacketHandler)Activator.CreateInstance(types[i]);
                    m_NetworkChannel.RegisterHandler(packetHandler);
                }
            }

            // 获取框架事件组件
            EventComponent Event
                = UnityGameFramework.Runtime.GameEntry.GetComponent<EventComponent>();

            Event.Subscribe(UnityGameFramework.Runtime.NetworkConnectedEventArgs.EventId, OnNetworkConnected);
            Event.Subscribe(UnityGameFramework.Runtime.NetworkClosedEventArgs.EventId, OnNetworkClosed);
            Event.Subscribe(UnityGameFramework.Runtime.NetworkMissHeartBeatEventArgs.EventId, OnNetworkMissHeartBeat);
            Event.Subscribe(UnityGameFramework.Runtime.NetworkErrorEventArgs.EventId, OnNetworkError);
            Event.Subscribe(UnityGameFramework.Runtime.NetworkCustomErrorEventArgs.EventId, OnNetworkCustomError);
        }

        /// <summary>
        /// 关闭并清理网络频道辅助器。
        /// </summary>
        public void Shutdown ()
        {
            // 获取框架事件组件
            EventComponent Event
                = UnityGameFramework.Runtime.GameEntry.GetComponent<EventComponent>();

            Event.Unsubscribe(UnityGameFramework.Runtime.NetworkConnectedEventArgs.EventId, OnNetworkConnected);
            Event.Unsubscribe(UnityGameFramework.Runtime.NetworkClosedEventArgs.EventId, OnNetworkClosed);
            Event.Unsubscribe(UnityGameFramework.Runtime.NetworkMissHeartBeatEventArgs.EventId, OnNetworkMissHeartBeat);
            Event.Unsubscribe(UnityGameFramework.Runtime.NetworkErrorEventArgs.EventId, OnNetworkError);
            Event.Unsubscribe(UnityGameFramework.Runtime.NetworkCustomErrorEventArgs.EventId, OnNetworkCustomError);

            m_NetworkChannel = null;
        }

        /// <summary>
        /// 发送心跳消息包。
        /// </summary>
        /// <returns>是否发送心跳消息包成功。</returns>
        public bool SendHeartBeat ()
        {
            return true;
        }

        /// <summary>
        /// 序列化消息包。
        /// </summary>
        /// <typeparam name="T">消息包类型。</typeparam>
        /// <param name="packet">要序列化的消息包。</param>
        public bool Serialize<T> (T packet, Stream destination) where T : Packet
        {
            PacketBase packetImpl = packet as PacketBase;
            if (packetImpl == null)
            {
                Log.Warning("Packet is invalid.");
                return false;
            }

            if (packetImpl.PacketType != PacketType.ClientToServer)
            {
                Log.Warning("Send packet invalid.");
                return false;
            }
            
    
            //协议格式
            //************************************
            // i2 2个字节的消息总长度
            // i4 4个字节的checkcode
            // i4 4个字节的cmd -> code
            // n个字节的包体 
            //************************************

            
            //先序列化包体 包体序列化直接是大端的（不知道为啥）
            destination.Position = 10;
            Serializer.Serialize(destination, packet);
            
            //计算数据总长度 说明在上面 （这里需要减去游标已经移动的10个字节）
            UInt16 len = (UInt16)(4 + 4  + destination.Length - 10);
            destination.Position = 0;
            //将大端字节顺序写入Stream
            byte[] bytes = BitConverter.GetBytes(len);
            Array.Reverse(bytes); 
            destination.Write(bytes, 0, bytes.Length);
            
            //然后写入checkcode 目前为0
            Int32 checkcode = 0;
            destination.Position = 2;
            bytes = BitConverter.GetBytes(checkcode);
            Array.Reverse(bytes); 
            destination.Write(bytes, 0, bytes.Length);
            
            //写入cmd  这个cmd是通过crc32计算得到
            //eg:  2432403469 = crc32(login.login_req)
            UInt32 cmd = 2432403469;
            destination.Position = 6;
            bytes = BitConverter.GetBytes(cmd);
            Array.Reverse(bytes); 
            destination.Write(bytes, 0, bytes.Length);
    
            //测试代码 打印下字节流
            StringBuilder sb = new StringBuilder();
            byte[] buffer = new byte[32];
            int bytesRead;
            destination.Position = 0;
            while ((bytesRead = destination.Read(buffer, 0, buffer.Length)) > 0)
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    sb.Append($"{buffer[i]:X2} "); // 以16进制格式打印每个字节
                }
            }
            Debug.Log(sb.ToString());
            //协议组装完毕 发送即可
            return true;
        }

        /// <summary>
        /// 反序列消息包头。
        /// </summary>
        /// <param name="source">要反序列化的来源流。</param>
        /// <param name="customErrorData">用户自定义错误数据。</param>
        /// <returns></returns>
        public IPacketHeader DeserializePacketHeader (Stream source, out object customErrorData)
        {
            // 注意：此函数并不在主线程调用！
            customErrorData = null;

            return Serializer.DeserializeWithLengthPrefix<SCPacketHeader>(source, PrefixStyle.Fixed32);
            // return (IPacketHeader)RuntimeTypeModel.Default.Deserialize(source, ReferencePool.Acquire<SCPacketHeader>(), typeof(SCPacketHeader));
        }

        /// <summary>
        /// 反序列化消息包。
        /// </summary>
        /// <param name="packetHeader">消息包头。</param>
        /// <param name="source">要反序列化的来源流。</param>
        /// <param name="customErrorData">用户自定义错误数据。</param>
        /// <returns>反序列化后的消息包。</returns>
        public Packet DeserializePacket (IPacketHeader packetHeader, Stream source, out object customErrorData)
        {
            // 注意：此函数并不在主线程调用！
            customErrorData = null;

            SCPacketHeader scPacketHeader = packetHeader as SCPacketHeader;
            if (scPacketHeader == null)
            {
                Log.Warning("Packet header is invalid.");
                return null;
            }

            Packet packet = null;
            if (scPacketHeader.IsValid)
            {
                Type packetType = GetServerToClientPacketType(scPacketHeader.Id);
                if (packetType != null)
                {
                    packet = (Packet)RuntimeTypeModel.Default.DeserializeWithLengthPrefix(
                        source, ReferencePool.Acquire(packetType), packetType, PrefixStyle.Fixed32, 0);
                }
                else
                {
                    Log.Warning("Can not deserialize packet for packet id '{0}'.", scPacketHeader.Id.ToString());
                }
            }
            else
            {
                Log.Warning("Packet header is invalid.");
            }

            ReferencePool.Release(scPacketHeader);

            return packet;
        }

        private Type GetServerToClientPacketType (int id)
        {
            Type type = null;
            if (m_ServerToClientPacketTypes.TryGetValue(id, out type))
            {
                return type;
            }

            return null;
        }

        private void OnNetworkConnected (object sender, GameEventArgs e)
        {
            UnityGameFramework.Runtime.NetworkConnectedEventArgs ne = (UnityGameFramework.Runtime.NetworkConnectedEventArgs)e;
            if (ne.NetworkChannel != m_NetworkChannel)
            {
                return;
            }

            Log.Info("Network channel '{0}' connected.", ne.NetworkChannel.Name);
        }

        private void OnNetworkClosed (object sender, GameEventArgs e)
        {
            UnityGameFramework.Runtime.NetworkClosedEventArgs ne = (UnityGameFramework.Runtime.NetworkClosedEventArgs)e;
            if (ne.NetworkChannel != m_NetworkChannel)
            {
                return;
            }

            Log.Info("Network channel '{0}' closed.", ne.NetworkChannel.Name);
        }

        private void OnNetworkMissHeartBeat (object sender, GameEventArgs e)
        {
            UnityGameFramework.Runtime.NetworkMissHeartBeatEventArgs ne = (UnityGameFramework.Runtime.NetworkMissHeartBeatEventArgs)e;
            if (ne.NetworkChannel != m_NetworkChannel)
            {
                return;
            }

            Log.Info("Network channel '{0}' miss heart beat '{1}' times.", ne.NetworkChannel.Name, ne.MissCount.ToString());

            if (ne.MissCount < 2)
            {
                return;
            }

            ne.NetworkChannel.Close();
        }

        private void OnNetworkError (object sender, GameEventArgs e)
        {
            UnityGameFramework.Runtime.NetworkErrorEventArgs ne = (UnityGameFramework.Runtime.NetworkErrorEventArgs)e;
            if (ne.NetworkChannel != m_NetworkChannel)
            {
                return;
            }

            Log.Info("Network channel '{0}' error, error code is '{1}', error message is '{2}'.", ne.NetworkChannel.Name, ne.ErrorCode.ToString(), ne.ErrorMessage);

            ne.NetworkChannel.Close();
        }

        private void OnNetworkCustomError (object sender, GameEventArgs e)
        {
            UnityGameFramework.Runtime.NetworkCustomErrorEventArgs ne = (UnityGameFramework.Runtime.NetworkCustomErrorEventArgs)e;
            if (ne.NetworkChannel != m_NetworkChannel)
            {
                return;
            }
        }
    }
}
