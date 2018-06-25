namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class for response messages.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.PacketBase" />
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseBase"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
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

        /// <summary>
        /// Gets a value indicating whether this instance is successful.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is successful; otherwise, <c>false</c>.
        /// </value>
        public virtual bool IsSuccessful => IsCrcValid && Response == ResponseCode.Ack && (!HasDataPacket || ResponseDataPacket.IsCrcValid);

        /// <summary>
        /// Gets the error code.
        /// </summary>
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

        /// <summary>
        /// Gets the response data packet.
        /// </summary>
        internal ResponseDataPacket ResponseDataPacket => (ResponseDataPacket)DataPacket;

        /// <summary>
        /// Gets the response code.
        /// </summary>
        protected ResponseCode Response { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has a valid CRC.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has a valid CRC; otherwise, <c>false</c>.
        /// </value>
        protected bool IsCrcValid { get; }

        /// <summary>
        /// Gets a unsuccessful response.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <param name="errorCode">The error code for the unsuccessful response.</param>
        /// <returns>An unsuccessful response of type T.</returns>
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
