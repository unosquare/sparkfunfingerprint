namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;

    public abstract class ResponseBase
        : PacketBase
    {
        internal static readonly Dictionary<CommandCode, int> ResponseDataLength = new Dictionary<CommandCode, int>
        {
            { CommandCode.Open, 24 },
            { CommandCode.Enroll3, 498 },
            { CommandCode.MakeTemplate, 498 },
            { CommandCode.GetImage, 52116 },
            { CommandCode.GetRawImage, 19200},
            { CommandCode.GetTemplate, 498 },
        };

        protected ResponseBase(byte[] payload)
        {
            Payload = payload;
            Parameter = payload.LittleEndianArrayToInt(4);
            Response = (ResponseCode)payload.LittleEndianArrayToUInt16(8);
            IsCrcValid = payload.ValidateChecksum(0, BasePacketLenght - 1);

            if (payload.Length <= BasePacketLenght) return;
            var datapacketPayload = new byte[payload.Length - BasePacketLenght];
            Array.Copy(payload, BasePacketLenght, datapacketPayload, 0, datapacketPayload.Length);

            DataPacket = new ResponseDataPacket(datapacketPayload);
        }

        protected ResponseCode Response { get; }

        public virtual bool IsSuccessful => IsCrcValid && Response == ResponseCode.Ack && (!HasDataPacket || ResponseDataPacket.IsCrcValid);

        public ErrorCode ErrorCode
        {
            get
            {
                if (IsSuccessful)
                    return ErrorCode.NoError;

                if (!IsCrcValid || (HasDataPacket && !ResponseDataPacket.IsCrcValid))
                    return ErrorCode.InvalidCheckSum;

                if (Parameter >= 0 && Parameter < 0x1001)
                    return ErrorCode.DuplicateFingerprint;

                return (ErrorCode)Parameter;
            }
        }

        protected bool IsCrcValid { get; }

        internal ResponseDataPacket ResponseDataPacket => (ResponseDataPacket)DataPacket;

        internal static T GetUnsuccessfulResponse<T>(ErrorCode errorCode)
            where T : ResponseBase
        {
            // TODO: General payload generation code
            var payload = new List<byte>() { BaseStartCode1, BaseStartCode2 };
            payload.AddRange(BaseDeviceId);
            payload.AddRange(((int)errorCode).ToLittleEndianArray());
            payload.AddRange(((ushort)ResponseCode.Nack).ToLittleEndianArray());
            var crc = payload.ComputeChecksum().ToLittleEndianArray();
            payload.AddRange(crc);
            
            return Activator.CreateInstance(typeof(T), payload.ToArray()) as T;
        }
    }
}
