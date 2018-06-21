namespace Unosquare.Sparkfun.FingerprintModule
{
    public abstract class PacketBase : MessageBase
    {
        public int Paremeter { get; protected set; }

        public bool HasDataPacket => DataPacket != null;

        internal DataPacket DataPacket { get; set; }
    }
}
