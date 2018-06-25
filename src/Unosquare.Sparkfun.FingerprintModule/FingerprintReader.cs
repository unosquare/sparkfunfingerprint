namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;
    using System.IO.Ports;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

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

        #endregion

        #region Constructor
        
        public FingerprintReader(FingerprintReaderModel model)
        {
            FingerprintCapacity = (int)model - 1;
        }

        #endregion

        #region Properties

        public int FingerprintCapacity { get; }

        public string FirmwareVersion => _deviceInfo?.FirmwareVersion ?? InitializationResponse.NoInfo;

        public string SerialNumber => _deviceInfo?.SerialNumber ?? InitializationResponse.NoInfo;

        public int IsoAreaMaxSize => _deviceInfo?.IsoAreaMaxSize ?? -1;

        #endregion

        #region Open-Close

        public Task Open(string portName)
        {
            if (_serialPort != null)
                throw new InvalidOperationException("Device is already open. Call the close method first.");

            return Open(portName, InitialBaudRate);
        }

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

        #endregion

        #region Commands

        private Task<InitializationResponse> OpenDeviceAsync() => 
            GetResponseAsync<InitializationResponse>(Command.Create(CommandCode.Open, 1));

        private Task<BasicResponse> CloseDeviceAsync() => 
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.Close));

        public Task<BasicResponse> SetLetStatusAsync(LedStatus status)
        {
            if (status == LedStatus.On)
                return TurnLedOnAsync();
            else
                return TurnLedOffAsync();
        }

        public Task<BasicResponse> TurnLedOnAsync() => 
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.CmosLed, 1));
        
        public Task<BasicResponse> TurnLedOffAsync() => 
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.CmosLed));
        
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

        public Task<FastSearchingResponse> FastDeviceSearching() => 
            GetResponseAsync<FastSearchingResponse>(Command.Create(CommandCode.UsbInternalCheck));
        
        public Task<CountEnrolledFingerprintResponse> CountEnrolledFingerprintAsync() => 
            GetResponseAsync<CountEnrolledFingerprintResponse>(Command.Create(CommandCode.GetEnrollCount));
        
        public Task<CheckEnrollmentResponse> CheckEnrollmentStatusAsync(int userId) =>
            GetResponseAsync<CheckEnrollmentResponse>(Command.Create(CommandCode.CheckEnrolled, userId));

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

        public Task<bool> WaitFingerActionAsync(FingerAction action) => WaitFingerActionAsync(action, FingerActionTimeout);

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

        public Task<CheckFingerPressingResponse> CheckFingerPressingStatusAsync() => 
            GetResponseAsync<CheckFingerPressingResponse>(Command.Create(CommandCode.IsPressFinger));

        public Task<BasicResponse> DeleteAllUsersAsync() =>
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.DeleteAll));

        public Task<BasicResponse> DeleteUserAsync(int userId)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.DeleteID, userId));
        }

        public async Task<BasicResponse> MatchOneToOneAsync(int userId)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            var captureResult = await CaptureFingerprintPatternAsync<BasicResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.Verify, userId));
        }

        public async Task<BasicResponse> MatchOneToOneAsync(int userId, byte[] template)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            var captureResult = await CaptureFingerprintPatternAsync<BasicResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<BasicResponse>(Command.Create(CommandCode.VerifyTemplate, userId, template));
        }

        public async Task<MatchOneToNResponse> MatchOneToN()
        {
            var captureResult = await CaptureFingerprintPatternAsync<MatchOneToNResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<MatchOneToNResponse>(Command.Create(CommandCode.Identify));
        }

        public async Task<MatchOneToNResponse> MatchOneToN(byte[] template)
        {
            var captureResult = await CaptureFingerprintPatternAsync<MatchOneToNResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<MatchOneToNResponse>(Command.Create(CommandCode.IdentifyTemplate,0,template));
        }

        public async Task<MatchOneToNResponse> MatchOneToN2(byte[] template)
        {
            var captureResult = await CaptureFingerprintPatternAsync<MatchOneToNResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<MatchOneToNResponse>(Command.Create(CommandCode.IdentifyTemplate2, 500,template));
        }
        
        public async Task<TemplateResponse> MakeTemplateAsync()
        {
            var captureResult = await CaptureFingerprintPatternAsync<TemplateResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<TemplateResponse>(Command.Create(CommandCode.MakeTemplate));
        }

        public async Task<GetFingerprintImageResponse> GetImageAsync()
        {
            var captureResult = await CaptureFingerprintPatternAsync<GetFingerprintImageResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<GetFingerprintImageResponse>(Command.Create(CommandCode.GetImage));
        }

        public async Task<GetRawImageResponse> GetRawImageAsync()
        {
            var captureResult = await CaptureFingerprintPatternAsync<GetRawImageResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<GetRawImageResponse>(Command.Create(CommandCode.GetRawImage));
        }

        private Task<T> CaptureFingerprintPatternAsync<T>()
            where T : ResponseBase => CaptureFingerprintPatternAsync<T>(FingerActionTimeout);

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

        private Task<BasicResponse> CaptureFingerprintAsync() => 
            GetResponseAsync<BasicResponse>(Command.Create(CommandCode.CaptureFinger));

        public Task<TemplateResponse> GetTemplateAsync(int userId)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<TemplateResponse>(Command.Create(CommandCode.GetTemplate, userId));
        }

        public Task<BasicResponse> SetTemplateAsync(int userId, byte[] template)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.SetTemplate, userId,template));
        }

        public Task<BasicResponse> EnterStandByMode()
        {
            // TODO: Implement to wake up
            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.EnterStandbyMode));
        }

        public Task<BasicResponse> SetSecurityLevelAsync(int level)
        {
            if (level < 1 || level > 5)
                throw new ArgumentOutOfRangeException($"{nameof(level)} must be a number between 1 and 5.");

            return GetResponseAsync<BasicResponse>(Command.Create(CommandCode.SetSecurityLevel, level));
        }

        public Task<GetSecurityLevelResponse> GetSecurityLevelAsync() => 
            GetResponseAsync<GetSecurityLevelResponse>(Command.Create(CommandCode.GetSecurityLevel));

        #endregion

        #region Write-Read

        private Task<T> GetResponseAsync<T>(Command command)
            where T : ResponseBase => GetResponseAsync<T>(command, DefaultTimeout);

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

        private async Task WriteAsync(byte[] payload, CancellationToken ct = default)
        {
            if (_serialPort == null || _serialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(Open)} method before attempting communication");

            _serialPortDone.Wait(ct);
            _serialPortDone.Reset();

            try
            {
                await _serialPort.BaseStream.WriteAsync(payload, 0, payload.Length, ct);
                await _serialPort.BaseStream.FlushAsync(ct);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                _serialPortDone.Set();
            }
        }

        private async Task<byte[]> ReadAsync(int expectedResponseLength, TimeSpan timeout, CancellationToken ct = default)
        {
            if (_serialPort == null || _serialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(Open)} method before attempting communication");

            _serialPortDone.Wait(ct);
            _serialPortDone.Reset();

            try
            {
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
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                _serialPortDone.Set();
            }
        }

        #endregion

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

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

        public void Dispose() => Dispose(true);

        #endregion
    }
}
