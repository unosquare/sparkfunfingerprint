namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Linq;

    public sealed class BasicResponse : ResponseBase
    {
        public BasicResponse(byte[] payload)
            : base(payload)
        {
        }
    }

    public sealed class InitializationResponse : ResponseBase
    {
        public const string NoInfo = "No info available";

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

        public string FirmwareVersion { get; } = NoInfo;

        public int IsoAreaMaxSize { get; } = -1;

        public string SerialNumber { get; } = NoInfo;

        public override bool IsSuccessful => base.IsSuccessful && (RawSerialNumber?.Any(x => x != 0) ?? false);

        private byte[] RawSerialNumber { get; }
    }

    public sealed class FastSearchingResponse : ResponseBase
    {
        public FastSearchingResponse(byte[] payload)
            : base(payload)
        {
        }

        public override bool IsSuccessful => base.IsSuccessful && Parameter == 0x55;
    }

    public sealed class CountEnrolledFingerprintResponse : ResponseBase
    {
        public CountEnrolledFingerprintResponse(byte[] payload)
            : base(payload)
        {
        }

        public int EnrolledFingerprints => IsSuccessful ? Parameter : 0;
    }

    public sealed class CheckEnrollmentResponse : ResponseBase
    {
        public CheckEnrollmentResponse(byte[] payload)
            : base(payload)
        {
        }

        public bool IsEnrolled => IsSuccessful;
    }

    public sealed class EnrollmentResponse : ResponseBase
    {
        public EnrollmentResponse(byte[] payload)
            : base(payload)
        {
        }

        public byte[] Template => HasDataPacket ? (byte[])ResponseDataPacket.Data.Clone() : new byte[] { };
    }

    public sealed class CheckFingerPressingResponse : ResponseBase
    {
        public CheckFingerPressingResponse(byte[] payload)
            : base(payload)
        {
        }

        public bool IsPressed => Parameter == 0;
    }

    public sealed class MatchOneToNResponse : ResponseBase
    {
        public MatchOneToNResponse(byte[] payload)
            : base(payload)
        {
        }

        public int UserId => IsSuccessful ? Parameter : -1;
    }

    public sealed class TemplateResponse : ResponseBase
    {
        public TemplateResponse(byte[] payload)
            : base(payload)
        {
        }

        public byte[] Template => (byte[])ResponseDataPacket.Data.Clone();
    }

    public sealed class GetFingerprintImageResponse : ResponseBase
    {
        public GetFingerprintImageResponse(byte[] payload)
            : base(payload)
        {
        }

        public byte[] Image => (byte[])ResponseDataPacket.Data.Clone();
    }

    public sealed class GetRawImageResponse : ResponseBase
    {
        public GetRawImageResponse(byte[] payload)
            : base(payload)
        {
        }

        public byte[] Image => (byte[])ResponseDataPacket.Data.Clone();
    }

    public sealed class GetSecurityLevelResponse : ResponseBase
    {
        public GetSecurityLevelResponse(byte[] payload)
            : base(payload)
        {
        }

        public int SecurityLevel => IsSuccessful ? Parameter : -1;
    }
}
