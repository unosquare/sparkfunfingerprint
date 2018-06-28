namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;
    using System.IO.Ports;
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
        #region Private consts

        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 115200;

        #endregion

        #region Private static fields

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan FingerActionTimeout = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan EnrollTimeout = TimeSpan.FromSeconds(5);

        #endregion

        #region Private fields

        private ManualResetEventSlim _serialPortDone = new ManualResetEventSlim(true);
        private SerialPort _serialPort;
        private InitializationResponse _deviceInfo;
        private bool _disposedValue; // To detect redundant calls

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="FingerprintReader"/> class.
        /// </summary>
        /// <param name="model">The fingerprint reader model.</param>
        /// <remarks>The model determines the device capacity.</remarks>
        public FingerprintReader(FingerprintReaderModel model)
        {
            FingerprintCapacity = (int)model - 1;
        }

        #endregion

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
        /// <returns>A task that represents the asynchronous open operation.</returns>
        /// <exception cref="InvalidOperationException">Device is already open. Call the close method first.</exception>
        public Task Open(string portName)
        {
            if (_serialPort != null)
                throw new InvalidOperationException("Device is already open. Call the close method first.");

            return Open(portName, InitialBaudRate);
        }

        /// <summary>
        /// Closes the fingerprint device if open.
        /// </summary>
        /// <returns>A task that represents the asynchronous close operation.</returns>
        public async Task Close()
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                return;

            try
            {
                _deviceInfo = null;
                await TurnLedOffAsync();
                await CloseDeviceAsync();
                _serialPort.Close();
                await Task.Delay(100);
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
        /// <returns>A task that represents the asynchronous set led status operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        public Task<BasicResponse> SetLetStatusAsync(LedStatus status)
        {
            if (status == LedStatus.On)
                return TurnLedOnAsync();
            else
                return TurnLedOffAsync();
        }

        /// <summary>
        /// Turns the led on asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous turn led on operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        public Task<BasicResponse> TurnLedOnAsync() =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.CmosLed, 1));

        /// <summary>
        /// Turns the led off asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous turn led off operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        public Task<BasicResponse> TurnLedOffAsync() =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.CmosLed));

        /// <summary>
        /// Fasts device searching asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous set fast device searching goperation.
        /// The result of the task contains an instance of <see cref="FastSearchingResponse"/>. 
        /// </returns>
        /// <remarks>The device operates as removable CD drive. If another removable CD drive exists in the system, connection time maybe will be long.
        /// To prevent this, <see cref="FingerprintReader.FastDeviceSearching"/> command is used for fast searching of the device.
        /// </remarks>
        public Task<FastSearchingResponse> FastDeviceSearching() =>
            GetResponseAsync<FastSearchingResponse>(Command.Create(CommandCode.UsbInternalCheck));

        /// <summary>
        /// Counts the enrolled fingerprint asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous count enrolled fingerprint operation.
        /// The result of the task contains an instance of <see cref="CountEnrolledFingerprintResponse"/>. 
        /// </returns>
        public Task<CountEnrolledFingerprintResponse> CountEnrolledFingerprintAsync() =>
            GetResponseAsync<CountEnrolledFingerprintResponse>(Command.Create(CommandCode.GetEnrollCount));

        /// <summary>
        /// Checks the enrollment status asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier to check.</param>
        /// <returns>A task that represents the asynchronous check enrolled status operation.
        /// The result of the task contains an instance of <see cref="CheckEnrollmentResponse"/>. 
        /// </returns>
        public Task<CheckEnrollmentResponse> CheckEnrollmentStatusAsync(int userId) =>
            GetResponseAsync<CheckEnrollmentResponse>(Command.Create(CommandCode.CheckEnrolled, userId));

        /// <summary>
        /// Enrolls a new user asynchronous.
        /// </summary>
        /// <param name="iteration">The iteration.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>A task that represents the asynchronous enroll user operation.
        /// The result of the task contains an instance of <see cref="EnrollmentResponse"/>. 
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// iteration
        /// or
        /// userId
        /// </exception>
        public async Task<EnrollmentResponse> EnrollUserAsync(int iteration, int userId)
        {
            if (iteration < 0 || iteration > 3)
                throw new ArgumentOutOfRangeException($"{nameof(iteration)} must be a number between 1 and 3");

            if (userId < -1 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between -1 and {FingerprintCapacity}.");

            if (iteration == 1)
            {
                // Start enrollment
                var startResult = await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.EnrollStart, userId));
                if (!startResult.IsSuccessful)
                    return ResponseBase.GetUnsuccessfulResponse<EnrollmentResponse>(startResult.ErrorCode);
            }

            return await EnrollAsync(iteration, userId);
        }

        /// <summary>
        /// Waits a finger action asynchronous.
        /// </summary>
        /// <param name="action">The action to wait for.</param>
        /// <returns>A task that represents the asynchronous wait finger action operation.
        /// The result of the task contains a <see cref="bool"/> indicating if the action was performed.
        /// <c>true</c> if the action was performed; otherwise, <c>false</c>.
        /// </returns>
        public Task<bool> WaitFingerActionAsync(FingerAction action) => WaitFingerActionAsync(action, FingerActionTimeout);

        /// <summary>
        /// Waits a finger action for a specified time period asynchronous.
        /// </summary>
        /// <param name="action">The action to wait for.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns>A task that represents the asynchronous wait finger action operation.
        /// The result of the task contains a <see cref="bool"/> indicating if the action was performed.
        /// <c>true</c> if the action was performed; otherwise, <c>false</c>.
        /// </returns>
        public async Task<bool> WaitFingerActionAsync(FingerAction action, TimeSpan timeout)
        {
            var startTime = DateTime.Now;
            while (true)
            {
                var result = await CheckFingerPressingStatusAsync();
                if (!result.IsSuccessful)
                    return false;

                if ((action == FingerAction.Place && result.IsPressed) ||
                    (action == FingerAction.Remove && !result.IsPressed))
                    return true;

                if (DateTime.Now.Subtract(startTime) > timeout)
                    return false;

                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Checks the finger pressing status asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous check finger pressing status operation.
        /// The result of the task contains an instance of <see cref="CheckFingerPressingResponse"/>. 
        /// </returns>
        public Task<CheckFingerPressingResponse> CheckFingerPressingStatusAsync() =>
            GetResponseAsync<CheckFingerPressingResponse>(Command.Create(CommandCode.IsPressFinger));

        /// <summary>
        /// Deletes all users from device's database asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous delete all users operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        public Task<BasicResponse> DeleteAllUsersAsync() =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.DeleteAll));

        /// <summary>
        /// Deletes a specific user asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>A task that represents the asynchronous delete user operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public Task<BasicResponse> DeleteUserAsync(int userId)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.DeleteID, userId));
        }

        /// <summary>
        /// Match 1:1 asynchronous. Acquires an image from the device and verify if it matches the supplied user id.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>A task that represents the asynchronous match one to one operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public async Task<BasicResponse> MatchOneToOneAsync(int userId)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            var captureResult = await CaptureFingerprintPatternAsync<BasicResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.Verify, userId));
        }

        /// <summary>
        /// Match 1:1 asynchronous. Verify if a provided fingerprint template matches the supplied user id.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="template">The fingerprint template.</param>
        /// <returns>A task that represents the asynchronous match one to one operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public async Task<BasicResponse> MatchOneToOneAsync(int userId, byte[] template)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.VerifyTemplate, userId, template));
        }

        /// <summary>
        /// Match 1:N asynchronous. Acquires an image from the device and identifies the user id it belongs to.
        /// </summary>
        /// <returns>A task that represents the asynchronous match one to n operation.
        /// The result of the task contains an instance of <see cref="MatchOneToNResponse"/>. 
        /// </returns>
        public async Task<MatchOneToNResponse> MatchOneToN()
        {
            var captureResult = await CaptureFingerprintPatternAsync<MatchOneToNResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<MatchOneToNResponse>(Command.Create(CommandCode.Identify));
        }

        /// <summary>
        /// Match 1:N asynchronous. Identifies the user id whom a provided fingerprint template belongs to.
        /// </summary>
        /// <param name="template">The fingerprint template.</param>
        /// <returns>A task that represents the asynchronous match one to n operation.
        /// The result of the task contains an instance of <see cref="MatchOneToNResponse" />.
        /// </returns>
        public async Task<MatchOneToNResponse> MatchOneToN(byte[] template) => 
            await GetResponseAsync<MatchOneToNResponse>(Command.Create(CommandCode.IdentifyTemplate, 0, template));

        /// <summary>
        /// Match 1:N asynchronous. Identifies the user id whom a provided fingerprint template belongs to.
        /// </summary>
        /// <param name="template">The special fingerprint template.</param>
        /// <returns>A task that represents the asynchronous match one to n operation.
        /// The result of the task contains an instance of <see cref="MatchOneToNResponse" />.
        /// </returns>
        /// <remarks><see cref="MatchOneToN2"/> uses a special fingerprint template with 2 extra bytes at the beginning of the byte array.</remarks>
        public async Task<MatchOneToNResponse> MatchOneToN2(byte[] template) =>
            await GetResponseAsync<MatchOneToNResponse>(Command.Create(CommandCode.IdentifyTemplate2, 500, template));

        /// <summary>
        /// Makes a fingerprint template asynchronous. This template must be used only for transmission and not for user enrollment.
        /// </summary>
        /// <returns>A task that represents the asynchronous make template operation.
        /// The result of the task contains an instance of <see cref="TemplateResponse" />.
        /// </returns>
        public async Task<TemplateResponse> MakeTemplateAsync()
        {
            var captureResult = await CaptureFingerprintPatternAsync<TemplateResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<TemplateResponse>(Command.Create(CommandCode.MakeTemplate));
        }

        /// <summary>
        /// Gets a fingerprint image asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous get image operation.
        /// The result of the task contains an instance of <see cref="GetFingerprintImageResponse" />.
        /// </returns>
        public async Task<GetFingerprintImageResponse> GetImageAsync()
        {
            var captureResult = await CaptureFingerprintPatternAsync<GetFingerprintImageResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<GetFingerprintImageResponse>(Command.Create(CommandCode.GetImage));
        }

        /// <summary>
        /// Gets a raw image from the device asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous get raw image operation.
        /// The result of the task contains an instance of <see cref="GetRawImageResponse" />.
        /// </returns>
        public async Task<GetRawImageResponse> GetRawImageAsync()
        {
            var captureResult = await CaptureFingerprintPatternAsync<GetRawImageResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<GetRawImageResponse>(Command.Create(CommandCode.GetRawImage));
        }
                
        /// <summary>
        /// Gets a fingerprint template asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>A task that represents the asynchronous get template operation.
        /// The result of the task contains an instance of <see cref="TemplateResponse" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public Task<TemplateResponse> GetTemplateAsync(int userId)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<TemplateResponse>(Command.Create(CommandCode.GetTemplate, userId));
        }

        /// <summary>
        /// Sets a fingerprint template asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="template">The fingerprint template.</param>
        /// <returns>A task that represents the asynchronous set template operation.
        /// The result of the task contains an instance of <see cref="BasicResponse" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">userId</exception>
        public Task<BasicResponse> SetTemplateAsync(int userId, byte[] template)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.SetTemplate, userId, template));
        }

        /// <summary>
        /// Sets device to stand by mode (low power mode).
        /// </summary>
        /// <returns>A task that represents the asynchronous enter standby operation.
        /// The result of the task contains an instance of <see cref="BasicResponse" />.
        /// </returns>
        public Task<BasicResponse> EnterStandByMode()
        {
            // TODO: Implement to wake up
            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.EnterStandbyMode));
        }

        /// <summary>
        /// Sets the device's security level asynchronous. 1 is the lowest security level, 5 is the highest security level.
        /// </summary>
        /// <param name="level">The security level.</param>
        /// <returns>A task that represents the asynchronous set security level operation.
        /// The result of the task contains an instance of <see cref="BasicResponse" />.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">level</exception>
        public Task<BasicResponse> SetSecurityLevelAsync(int level)
        {
            if (level < 1 || level > 5)
                throw new ArgumentOutOfRangeException($"{nameof(level)} must be a number between 1 and 5.");

            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.SetSecurityLevel, level));
        }

        /// <summary>
        /// Gets the device's security level asynchronous. 1 is the lowest security level, 5 is the highest security level.
        /// </summary>
        /// <returns>A task that represents the asynchronous get security level operation.
        /// The result of the task contains an instance of <see cref="GetSecurityLevelResponse" />.
        /// </returns>
        public Task<GetSecurityLevelResponse> GetSecurityLevelAsync() =>
            GetResponseAsync<GetSecurityLevelResponse>(Command.Create(CommandCode.GetSecurityLevel));

        #endregion

        #region Private Functions

        /// <summary>
        /// Opens and initialize the fingerprint device at the specified port name with the especified baudrate.
        /// </summary>
        /// <param name="portName">Name of the port.</param>
        /// <param name="baudrate">The baudrate.</param>
        /// <returns>A task that represents the asynchronous open operation.</returns>
        /// <exception cref="Exception">The device could not be initialized.</exception>
        private async Task Open(string portName, int baudrate)
        {
            _serialPort = new SerialPort(portName, baudrate, Parity.None, 8, StopBits.One);
            _serialPort.Open();
            await Task.Delay(100);

            if (baudrate != TargetBaudRate)
            {
                // Chage baudrate to target baudrate for better performance
                await SetBaudrateAsync(TargetBaudRate);
            }
            else
            {
                _deviceInfo = await OpenDeviceAsync();
                if (!_deviceInfo.IsSuccessful)
                    throw new Exception("The device could not be initialized.");

                await TurnLedOnAsync();
            }
        }

        /// <summary>
        /// Opens and initializes the device asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous open device operation.
        /// The result of the task contains an instance of <see cref="InitializationResponse"/>. 
        /// </returns>
        private Task<InitializationResponse> OpenDeviceAsync() =>
            GetResponseAsync<InitializationResponse>(Command.Create(CommandCode.Open, 1));

        /// <summary>
        /// Closes the device asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous close device operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        private Task<BasicResponse> CloseDeviceAsync() =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.Close));

        /// <summary>
        /// Sets the baudrate asynchronous.
        /// This closes and re-opens the device.
        /// </summary>
        /// <param name="baudrate">The baudrate.</param>
        /// <returns>A task that represents the asynchronous set baudrate operation.
        /// The result of the task contains an instance of <see cref="BasicResponse"/>. 
        /// </returns>
        private async Task<BasicResponse> SetBaudrateAsync(int baudrate)
        {
            var response = await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.ChangeBaudRate, baudrate));

            // It is possible that we don't have a response when changing baudrate 
            // because we are still listening with the previous config. 
            // If this happens we'll have a communication error response (CommErr)
            if (response.IsSuccessful || response.ErrorCode == ErrorCode.CommErr)
            {
                var portName = _serialPort.PortName;
                await Close();
                await Open(portName, baudrate);
            }

            return response;
        }

        /// <summary>
        /// Enroll stage asynchronous.
        /// </summary>
        /// <param name="iteration">The iteration.</param>
        /// <param name="userId">The user identifier.</param>
        /// <returns>A task that represents the asynchronous enroll operation.
        /// The result of the task contains an instance of <see cref="EnrollmentResponse"/>. 
        /// </returns>
        private async Task<EnrollmentResponse> EnrollAsync(int iteration, int userId)
        {
            var enrollmentFingerActionTimeOut = TimeSpan.FromSeconds(10);

            var captureResult = await CaptureFingerprintPatternAsync<EnrollmentResponse>(enrollmentFingerActionTimeOut);
            if (!captureResult.IsSuccessful)
                return captureResult;

            var cmd = iteration == 1 ? CommandCode.Enroll1 :
                                       iteration == 2 ? CommandCode.Enroll2 :
                                                        CommandCode.Enroll3;

            return await GetResponseAsync<EnrollmentResponse>(Command.Create(cmd, userId), EnrollTimeout);
        }

        /// <summary>
        /// Capture fingerprint pattern asynchronous. A special pattern for capture a fingerprint from the device.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <returns>A task that represents the asynchronous capture fingerprint pattern operation.
        /// The result of the task contains an instance of a response of type T.
        /// </returns>
        private Task<T> CaptureFingerprintPatternAsync<T>()
            where T : ResponseBase => CaptureFingerprintPatternAsync<T>(FingerActionTimeout);

        /// <summary>
        /// Capture fingerprint pattern asynchronous. A special pattern for capture a fingerprint from the device.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <param name="fingerActionTimeout">The finger action timeout.</param>
        /// <returns>A task that represents the asynchronous capture fingerprint pattern operation.
        /// The result of the task contains an instance of a response of type T.
        /// </returns>
        private async Task<T> CaptureFingerprintPatternAsync<T>(TimeSpan fingerActionTimeout)
            where T : ResponseBase
        {
            var actionPerformed = await WaitFingerActionAsync(FingerAction.Place, fingerActionTimeout);
            if (!actionPerformed)
                return ResponseBase.GetUnsuccessfulResponse<T>(ErrorCode.FingerNotPressed);

            var captureResult = await CaptureFingerprintAsync();

            if (typeof(T) == captureResult.GetType())
                return captureResult as T;

            return Activator.CreateInstance(typeof(T), captureResult.Payload) as T;
        }

        /// <summary>
        /// Captures a fingerprint from the device asynchronous.
        /// </summary>
        /// <returns>A task that represents the asynchronous capture fingerprint operation.
        /// The result of the task contains an instance of <see cref="BasicResponse" />.
        /// </returns>
        private Task<BasicResponse> CaptureFingerprintAsync() =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.CaptureFinger));

        #endregion

        #region Write-Read

        /// <summary>
        /// Sends a command to the device and gets the device's response asynchronous.
        /// </summary>
        /// <typeparam name="T">A final response type.</typeparam>
        /// <param name="command">The command object to send.</param>
        /// <returns>A task that represents the asynchronous get response operation.
        /// The result of the task contains an instance of a response type T.
        /// </returns>
        private Task<T> GetResponseAsync<T>(Command command)
            where T : ResponseBase => GetResponseAsync<T>(command, DefaultTimeout);

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
        private async Task<T> GetResponseAsync<T>(Command command, TimeSpan responseTimeout, CancellationToken ct = default)
                    where T : ResponseBase
        {
            var expectedResponseLength = PacketBase.BasePacketLenght;
            if (ResponseBase.ResponseDataLength.ContainsKey(command.CommandCode))
            {
                expectedResponseLength += 6 + ResponseBase.ResponseDataLength[command.CommandCode];

                // Special cases
                if ((command.CommandCode == CommandCode.Open && command.Parameter == 0) ||
                    (command.CommandCode == CommandCode.Enroll3 && command.Parameter != -1))
                    expectedResponseLength = PacketBase.BasePacketLenght;
            }

            _serialPortDone.Wait(ct);
            _serialPortDone.Reset();
            try
            {
                await WriteAsync(command.Payload, ct);
                var response = await ReadAsync(expectedResponseLength, DefaultTimeout, ct);
                if (response == null || response.Length == 0)
                    return ResponseBase.GetUnsuccessfulResponse<T>(ErrorCode.CommErr);

                var responsePkt = Activator.CreateInstance(typeof(T), response) as T;

                if (command.HasDataPacket && responsePkt?.IsSuccessful == true)
                {
                    await WriteAsync(command.DataPacket.Payload, ct);
                    response = await ReadAsync(expectedResponseLength, DefaultTimeout, ct);
                    if (response == null || response.Length == 0)
                        return ResponseBase.GetUnsuccessfulResponse<T>(ErrorCode.CommErr);

                    responsePkt = Activator.CreateInstance(typeof(T), response) as T;
                }

                return responsePkt;
            }
            finally
            {
                _serialPortDone.Set();
            }
        }

        /// <summary>
        /// Writes data to the serial port asynchronous.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        private async Task WriteAsync(byte[] payload, CancellationToken ct = default)
        {
            if (_serialPort == null || _serialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(Open)} method before attempting communication");

            await _serialPort.BaseStream.WriteAsync(payload, 0, payload.Length, ct);
            await _serialPort.BaseStream.FlushAsync(ct);
        }

        /// <summary>
        /// Reads data from the serial port asynchronous.
        /// </summary>
        /// <param name="expectedResponseLength">Expected length of the response.</param>
        /// <param name="timeout">The timeout.</param>
        /// <param name="ct">An instance of <see cref="CancellationToken"/></param>
        /// <returns>A task that represents the asynchronous read operation.</returns>
        private async Task<byte[]> ReadAsync(int expectedResponseLength, TimeSpan timeout, CancellationToken ct = default)
        {
            if (_serialPort == null || _serialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(Open)} method before attempting communication");

            var data = new List<byte>();
            var readed = new byte[1024];
            var startTime = DateTime.Now;

            while (data.Count < expectedResponseLength || _serialPort.BytesToRead > 0)
            {
                if (_serialPort.BytesToRead > 0)
                {
                    var bytesRead = await _serialPort.BaseStream.ReadAsync(readed, 0, readed.Length, ct);
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
                Close().Wait();
                _serialPortDone.Dispose();
            }

            _serialPortDone = null;
            _disposedValue = true;
        }

        #endregion
    }
}
