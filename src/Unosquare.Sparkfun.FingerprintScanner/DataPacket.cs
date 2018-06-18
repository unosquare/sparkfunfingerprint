namespace Unosquare.Sparkfun.FingerprintScanner
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class DataPacket 
        : MessageBase
    {
        public DataPacket() 
            : base()
        {
            StartCode1 = 0x5A;
            StartCode2 = 0xA5;
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

        }
    }
}
