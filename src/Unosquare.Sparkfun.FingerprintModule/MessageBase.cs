namespace Unosquare.Sparkfun.FingerprintModule
{
    public abstract class MessageBase
    {
        internal const byte BaseStartCode1 = 0x55;
        internal const byte BaseStartCode2 = 0xAA;
        internal static readonly byte[] BaseDeviceId = { 01, 00 };

        protected MessageBase()
        {
            DeviceId = BaseDeviceId;
        }

        protected byte StartCode1 { get; set; } = BaseStartCode1;

        protected byte StartCode2 { get; set; } = BaseStartCode2;

        protected byte[] DeviceId { get; }

        protected internal byte[] Payload { get; set; }
    }
}
