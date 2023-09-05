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

namespace StarForce
{

    // internal static class crc32
    // {
    //     [DllImport("Crc32Lib")]
    //     internal static extern void make_crc32_table();
    //
    //     [DllImport("Crc32Lib")]
    //     internal static extern uint make_crc(uint crc, string str, uint length);
    // }
    
    public class Hash
    {
        /// <summary>CRC32 value table.</summary>
        private static readonly uint[] CRC32Table =
        {
            0x00000000, 0x77073096, 0xee0e612c, 0x990951ba,  
            0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,  
            0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,  
            0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,  
            0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,  
            0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,  
            0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec,  
            0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,  
            0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,  
            0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,  
            0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940,  
            0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,  
            0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116,  
            0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,  
            0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,  
            0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,  
            0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a,  
            0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,  
            0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818,  
            0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,  
            0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,  
            0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,  
            0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c,  
            0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,  
            0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,  
            0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,  
            0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,  
            0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,  
            0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086,  
            0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,  
            0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4,  
            0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,  
            0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,  
            0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,  
            0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,  
            0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,  
            0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe,  
            0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,  
            0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,  
            0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,  
            0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252,  
            0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,  
            0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60,  
            0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,  
            0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,  
            0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,  
            0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04,  
            0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,  
            0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a,  
            0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,  
            0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,  
            0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,  
            0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e,  
            0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,  
            0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,  
            0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,  
            0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,  
            0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,  
            0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0,  
            0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,  
            0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6,  
            0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,  
            0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,  
            0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d  
        };

        /// <summary>Internal reference to ASCIEncoding object</summary>
        private static System.Text.ASCIIEncoding asciiEncoding;

        /// <summary>Calculate the 32-bit Cyclic Redundancy Check of a given byte array</summary>
        /// <param name="value">Byte array to CRC32.</param>
        /// <returns>Byte array containing the CRC32 value.</returns>
        public static byte[] CRC32(byte[] value)
        {
            uint crcVal = 0xffffffff;
            for (int i = 0; i < value.Length; i++)
            {
                crcVal = (crcVal >> 8) ^ CRC32Table[(crcVal & 0xff) ^ value[i]];
            }

            crcVal ^= 0xffffffff; // Toggle operation
            byte[] result = new byte[4];

            result[0] = (byte)(crcVal >> 24);
            result[1] = (byte)(crcVal >> 16);
            result[2] = (byte)(crcVal >> 8);
            result[3] = (byte)crcVal;

            return result;
        }

        /// <summary>Calculate the 32-bit Cyclic Redundancy Check of a given string</summary>
        /// <param name="value">String value to calculate CRC32 from</param>
        /// <param name="includeDashes">If set to true will include bashes between byte values.</param>
        /// <returns>String value containing the CRC32.</returns>
        public static string CRC32(string value, bool includeDashes = false)
        {
            asciiEncoding = new System.Text.ASCIIEncoding();

            string tmp = System.BitConverter.ToString(CRC32((byte[])asciiEncoding.GetBytes(value)));

            if (!includeDashes)
            {
                tmp = tmp.Replace("-", string.Empty);
            }

            return tmp;
        }
    }
    

    public class Crc32 : HashAlgorithm
    {
        public uint Crc32Value { get; private set; }

        public Crc32()
        {
            Initialize();
        }

        uint CalculateCRC32(string input)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
            this.ComputeHash(bytes);
            return Crc32Value;
        }


        public override void Initialize()
        {
            Crc32Value = 0xFFFFFFFF;
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            Crc32Value ^= 0xFFFFFFFF;

            for (int i = ibStart; i < cbSize; i++)
            {
                Crc32Value = (Crc32Value >> 8) ^ crc32Table[(Crc32Value ^ array[i]) & 0xFF];
            }

            Crc32Value ^= 0xFFFFFFFF;
        }

        protected override byte[] HashFinal()
        {
            byte[] hashBuffer = BitConverter.GetBytes(Crc32Value);
            Array.Reverse(hashBuffer);
            return hashBuffer;
        }
        
        private static readonly uint[] crc32Table = {
            0x00000000, 0x77073096, 0xee0e612c, 0x990951ba,  
            0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3,  
            0x0edb8832, 0x79dcb8a4, 0xe0d5e91e, 0x97d2d988,  
            0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91,  
            0x1db71064, 0x6ab020f2, 0xf3b97148, 0x84be41de,  
            0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7,  
            0x136c9856, 0x646ba8c0, 0xfd62f97a, 0x8a65c9ec,  
            0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5,  
            0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,  
            0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b,  
            0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940,  
            0x32d86ce3, 0x45df5c75, 0xdcd60dcf, 0xabd13d59,  
            0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116,  
            0x21b4f4b5, 0x56b3c423, 0xcfba9599, 0xb8bda50f,  
            0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924,  
            0x2f6f7c87, 0x58684c11, 0xc1611dab, 0xb6662d3d,  
            0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a,  
            0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,  
            0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818,  
            0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01,  
            0x6b6b51f4, 0x1c6c6162, 0x856530d8, 0xf262004e,  
            0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457,  
            0x65b0d9c6, 0x12b7e950, 0x8bbeb8ea, 0xfcb9887c,  
            0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65,  
            0x4db26158, 0x3ab551ce, 0xa3bc0074, 0xd4bb30e2,  
            0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb,  
            0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,  
            0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9,  
            0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086,  
            0x5768b525, 0x206f85b3, 0xb966d409, 0xce61e49f,  
            0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4,  
            0x59b33d17, 0x2eb40d81, 0xb7bd5c3b, 0xc0ba6cad,  
            0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a,  
            0xead54739, 0x9dd277af, 0x04db2615, 0x73dc1683,  
            0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8,  
            0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,  
            0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe,  
            0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7,  
            0xfed41b76, 0x89d32be0, 0x10da7a5a, 0x67dd4acc,  
            0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5,  
            0xd6d6a3e8, 0xa1d1937e, 0x38d8c2c4, 0x4fdff252,  
            0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b,  
            0xd80d2bda, 0xaf0a1b4c, 0x36034af6, 0x41047a60,  
            0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79,  
            0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,  
            0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f,  
            0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04,  
            0xc2d7ffa7, 0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d,  
            0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a,  
            0x9c0906a9, 0xeb0e363f, 0x72076785, 0x05005713,  
            0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38,  
            0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7, 0x0bdbdf21,  
            0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e,  
            0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,  
            0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c,  
            0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45,  
            0xa00ae278, 0xd70dd2ee, 0x4e048354, 0x3903b3c2,  
            0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db,  
            0xaed16a4a, 0xd9d65adc, 0x40df0b66, 0x37d83bf0,  
            0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9,  
            0xbdbdf21c, 0xcabac28a, 0x53b39330, 0x24b4a3a6,  
            0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf,  
            0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,  
            0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d  
        };
    }

    
    

    public class NetworkChannelHelper : INetworkChannelHelper

    {
        
        static uint CalculateCRC32(string input)
        {
            using (Crc32 crc32 = new Crc32())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
                crc32.ComputeHash(bytes);
                return crc32.Crc32Value;
            }
        }
        
        private readonly Dictionary<int, Type> m_ServerToClientPacketTypes = new Dictionary<int, Type>();
        private INetworkChannel m_NetworkChannel = null;

        /// <summary>
        /// 获取消息包头长度。
        /// </summary>
        public int PacketHeaderLength
        {
            get
            {
                return 10;
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
            
            //string s = "login.login_req";
            //Log.Warning(Hash.CRC32(s));
            
            // destination 是一个字节流 position 是游标位置
            // 我们的消息格式
            // i2 2个字节的消息总长度
            // i4 4个字节的checkcode
            // i4 4个字节的cmd -> code
            // 所以按照下面的示例  我们先要跳过 10字节

            // 先序列化消息内容
            destination.Position = 10;
            Serializer.SerializeWithLengthPrefix(destination, packet, PrefixStyle.Fixed32);

            //我们不需要 包头 id  全部自动组装

            //首先写入总长度
            var len = 4 + 4  + destination.Length;
            destination.Position = 0;
            Serializer.SerializeWithLengthPrefix(destination, len, PrefixStyle.Fixed32);

            //然后写入checkcode 目前为0
            var checkcode = 0;
            destination.Position = 2;
            Serializer.SerializeWithLengthPrefix(destination, checkcode, PrefixStyle.Fixed32);

            //最后写入cmd   cmd由crc32 转换协议名得到
            var cmd = int.Parse(Hash.CRC32("login.login_req"));
            destination.Position = 6;
            Serializer.SerializeWithLengthPrefix(destination, cmd, PrefixStyle.Fixed32);

            //协议组装完毕 发送即可

            /* 以下内容为木头本人做的改动,不知道是否有错误的地方(虽然它运行起来是正确的),希望大家能帮忙指正 */
            // 因为头部消息有8字节长度，所以先跳过8字节
            // destination.Position = 8;
            // Serializer.SerializeWithLengthPrefix(destination, packet, PrefixStyle.Fixed32);

            // 头部消息
            // CSPacketHeader packetHeader = ReferencePool.Acquire<CSPacketHeader>();
            // packetHeader.Id = packet.Id;
            // packetHeader.PacketLength = (int)destination.Length - 8; // 消息内容长度需要减去头部消息长度

            // destination.Position = 0;
            // Serializer.SerializeWithLengthPrefix(destination, packetHeader, PrefixStyle.Fixed32);

            // ReferencePool.Release(packetHeader);

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
