namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;

    internal class DataPacket 
        : MessageBase
    {
        internal const byte DataPacketStartCode1 = 0x5A;
        internal const byte DataPacketStartCode2 = 0xA5;

        public DataPacket() 
            : base()
        {
            StartCode1 = DataPacketStartCode1;
            StartCode2 = DataPacketStartCode2;
        }

        public byte[] Data { get; protected set; }
    }

    internal class CommandDataPacket 
        : DataPacket
    {
        public CommandDataPacket(byte[] data)
        {
            Data = data;
            CreatePayload();
        }

        private void CreatePayload()
        {
            var payload = new List<byte>() { StartCode1, StartCode2 };
            payload.AddRange(DeviceId);
            payload.AddRange(Data);
            var crc = payload.ComputeChecksum().ToLittleEndianArray();
            payload.AddRange(crc);
            Payload = payload.ToArray();
        }
    }

    internal class ResponseDataPacket
        : DataPacket
    {
        public ResponseDataPacket(byte[] payload)
        {
            Payload = payload;

            // Extracting data
            var data = new byte[payload.Length - 6];
            Array.Copy(payload, 4, data, 0, data.Length);
            Data = data;

            IsCrcValid = payload.ValidateChecksum();
        }
        
        public bool IsCrcValid { get; private set; }
    }
}
