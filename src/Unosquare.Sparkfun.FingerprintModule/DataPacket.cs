namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for data packets.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.MessageBase" />
    internal abstract class DataPacket 
        : MessageBase
    {
        internal const byte DataPacketStartCode1 = 0x5A;
        internal const byte DataPacketStartCode2 = 0xA5;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataPacket"/> class.
        /// </summary>
        protected DataPacket()
        {
            StartCode1 = DataPacketStartCode1;
            StartCode2 = DataPacketStartCode2;
        }

        /// <summary>
        /// Gets or sets the byte array representing the real data the packet contains.
        /// </summary>
        public byte[] Data { get; protected set; }
    }

    /// <summary>
    /// Represents a command data packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.DataPacket" />
    internal class CommandDataPacket 
        : DataPacket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandDataPacket"/> class.
        /// </summary>
        /// <param name="data">A byte array representing the real data the packet contains.</param>
        public CommandDataPacket(byte[] data)
        {
            Data = data;
            GeneratePayload();
        }

        /// <summary>
        /// Generates the payload for the packet.
        /// </summary>
        private void GeneratePayload()
        {
            var payload = new List<byte>() { StartCode1, StartCode2 };
            payload.AddRange(DeviceId);
            payload.AddRange(Data);
            var crc = payload.ComputeChecksum().ToLittleEndianArray();
            payload.AddRange(crc);
            Payload = payload.ToArray();
        }
    }

    /// <summary>
    /// Represents a response data packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.DataPacket" />
    internal class ResponseDataPacket
        : DataPacket
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseDataPacket"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the packet.</param>
        public ResponseDataPacket(byte[] payload)
        {
            Payload = payload;

            // Extracting data
            var data = new byte[payload.Length - 6];
            Array.Copy(payload, 4, data, 0, data.Length);
            Data = data;

            IsCrcValid = payload.ValidateChecksum();
        }

        /// <summary>
        /// Gets a value indicating whether this instance has a valid CRC.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has a valid CRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsCrcValid { get; }
    }
}
