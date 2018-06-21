namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;

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
            var payload = new byte[6 + Data.Length];
            payload[0] = StartCode1;
            payload[1] = StartCode2;
            Array.Copy(DeviceId, 0, payload, 2, DeviceId.Length);
            Array.Copy(Data, 0, payload, 2 + DeviceId.Length, Data.Length);
            var crc = payload.ComputeChecksum(0, payload.Length - 3).ToLittleEndianArray();
            Array.Copy(crc, 0, payload, payload.Length - 2, crc.Length);
            Payload = payload;
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
