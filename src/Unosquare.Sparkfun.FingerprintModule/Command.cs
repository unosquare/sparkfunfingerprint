namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represenst a command or request for fingerprint device.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.PacketBase" />
    public class Command
        : PacketBase
    {
        internal static readonly Dictionary<CommandCode, int> CommandDataLength = new Dictionary<CommandCode, int>
        {
            { CommandCode.VerifyTemplate, 498 },
            { CommandCode.IdentifyTemplate, 498 },
            { CommandCode.IdentifyTemplate2, 500 },
            { CommandCode.SetTemplate, 498 },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// </summary>
        /// <param name="commandCode">The command code.</param>
        /// <param name="parameter">The parameter.</param>
        protected Command(CommandCode commandCode, int parameter)
        {
            CommandCode = commandCode;
            Parameter = parameter;
            GeneratePayload();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Command"/> class.
        /// </summary>
        /// <param name="commandCode">The command code.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="data">A byte array representing the data for a <see cref="CommandDataPacket"/>.</param>
        protected Command(CommandCode commandCode, int parameter, byte[] data) 
            : this(commandCode, parameter)
        {
            DataPacket = new CommandDataPacket(data);
        }

        /// <summary>
        /// Gets the command code.
        /// </summary>
        public CommandCode CommandCode { get; }

        /// <summary>
        /// Creates a <see cref="Command"/> object with the specified command code.
        /// </summary>
        /// <param name="commandCode">The command code.</param>
        /// <returns>A <see cref="Command"/> object.</returns>
        internal static Command Create(CommandCode commandCode) =>
            Create(commandCode, 0);

        /// <summary>
        /// Creates a <see cref="Command"/> object with the specified command code and parameter.
        /// </summary>
        /// <param name="commandCode">The command code.</param>
        /// <param name="parameter">The parameter.</param>
        /// <returns>A <see cref="Command"/> object.</returns>
        internal static Command Create(CommandCode commandCode, int parameter) => 
            new Command(commandCode, parameter);

        /// <summary>
        /// Creates a <see cref="Command"/> object with the specified command code and parameter. Additionally creates a <see cref="CommandDataPacket"/> with the specific data.
        /// </summary>
        /// <param name="commandCode">The command code.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="data">A byte array representing the data for a <see cref="CommandDataPacket"/>.</param>
        /// <returns>A <see cref="Command"/> object containing a <see cref="CommandDataPacket"/>.</returns>
        /// <exception cref="ArgumentNullException">data</exception>
        /// <exception cref="ArgumentOutOfRangeException">data - Current data length does not match expected data length for the command.</exception>
        internal static Command Create(CommandCode commandCode, int parameter, byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            if (CommandDataLength.ContainsKey(commandCode) && data.Length != CommandDataLength[commandCode])
            {
                throw new ArgumentOutOfRangeException(nameof(data), "Current data length does not match expected data length for the command.");
            }

            return new Command(commandCode, parameter, data);
        }

        /// <summary>
        /// Generates the payload for the command packet.
        /// </summary>
        private void GeneratePayload()
        {
            var payload = new List<byte>() { BaseStartCode1, BaseStartCode2 };
            payload.AddRange(BaseDeviceId);
            payload.AddRange(Parameter.ToLittleEndianArray());
            payload.AddRange(((ushort)CommandCode).ToLittleEndianArray());
            var crc = payload.ComputeChecksum().ToLittleEndianArray();
            payload.AddRange(crc);
            Payload = payload.ToArray();
        }
    }
}
