namespace Unosquare.Sparkfun.FingerprintScanner
{
    internal abstract class MessageBase
    {
        public MessageBase()
        {
            DeviceId = new byte[] { 01, 00 };
        }

        public byte StartCode1 { get; protected set; } = 0x55;

        public byte StartCode2 { get; protected set; } = 0xAA;

        public byte[] DeviceId { get; }

        public byte[] Payload { get; protected set; }
    }
}
