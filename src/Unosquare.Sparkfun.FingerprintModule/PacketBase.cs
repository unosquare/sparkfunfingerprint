namespace Unosquare.Sparkfun.FingerprintModule
{
    /// <summary>
    /// Base class for message packets (either command or response).
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.MessageBase" />
    public abstract class PacketBase : MessageBase
    {
        /// <summary>
        /// The base packet lenght.
        /// </summary>
        public const int BasePacketLenght = 12;

        /// <summary>
        /// Gets or sets the parameter.
        /// </summary>
        public int Parameter { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this instance has data packet.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has data packet; otherwise, <c>false</c>.
        /// </value>
        public bool HasDataPacket => DataPacket != null;

        /// <summary>
        /// Gets or sets the data packet.
        /// </summary>
        internal DataPacket DataPacket { get; set; }
    }
}
