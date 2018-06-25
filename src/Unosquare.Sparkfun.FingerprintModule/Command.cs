namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;

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

        protected Command(CommandCode commandCode, int parameter)
        {
            CommandCode = commandCode;
            Parameter = parameter;
            CreatePayload();
        }

        protected Command(CommandCode commandCode, int parameter, byte[] data) 
            : this(commandCode, parameter)
        {
            DataPacket = new CommandDataPacket(data);
        }

        public CommandCode CommandCode { get; }
        
        internal static Command Create(CommandCode commandCode) =>
            Create(commandCode, 0);

        internal static Command Create(CommandCode commandCode, int parameter) => 
            new Command(commandCode, parameter);

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

        private void CreatePayload()
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
