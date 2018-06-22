namespace Unosquare.Sparkfun.FingerprintModule
{
    public abstract class PacketBase : MessageBase
    {
        public const int BasePacketLenght = 12;

        public int Parameter { get; protected set; }

        public bool HasDataPacket => DataPacket != null;

        internal DataPacket DataPacket { get; set; }
    }
}
