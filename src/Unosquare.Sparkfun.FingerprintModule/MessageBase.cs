namespace Unosquare.Sparkfun.FingerprintModule
{
    /// <summary>
    /// Base class for any kind of message (command, response or data packet).
    /// </summary>
    public abstract class MessageBase
    {
        internal const byte BaseStartCode1 = 0x55;
        internal const byte BaseStartCode2 = 0xAA;
        internal static readonly byte[] BaseDeviceId = { 01, 00 };

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBase"/> class.
        /// </summary>
        protected MessageBase()
        {
            DeviceId = BaseDeviceId;
        }

        /// <summary>
        /// Gets or sets the byte payload (the byte array representation of the message).
        /// </summary>
        protected internal byte[] Payload { get; set; }

        /// <summary>
        /// Gets or sets the first synchronization byte.
        /// </summary>
        protected byte StartCode1 { get; set; } = BaseStartCode1;

        /// <summary>
        /// Gets or sets the second synchronization byte.
        /// </summary>
        protected byte StartCode2 { get; set; } = BaseStartCode2;

        /// <summary>
        /// Gets the device identifier.
        /// </summary>
        /// <remarks>For current devices, default DeviceId is 0x0001, always fixed.</remarks>
        protected byte[] DeviceId { get; }
    }
}
