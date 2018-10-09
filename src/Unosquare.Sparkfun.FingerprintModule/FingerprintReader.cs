namespace Unosquare.Sparkfun.FingerprintModule
{
    using SerialPort;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The main class representing the Sparkfun fingerprint reader module GT521Fxx.
    /// Reference: https://cdn.sparkfun.com/assets/learn_tutorials/7/2/3/GT-521F52_Programming_guide_V10_20161001.pdf
    /// WIKI: https://learn.sparkfun.com/tutorials/fingerprint-scanner-gt-521fxx-hookup-guide
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class FingerprintReader : IDisposable
    {
        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 115200;

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan FingerActionTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan EnrollTimeout = TimeSpan.FromSeconds(5);

        private ISerialPort _serialPort;
        private ManualResetEventSlim _serialPortDone = new ManualResetEventSlim(true);
        private InitializationResponse _deviceInfo;
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="FingerprintReader"/> class.
        /// </summary>
        /// <param name="model">The fingerprint reader model.</param>
        /// <remarks>The model determines the device capacity.</remarks>
        public FingerprintReader(FingerprintReaderModel model)
        {
            FingerprintCapacity = (int)model - 1;
        }

        #region Properties

        /// <summary>
        /// Gets the fingerprint capacity.
        /// </summary>
        public int FingerprintCapacity { get; }

        /// <summary>
        /// Gets the device firmware version.
        /// </summary>
        public string FirmwareVersion => _deviceInfo?.FirmwareVersion ?? InitializationResponse.NoInfo;

        /// <summary>
        /// Gets the device serial number.
        /// </summary>
        public string SerialNumber => _deviceInfo?.SerialNumber ?? InitializationResponse.NoInfo;

        /// <summary>
        /// Gets the maximum size of the iso area.
        /// </summary>
        public int IsoAreaMaxSize => _deviceInfo?.IsoAreaMaxSize ?? -1;

        #endregion

        #region Open-Close

        /// <summary>
        /// Opens and initialize the fingerprint device at the specified port name.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous open operation.</returns>
        /// <exception cref="InvalidOperationException">Device is already open. Call the close method first.</exception>
        public Task OpenAsync(string portName, CancellationToken ct = default)
        {
            if (_serialPort != null)
                throw new InvalidOperationException("Device is already open. Call the close method first.");

            return OpenAsync(portName, InitialBaudRate, ct);
        }

        /// <summary>
        /// Closes the fingerprint device if open.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous close operation.</returns>
        public async Task CloseAsync(CancellationToken ct = default)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return;

            try
            {
                _deviceInfo = null;
                await TurnLedOffAsync(ct);
                await CloseDeviceAsync(ct);
                _serialPort.Close();
                await Task.Delay(100, ct);
            }
            finally
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
        }
        #endregion

        #region Commands

        /// <summary>
        /// Sets the let status asynchronous.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous set led status operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        public Task<BasicResponse> SetLetStatusAsync(LedStatus status, CancellationToken ct = default)
        {
            if (status == LedStatus.On)
                return TurnLedOnAsync(ct);
            else
                return TurnLedOffAsync(ct);
        }

        /// <summary>
        /// Turns the led on asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous turn led on operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        public Task<BasicResponse> TurnLedOnAsync(CancellationToken ct = default) =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.CmosLed, 1), ct);

        /// <summary>
        /// Turns the led off asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous turn led off operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        public Task<BasicResponse> TurnLedOffAsync(CancellationToken ct = default) =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.CmosLed), ct);

        /// <summary>
        /// Fasts device searching asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous set fast device searching goperation.
        /// The result of the task contains an instance of <see cref="FastSearchingResponse"/>. 
        /// </returns>
        /// <remarks>The device operates as removable CD drive. If another removable CD drive exists in the system, connection time maybe will be long.
        /// To prevent this, <see cref="FingerprintReader.FastDeviceSearching"/> command is used for fast searching of the device.
        /// </remarks>
        public Task<FastSearchingResponse> FastDeviceSearching(CancellationToken ct = default) =>
            GetResponseAsync<FastSearchingResponse>(Command.Create(CommandCode.UsbInternalCheck), ct);

        /// <summary>
        /// Counts the enrolled fingerprint asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous count enrolled fingerprint operation.
        /// The result of the task contains an instance of <see cref="CountEnrolledFingerprintResponse"/>. 
        /// </returns>
        public Task<CountEnrolledFingerprintResponse> CountEnrolledFingerprintAsync(CancellationToken ct = default) =>
            GetResponseAsync<CountEnrolledFingerprintResponse>(Command.Create(CommandCode.GetEnrollCount), ct);

        /// <summary>
        /// Checks the enrollment status asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier to check.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous check enrolled status operation.
        /// The result of the task contains an instance of <see cref="CheckEnrollmentResponse"/>. 
        /// </returns>
        public Task<CheckEnrollmentResponse> CheckEnrollmentStatusAsync(int userId, CancellationToken ct = default) =>
            GetResponseAsync<CheckEnrollmentResponse>(Command.Create(CommandCode.CheckEnrolled, userId), ct);

        /// <summary>
        /// Enrolls a new user asynchronous.
        /// </summary>
        /// <param name="iteration">The iteration.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous enroll user operation.
        /// The result of the task contains an instance of <see cref="EnrollmentResponse"/>. 
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// iteration
        /// or
        /// userId
        /// </exception>
        public async Task<EnrollmentResponse> EnrollUserAsync(int iteration, int userId, CancellationToken ct = default)
        {
            if (iteration < 0 || iteration > 3)
                throw new ArgumentOutOfRangeException($"{nameof(iteration)} must be a number between 1 and 3");

            if (userId < -1 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between -1 and {FingerprintCapacity}.");

            if (iteration == 1)
            {
                // Start enrollment
                var startResult = await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.EnrollStart, userId), ct);
                if (!startResult.IsSuccessful)
                    return ResponseBase.GetUnsuccessfulResponse<EnrollmentResponse>(startResult.ErrorCode);
            }

            return await EnrollAsync(iteration, userId, ct);
        }

        /// <summary>
        /// Waits a finger action asynchronous.
        /// </summary>
        /// <param name="action">The action to wait for.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous wait finger action operation.
        /// The result of the task contains a <see cref="bool"/> indicating if the action was performed.
        /// <c>true</c> if the action was performed; otherwise, <c>false</c>.
        /// </returns>
        public Task<bool> WaitFingerActionAsync(FingerAction action, CancellationToken ct = default) =>
            WaitFingerActionAsync(action, FingerActionTimeout, ct);

        /// <summary>
        /// Waits a finger action for a specified time period asynchronous.
        /// </summary>
        /// <param name="action">The action to wait for.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous wait finger action operation.
        /// The result of the task contains a <see cref="bool"/> indicating if the action was performed.
        /// <c>true</c> if the action was performed; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> WaitFingerActionAsync(FingerAction action, TimeSpan timeout, CancellationToken ct = default)
        {
            var startTime = DateTime.Now;
            while (true)
            {
                var result = await CheckFingerPressingStatusAsync(ct);
                if (!result.IsSuccessful)
                    return false;

                if ((action == FingerAction.Place && result.IsPressed) ||
                    (action == FingerAction.Remove && !result.IsPressed))
                    return true;

                if (DateTime.Now.Subtract(startTime) > timeout)
                    return false;

                await Task.Delay(10, ct);
            }
        }

        /// <summary>
        /// Checks the finger pressing status asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous check finger pressing status operation.
        /// The result of the task contains an instance of <see cref="CheckFingerPressingResponse"/>. 
        /// </returns>
        public Task<CheckFingerPressingResponse> CheckFingerPressingStatusAsync(CancellationToken ct = default) =>
            GetResponseAsync<CheckFingerPressingResponse>(Command.Create(CommandCode.IsPressFinger), ct);

        /// <summary>
        /// Deletes all users from device's database asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous delete all users operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        public Task<BasicResponse> DeleteAllUsersAsync(CancellationToken ct = default) =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.DeleteAll), ct);

        /// <summary>
        /// Deletes a specific user asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous delete user operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public Task<BasicResponse> DeleteUserAsync(int userId, CancellationToken ct = default)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.DeleteID, userId), ct);
        }

        /// <summary>
        /// Match 1:1 asynchronous. Acquires an image from the device and verify if it matches the supplied user id.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous match one to one operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public async Task<BasicResponse> MatchOneToOneAsync(int userId, CancellationToken ct = default)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            var captureResult = await CaptureFingerprintPatternAsync<BasicResponse>(ct);
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.Verify, userId), ct);
        }

        /// <summary>
        /// Match 1:1 asynchronous. Verify if a provided fingerprint template matches the supplied user id.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="template">The fingerprint template.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous match one to one operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public async Task<BasicResponse> MatchOneToOneAsync(int userId, byte[] template, CancellationToken ct = default)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.VerifyTemplate, userId, template), ct);
        }

        /// <summary>
        /// Match 1:N asynchronous. Acquires an image from the device and identifies the user id it belongs to.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous match one to n operation.
        /// The result of the task contains an instance of <see cref="MatchOneToNResponse"/>. 
        /// </returns>
        public async Task<MatchOneToNResponse> MatchOneToN(CancellationToken ct = default)
        {
            var captureResult = await CaptureFingerprintPatternAsync<MatchOneToNResponse>(ct);
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<MatchOneToNResponse>(Command.Create(CommandCode.Identify), ct);
        }

        /// <summary>
        /// Match 1:N asynchronous. Identifies the user id whom a provided fingerprint template belongs to.
        /// </summary>
        /// <param name="template">The fingerprint template.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous match one to n operation.
        /// The result of the task contains an instance of <see cref="MatchOneToNResponse" />.
        /// </returns>
        public async Task<MatchOneToNResponse> MatchOneToN(byte[] template, CancellationToken ct = default) =>
            await GetResponseAsync<MatchOneToNResponse>(Command.Create(CommandCode.IdentifyTemplate, 0, template), ct);

        /// <summary>
        /// Match 1:N asynchronous. Identifies the user id whom a provided fingerprint template belongs to.
        /// </summary>
        /// <param name="template">The special fingerprint template.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous match one to n operation.
        /// The result of the task contains an instance of <see cref="MatchOneToNResponse" />.
        /// </returns>
        /// <remarks><see cref="MatchOneToN2"/> uses a special fingerprint template with 2 extra bytes at the beginning of the byte array.</remarks>
        public async Task<MatchOneToNResponse> MatchOneToN2(byte[] template, CancellationToken ct = default) =>
            await GetResponseAsync<MatchOneToNResponse>(Command.Create(CommandCode.IdentifyTemplate2, 500, template), ct);

        /// <summary>
        /// Makes a fingerprint template asynchronous. This template must be used only for transmission and not for user enrollment.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous make template operation.
        /// The result of the task contains an instance of <see cref="TemplateResponse" />.
        /// </returns>
        public async Task<TemplateResponse> MakeTemplateAsync(CancellationToken ct = default)
        {
            var captureResult = await CaptureFingerprintPatternAsync<TemplateResponse>(ct);
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<TemplateResponse>(Command.Create(CommandCode.MakeTemplate), ct);
        }

        /// <summary>
        /// Gets a fingerprint image asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous get image operation.
        /// The result of the task contains an instance of <see cref="GetFingerprintImageResponse" />.
        /// </returns>
        public async Task<GetFingerprintImageResponse> GetImageAsync(CancellationToken ct = default)
        {
            var captureResult = await CaptureFingerprintPatternAsync<GetFingerprintImageResponse>(ct);
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<GetFingerprintImageResponse>(Command.Create(CommandCode.GetImage), ct);
        }

        /// <summary>
        /// Gets a raw image from the device asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous get raw image operation.
        /// The result of the task contains an instance of <see cref="GetRawImageResponse" />.
        /// </returns>
        public async Task<GetRawImageResponse> GetRawImageAsync(CancellationToken ct = default)
        {
            var captureResult = await CaptureFingerprintPatternAsync<GetRawImageResponse>(ct);
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<GetRawImageResponse>(Command.Create(CommandCode.GetRawImage), ct);
        }

        /// <summary>
        /// Gets a fingerprint template asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous get template operation.
        /// The result of the task contains an instance of <see cref="TemplateResponse" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public Task<TemplateResponse> GetTemplateAsync(int userId, CancellationToken ct = default)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<TemplateResponse>(Command.Create(CommandCode.GetTemplate, userId), ct);
        }

        /// <summary>
        /// Sets a fingerprint template asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="template">The fingerprint template.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous set template operation.
        /// The result of the task contains an instance of <see cref="BasicResponse" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public Task<BasicResponse> SetTemplateAsync(int userId, byte[] template, CancellationToken ct = default)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.SetTemplate, userId, template), ct);
        }

        /// <summary>
        /// Sets device to stand by mode (low power mode).
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous enter standby operation.
        /// The result of the task contains an instance of <see cref="BasicResponse" />.
        /// </returns>
        public Task<BasicResponse> EnterStandByMode(CancellationToken ct = default)
        {
            // TODO: Implement to wake up
            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.EnterStandbyMode), ct);
        }

        /// <summary>
        /// Sets the device's security level asynchronous. 1 is the lowest security level, 5 is the highest security level.
        /// </summary>
        /// <param name="level">The security level.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous set security level operation.
        /// The result of the task contains an instance of <see cref="BasicResponse" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">level</exception>
        public Task<BasicResponse> SetSecurityLevelAsync(int level, CancellationToken ct = default)
        {
            if (level < 1 || level > 5)
                throw new ArgumentOutOfRangeException($"{nameof(level)} must be a number between 1 and 5.");

            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.SetSecurityLevel, level), ct);
        }

        /// <summary>
        /// Gets the device's security level asynchronous. 1 is the lowest security level, 5 is the highest security level.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous get security level operation.
        /// The result of the task contains an instance of <see cref="GetSecurityLevelResponse" />.
        /// </returns>
        public Task<GetSecurityLevelResponse> GetSecurityLevelAsync(CancellationToken ct = default) =>
            GetResponseAsync<GetSecurityLevelResponse>(Command.Create(CommandCode.GetSecurityLevel), ct);

        #endregion

        #region Private Functions

        /// <summary>
        /// Opens and initialize the fingerprint device at the specified port name with the specified baud rate.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="baudRate">The baud rate.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous open operation.</returns>
        /// <exception cref="Exception">The device could not be initialized.</exception>
        private async Task OpenAsync(string portName, int baudRate, CancellationToken ct)
        {
            _serialPort =
#if NET452
                new MsSerialPort(portName, baudRate);
#else
                new RjcpSerialPort(portName, baudRate);
#endif

            _serialPort.Open();
            await Task.Delay(100, ct);

            if (baudRate != TargetBaudRate)
            {
                // Change baud rate to target baud rate for better performance
                await SetBaudrateAsync(TargetBaudRate, ct);
            }
            else
            {
                _deviceInfo = await OpenDeviceAsync(ct);
                if (!_deviceInfo.IsSuccessful)
                    throw new Exception("The device could not be initialized.");

                await TurnLedOnAsync(ct);
            }
        }

        /// <summary>
        /// Opens and initializes the device asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous open device operation.
        /// The result of the task contains an instance of <see cref="InitializationResponse"/>. 
        /// </returns>
        private Task<InitializationResponse> OpenDeviceAsync(CancellationToken ct) =>
            GetResponseAsync<InitializationResponse>(Command.Create(CommandCode.Open, 1), ct);

        /// <summary>
        /// Closes the device asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous close device operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        private Task<BasicResponse> CloseDeviceAsync(CancellationToken ct) =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.Close), ct);

        /// <summary>
        /// Sets the baud rate asynchronous.
        /// This closes and re-opens the device.
        /// </summary>
        /// <param name="baudrate">The baud rate.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous set baud rate operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        private async Task<BasicResponse> SetBaudrateAsync(int baudrate, CancellationToken ct)
        {
            var response = await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.ChangeBaudRate, baudrate), ct);

            // It is possible that we don't have a response when changing baud rate 
            // because we are still listening with the previous config. 
            // If this happens we'll have a communication error response (CommErr)
            if (response.IsSuccessful || response.ErrorCode == ErrorCode.CommErr)
            {
                var portName = _serialPort.PortName;
                await CloseAsync(ct);
                await OpenAsync(portName, baudrate, ct);
            }

            return response;
        }

        /// <summary>
        /// Enroll stage asynchronous.
        /// </summary>
        /// <param name="iteration">The iteration.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous enroll operation.
        /// The result of the task contains an instance of <see cref="EnrollmentResponse"/>. 
        /// </returns>
        private async Task<EnrollmentResponse> EnrollAsync(int iteration, int userId, CancellationToken ct)
        {
            var enrollmentFingerActionTimeOut = TimeSpan.FromSeconds(10);

            var captureResult = await CaptureFingerprintPatternAsync<EnrollmentResponse>(enrollmentFingerActionTimeOut, ct);
            if (!captureResult.IsSuccessful)
                return captureResult;

            var cmd = iteration == 1 ? CommandCode.Enroll1 :
                      iteration == 2 ? CommandCode.Enroll2 :
                      CommandCode.Enroll3;

            return await GetResponseAsync<EnrollmentResponse>(Command.Create(cmd, userId), EnrollTimeout, ct);
        }

        /// <summary>
        /// Capture fingerprint pattern asynchronous. A special pattern for capture a fingerprint from the device.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <returns>A task that represents the asynchronous capture fingerprint pattern operation.
        /// The result of the task contains an instance of a response of type T.
        /// </returns>
        private Task<T> CaptureFingerprintPatternAsync<T>(CancellationToken ct)
            where T : ResponseBase => CaptureFingerprintPatternAsync<T>(FingerActionTimeout, ct);

        /// <summary>
        /// Capture fingerprint pattern asynchronous. A special pattern for capture a fingerprint from the device.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <param name="fingerActionTimeout">The finger action timeout.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous capture fingerprint pattern operation.
        /// The result of the task contains an instance of a response of type T.
        /// </returns>
        private async Task<T> CaptureFingerprintPatternAsync<T>(TimeSpan fingerActionTimeout, CancellationToken ct)
            where T : ResponseBase
        {
            var actionPerformed = await WaitFingerActionAsync(FingerAction.Place, fingerActionTimeout, ct);
            if (!actionPerformed)
                return ResponseBase.GetUnsuccessfulResponse<T>(ErrorCode.FingerNotPressed);

            var captureResult = await CaptureFingerprintAsync(ct);

            if (typeof(T) == captureResult.GetType())
                return captureResult as T;

            return Activator.CreateInstance(typeof(T), captureResult.Payload) as T;
        }

        /// <summary>
        /// Captures a fingerprint from the device asynchronous.
        /// </summary>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous capture fingerprint operation.
        /// The result of the task contains an instance of <see cref="BasicResponse" />.
        /// </returns>
        private Task<BasicResponse> CaptureFingerprintAsync(CancellationToken ct) =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.CaptureFinger), ct);

        #endregion

        #region Write-Read

        /// <summary>
        /// Sends a command to the device and gets the device's response asynchronous.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <param name="command">The command object to send.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous get response operation.
        /// The result of the task contains an instance of a response type T.
        /// </returns>
        private Task<T> GetResponseAsync<T>(Command command, CancellationToken ct)
            where T : ResponseBase => GetResponseAsync<T>(command, DefaultTimeout, ct);

        /// <summary>
        /// Sends a command to the device and gets the device's response asynchronous.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <param name="command">The command object to send.</param>
        /// <param name="responseTimeout">The response timeout.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous get response operation.
        /// The result of the task contains an instance of a response type T.
        /// </returns>
        private async Task<T> GetResponseAsync<T>(Command command, TimeSpan responseTimeout, CancellationToken ct)
                    where T : ResponseBase
        {
            var expectedResponseLength = PacketBase.BasePacketLength;
            if (ResponseBase.ResponseDataLength.ContainsKey(command.CommandCode))
            {
                expectedResponseLength += 6 + ResponseBase.ResponseDataLength[command.CommandCode];

                // Special cases
                if ((command.CommandCode == CommandCode.Open && command.Parameter == 0) ||
                    (command.CommandCode == CommandCode.Enroll3 && command.Parameter != -1))
                    expectedResponseLength = PacketBase.BasePacketLength;
            }

            _serialPortDone.Wait(ct);
            _serialPortDone.Reset();
            try
            {
                var responsePkt = await GetInternalResponseAsync<T>(command.Payload, expectedResponseLength, responseTimeout, ct);
                if (command.HasDataPacket && responsePkt?.IsSuccessful == true)
                {
                    responsePkt = await GetInternalResponseAsync<T>(command.DataPacket.Payload, expectedResponseLength, responseTimeout, ct);
                }

                return responsePkt;
            }
            finally
            {
                _serialPortDone.Set();
            }
        }

        /// <summary>
        /// Sends the command  payload to the device and gets the device's response asynchronous.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <param name="payload">The payload.</param>
        /// <param name="expectedResponseLength">Expected length of the response.</param>
        /// <param name="responseTimeout">The response timeout.</param>
        /// <param name="ct">The ct.</param>
        /// <returns>A task that represents the asynchronous get response operation.
        /// The result of the task contains an instance of a response type T.</returns>
        private async Task<T> GetInternalResponseAsync<T>(byte[] payload, int expectedResponseLength, TimeSpan responseTimeout, CancellationToken ct)
                            where T : ResponseBase
        {
            await WriteAsync(payload, ct);
            var response = await ReadAsync(expectedResponseLength, responseTimeout, CancellationToken.None);
            if (response == null || response.Length == 0)
                return ResponseBase.GetUnsuccessfulResponse<T>(ErrorCode.CommErr);

            return Activator.CreateInstance(typeof(T), response) as T;
        }

        /// <summary>
        /// Writes data to the serial port asynchronous.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteAsync(byte[] payload, CancellationToken ct)
        {
            if (_serialPort == null || _serialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(OpenAsync)} method before attempting communication");

#if NET452
            await _serialPort.BaseStream.WriteAsync(payload, 0, payload.Length, ct);
            await _serialPort.BaseStream.FlushAsync(ct);
#else
            await _serialPort.WriteAsync(payload, 0, payload.Length, ct);
            await _serialPort.FlushAsync(ct);
#endif
        }

        /// <summary>
        /// Reads data from the serial port asynchronous.
        /// </summary>
        /// <param name="expectedResponseLength">Expected length of the response.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private async Task<byte[]> ReadAsync(int expectedResponseLength, TimeSpan timeout, CancellationToken ct)
        {
            if (_serialPort == null || _serialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(OpenAsync)} method before attempting communication");

            var data = new List<byte>();
            var readed = new byte[1024];
            var startTime = DateTime.Now;

            while (data.Count < expectedResponseLength || _serialPort.BytesToRead > 0)
            {
                if (_serialPort.BytesToRead > 0)
                {
#if NET452
                    var bytesRead = await _serialPort.BaseStream.ReadAsync(readed, 0, readed.Length, ct);
#else
                    var bytesRead = await _serialPort.ReadAsync(readed, 0, readed.Length, ct);
#endif
                    if (bytesRead > 0)
                        data.AddRange(readed.Take(bytesRead));
                }

                if (DateTime.Now.Subtract(startTime) > timeout)
                    return null;

                await Task.Delay(10, ct);
            }

            return data.ToArray();
        }

        #endregion

        #region IDisposable Support

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            if (disposing)
            {
                CloseAsync().Wait();
                _serialPortDone.Dispose();
            }

            _serialPortDone = null;
            _disposedValue = true;
        }

        #endregion
    }
}
