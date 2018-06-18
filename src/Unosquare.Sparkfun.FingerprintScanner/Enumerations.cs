namespace Unosquare.Sparkfun.FingerprintScanner
{
    using System;

    /// <summary>
    /// Fingerprint reader model
    /// </summary>
    /// <remarks>Each model has different fingerprint hold capacity.</remarks>
    public enum FingerprintReaderModel
    {
        /// <summary>
        /// Model GT-521F32
        /// </summary>
        /// <remarks>Holds a maximum of 200 fingerprints.</remarks>
        GT521F32 = 200,

        /// <summary>
        /// Model GT-521F52
        /// </summary>
        /// /// <remarks>Holds a maximum of 3000 fingerprints.</remarks>
        GT521F52 = 3000,
    }

    /// <summary>
    /// Response codes
    /// </summary>
    public enum ResponseCode : ushort
    {
        /// <summary>
        /// Acknowledge
        /// </summary>
        Ack = 0x30,

        /// <summary>
        /// Non-acknowledge
        /// </summary>
        Nack = 0x31
    }

    /// <summary>
    /// Command codes
    /// </summary>
    public enum CommandCode : ushort
    {
        /// <summary>
        /// Initialization
        /// </summary>
        Open = 0x01,

        /// <summary>
        /// Termination
        /// </summary>
        Close = 0x02,

        /// <summary>
        /// Check if the connected USD device is valid
        /// </summary>
        UsbInternalCheck = 0x03,

        /// <summary>
        /// Change UART baudrate
        /// </summary>
        ChangeBaudRate = 0x04,

        /// <summary>
        /// Control CMOS led
        /// </summary>
        CmosLed = 0x12,

        /// <summary>
        /// Get enrolled fingerprint count
        /// </summary>
        GetEnrollCount = 0x20,

        /// <summary>
        /// Check whether a specified ID is already enrolled
        /// </summary>
        CheckEnrolled = 0x21,

        /// <summary>
        /// Start an enrollment
        /// </summary>
        EnrollStart = 0x22,

        /// <summary>
        /// Make first template for an enrollment
        /// </summary>
        Enroll1 = 0x23,

        /// <summary>
        /// Make second template for an enrollment
        /// </summary>
        Enroll2 = 0x24,

        /// <summary>
        /// Make third template for and enrollment,
        /// merge the three templates into one template,
        /// save marged template to the database
        /// </summary>
        Enroll3 = 0x25,

        /// <summary>
        /// Check if a finger is palced on the sensor
        /// </summary>
        IsPressFinger = 0x26,

        /// <summary>
        /// Delete a fingerprint with the specified ID
        /// </summary>
        DeleteID = 0x40,

        /// <summary>
        /// Delete all fingerprints from the device database
        /// </summary>
        DeleteAll = 0x41,

        /// <summary>
        /// 1:1 Verification on the capture fingerprint image with the specified ID
        /// </summary>
        Verify = 0x50,

        /// <summary>
        /// 1:N Identification on the capture fingerprint image with the device database
        /// </summary>
        Identify = 0x51,

        /// <summary>
        /// 1:1 Verification of a fingerprint template with the specified ID
        /// </summary>
        VerifyTemplate = 0x52,

        /// <summary>
        /// 1:N Identification of a fingerprint template with the device database
        /// </summary>
        IdentifyTemplate = 0x53,

        /// <summary>
        /// Capture a fingerprint image (256 x 256) from the sensor
        /// </summary>
        CaptureFinger = 0x60,

        /// <summary>
        /// Make a template for transmission
        /// </summary>
        MakeTemplate = 0x61,

        /// <summary>
        /// Download the capture fingerprint image (256 x 256)
        /// </summary>
        GetImage = 0x62,

        /// <summary>
        /// Capture and download a raw fingerprint image (320 x 240)
        /// </summary>
        GetRawImage = 0x63,

        /// <summary>
        /// Download the template of the specified ID
        /// </summary>
        GetTemplate = 0x70,

        /// <summary>
        /// Upload a template for the specified ID
        /// </summary>
        SetTemplate = 0x71,

        /// <summary>
        /// Start device database download
        /// </summary>
        [Obsolete("Command no longer supported.", true)]
        GetDatabaseStart = 0x72,

        /// <summary>
        /// End devoice database download
        /// </summary>
        [Obsolete("Command no longer supported.", true)]
        GetDatabaseEnd = 0x73,

        /// <summary>
        /// Set security level for a specified ID
        /// </summary>
        SetSecurityLevel = 0xF0,

        /// <summary>
        /// Get security level from a specified ID
        /// </summary>
        GetSecurityLevel = 0xF1,

        /// <summary>
        /// Identify of the capture fingerprint image with the specified template
        /// </summary>
        IdentifyTemplate2 = 0xF4,

        /// <summary>
        /// Enter standby mode (Low power mode)
        /// </summary>
        EnterStandbyMode = 0xF9,
    }

    /// <summary>
    /// Error codes
    /// </summary>
    /// <remarks>
    /// Error codes 0 to 2999 indicate the ID for a duplicate fingerprint while enrollment or setting template
    /// </remarks>
    public enum ErrorCode
    {
        /// <summary>
        /// Capture timeout
        /// </summary>
        [Obsolete]
        Timeout = 0x1001,

        /// <summary>
        /// Invalid serial baudrate
        /// </summary>
        [Obsolete]
        InvalidBaudrate = 0x1002,

        /// <summary>
        /// The specified ID is not between the valid range
        /// </summary>
        InvalidPos = 0x1003,

        /// <summary>
        /// The specified ID is not used
        /// </summary>
        NotUsed = 0x1004,

        /// <summary>
        /// The specified ID is already used
        /// </summary>
        AlreadyUse = 0x1005,

        /// <summary>
        /// Communication error
        /// </summary>
        CommErr = 0x1006,

        /// <summary>
        /// 1:1 verification error
        /// </summary>
        VerifyFail = 0x1007,

        /// <summary>
        /// 1:N identification error
        /// </summary>
        IdentifyFail = 0x1008,

        /// <summary>
        /// The device database is full
        /// </summary>
        DbFull = 0x1009,

        /// <summary>
        /// The device database is empty
        /// </summary>
        DbEmpty = 0x100A,

        /// <summary>
        /// Invalid order of the enrollment
        /// </summary>
        [Obsolete]
        TurnErr = 0x100B,

        /// <summary>
        /// Too bad finger
        /// </summary>
        BadFinger = 0x100C,

        /// <summary>
        /// Enrollment failure
        /// </summary>
        EnrollFailed = 0x100D,

        /// <summary>
        /// The specified command is not supported
        /// </summary>
        NotSupported = 0x100E,

        /// <summary>
        /// Device error
        /// </summary>
        DeviceErr = 0x100F,

        /// <summary>
        /// The capturing is cancelled
        /// </summary>
        [Obsolete]
        CaptureCanceled = 0x1010,

        /// <summary>
        /// Invalid parameter
        /// </summary>
        InvalidParam = 0x1011,

        /// <summary>
        /// Finger is not pressed
        /// </summary>
        FingerNotPressed = 0x1012,

        /// <summary>
        /// No error
        /// </summary>
        NoError = 0xFFFF,
    }

    /// <summary>
    /// CMOS led status
    /// </summary>
    public enum LedStatus
    {
        /// <summary>
        /// Off
        /// </summary>
        Off,

        /// <summary>
        /// On
        /// </summary>
        On
    }

    /// <summary>
    /// Finger action
    /// </summary>
    public enum FingerAction
    {
        /// <summary>
        /// Place fingerprint in sensor
        /// </summary>
        Place,

        /// <summary>
        /// Remove fingerprint from sensor
        /// </summary>
        Remove
    }
}