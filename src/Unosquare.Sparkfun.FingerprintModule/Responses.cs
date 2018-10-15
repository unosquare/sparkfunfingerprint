namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Linq;

    /// <summary>
    /// Basic response packet, with no extra data.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    public sealed class BasicResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasicResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public BasicResponse(byte[] payload)
            : base(payload)
        {
        }
    }

    /// <summary>
    /// Initialization (open) response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    /// <remarks>Initialization response packet could have a <see cref="ResponseDataPacket"/> data packet with device extra info.</remarks>
    public sealed class InitializationResponse : ResponseBase
    {
        /// <summary>
        /// The no information label.
        /// </summary>
        public static string NoInfo = "No info available";

        /// <summary>
        /// Initializes a new instance of the <see cref="InitializationResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public InitializationResponse(byte[] payload) 
            : base(payload)
        {
            if (!HasDataPacket) return;

            FirmwareVersion = $"{ResponseDataPacket.Data[3]:X2}" +
                              $"{ResponseDataPacket.Data[2]:X2}" +
                              $"{ResponseDataPacket.Data[1]:X2}" + 
                              $"{ResponseDataPacket.Data[0]:X2}";
            IsoAreaMaxSize = ResponseDataPacket.Data.LittleEndianArrayToInt(4);
            RawSerialNumber = new byte[16];
            Array.Copy(ResponseDataPacket.Data, 8, RawSerialNumber, 0, RawSerialNumber.Length);

            SerialNumber = string.Join(string.Empty, RawSerialNumber.Take(8).Select(x => x.ToString("X2"))) + "-" +
                           string.Join(string.Empty, RawSerialNumber.Skip(8).Select(x => x.ToString("X2")));
        }

        /// <summary>
        /// Gets the device firmware version.
        /// </summary>
        public string FirmwareVersion { get; } = NoInfo;

        /// <summary>
        /// Gets the maximum size of the iso area.
        /// </summary>
        public int IsoAreaMaxSize { get; } = -1;

        /// <summary>
        /// Gets the device serial number.
        /// </summary>
        public string SerialNumber { get; } = NoInfo;

        /// <summary>
        /// Gets a value indicating whether this instance is successful.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is successful; otherwise, <c>false</c>.
        /// </value>
        public override bool IsSuccessful => base.IsSuccessful && (RawSerialNumber?.Any(x => x != 0) ?? false);

        /// <summary>
        /// Gets the raw byte array of the device serial number.
        /// </summary>
        private byte[] RawSerialNumber { get; }
    }

    /// <summary>
    /// Fast searching response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    /// <remarks>The device operates as removable CD drive. If another removable CD drive exists in the system, connection time maybe will be long.
    /// To prevent this, <see cref="FingerprintReader.FastDeviceSearching"/> command is used for fast searching of the device.
    /// </remarks>
    public sealed class FastSearchingResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FastSearchingResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public FastSearchingResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets a value indicating whether this instance is successful.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is successful; otherwise, <c>false</c>.
        /// </value>
        public override bool IsSuccessful => base.IsSuccessful && Parameter == 0x55;
    }

    /// <summary>
    /// Count enrolled fingerprint response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    public sealed class CountEnrolledFingerprintResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CountEnrolledFingerprintResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public CountEnrolledFingerprintResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets the number of fingerprints registered in the device's database.
        /// </summary>
        public int EnrolledFingerprints => IsSuccessful ? Parameter : 0;
    }

    /// <summary>
    /// Check enrollment response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    public sealed class CheckEnrollmentResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckEnrollmentResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public CheckEnrollmentResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets a value indicating whether an id is enrolled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the id is enrolled; otherwise, <c>false</c>.
        /// </value>
        public bool IsEnrolled => IsSuccessful;
    }

    /// <summary>
    /// Enrollment response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    /// <remarks>Enrollment response packet could have a <see cref="ResponseDataPacket"/> data packet with the generated fingerprint template.</remarks>
    public sealed class EnrollmentResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnrollmentResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public EnrollmentResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets the fingerprint template.
        /// </summary>
        public byte[] Template => HasDataPacket ? (byte[])ResponseDataPacket.Data.Clone() : new byte[] { };
    }

    /// <summary>
    /// Check fingerprint pressing response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    public sealed class CheckFingerPressingResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CheckFingerPressingResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public CheckFingerPressingResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets a value indicating whether there is a finger pressing in the sensor.
        /// </summary>
        /// <value>
        ///   <c>true</c> if there is a finger pressing in the sensor; otherwise, <c>false</c>.
        /// </value>
        public bool IsPressed => Parameter == 0;
    }

    /// <summary>
    /// Match 1 to N response packet. Contains the User Id.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    public sealed class MatchOneToNResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MatchOneToNResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public MatchOneToNResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets the user identifier.
        /// </summary>
        public int UserId => IsSuccessful ? Parameter : -1;
    }

    /// <summary>
    /// Template response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    /// <remarks><see cref="TemplateResponse"/> is used for any command that returns a <see cref="ResponseDataPacket"/> with a fingerprint template .</remarks>
    public sealed class TemplateResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public TemplateResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets the fingerprint template.
        /// </summary>
        public byte[] Template => (byte[])ResponseDataPacket.Data.Clone();
    }

    /// <summary>
    /// Get fingerprint image response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    public sealed class GetFingerprintImageResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetFingerprintImageResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public GetFingerprintImageResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets the byte array representing the fingerprint image.
        /// </summary>
        public byte[] Image => (byte[])ResponseDataPacket.Data.Clone();
    }

    /// <summary>
    /// Get raw image response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    public sealed class GetRawImageResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetRawImageResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public GetRawImageResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets the byte array representing the fingerprint raw image.
        /// </summary>
        public byte[] Image => (byte[])ResponseDataPacket.Data.Clone();
    }

    /// <summary>
    /// Get security level response packet.
    /// </summary>
    /// <seealso cref="Unosquare.Sparkfun.FingerprintModule.ResponseBase" />
    public sealed class GetSecurityLevelResponse : ResponseBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetSecurityLevelResponse"/> class.
        /// </summary>
        /// <param name="payload">A byte array representing the payload of the response.</param>
        public GetSecurityLevelResponse(byte[] payload)
            : base(payload)
        {
        }

        /// <summary>
        /// Gets the device's current security level.
        /// </summary>
        public int SecurityLevel => IsSuccessful ? Parameter : -1;
    }
}
