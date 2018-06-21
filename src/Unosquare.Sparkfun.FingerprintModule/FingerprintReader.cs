namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;
    using System.IO.Ports;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Unosquare.Swan;

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

        public int FingerprintCapacity { get; private set; }

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

        private Task<InitializationResponse> OpenDeviceAsync()
        {
            return GetResponseAsync<InitializationResponse>(CommandCode.Open, 1);
        }

        private Task<BasicResponse> CloseDeviceAsync()
        {
            return GetResponseAsync<BasicResponse>(CommandCode.Close);
        }

        public Task<BasicResponse> SetLetStatusAsync(LedStatus status)
        {
            if (status == LedStatus.On)
                return TurnLedOnAsync();
            else
                return TurnLedOffAsync();
        }

        public Task<BasicResponse> TurnLedOnAsync()
        {
            return GetResponseAsync<BasicResponse>(CommandCode.CmosLed, 1);
        }

        public Task<BasicResponse> TurnLedOffAsync()
        {
            return GetResponseAsync<BasicResponse>(CommandCode.CmosLed);
        }

        private async Task<BasicResponse> SetBaudrateAsync(int baudrate)
        {
            var response = await GetResponseAsync<BasicResponse>(CommandCode.ChangeBaudRate, baudrate);

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

        public Task<FastSearchingResponse> FastDeviceSearching()
        {
            return GetResponseAsync<FastSearchingResponse>(CommandCode.UsbInternalCheck);
        }

        public Task<CountEnrolledFingerprintResponse> CountEnrolledFingerprintAsync()
        {
            return GetResponseAsync<CountEnrolledFingerprintResponse>(CommandCode.GetEnrollCount);
        }

        public Task<CheckEnrollmentResponse> CheckEnrollmentStatusAsync(int userId)
        {
            return GetResponseAsync<CheckEnrollmentResponse>(CommandCode.CheckEnrolled, userId);
        }

        public async Task<EnrollmentResponse> EnrollUserAsync(int iteration, int userId)
        {
            if (iteration < 0 || iteration > 3)
                throw new ArgumentOutOfRangeException($"{nameof(iteration)} must be a number between 1 and 3");

            if (userId < -1 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between -1 and {FingerprintCapacity}.");

            if (iteration == 1)
            {
                // Start enrollment
                var startResult = await GetResponseAsync<BasicResponse>(CommandCode.EnrollStart, userId);
                if (!startResult.IsSuccessful)
                    return ResponseBase.GetUnsuccessfulResponse<EnrollmentResponse>(startResult.ErrorCode);
            }

            return await EnrollAsync(iteration);
        }

        private async Task<EnrollmentResponse> EnrollAsync(int iteration)
        {
            var enrollmentFingerActionTimeOut = TimeSpan.FromSeconds(10);

            var captureResult = await CaptureFingerprintPatternAsync<EnrollmentResponse>(enrollmentFingerActionTimeOut);
            if (!captureResult.IsSuccessful)
                return captureResult;

            var cmd = iteration == 1 ? CommandCode.Enroll1 :
                                       iteration == 2 ? CommandCode.Enroll2 :
                                                        CommandCode.Enroll3;

            return await GetResponseAsync<EnrollmentResponse>(cmd, 0, EnrollTimeout);
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

        public Task<CheckFingerPressingResponse> CheckFingerPressingStatusAsync()
        {
            return GetResponseAsync<CheckFingerPressingResponse>(CommandCode.IsPressFinger);
        }

        public Task<BasicResponse> DeleteAllUsersAsync()
        {
            return GetResponseAsync<BasicResponse>(CommandCode.DeleteAll);
        }

        public Task<BasicResponse> DeleteUserAsync(int userId)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<BasicResponse>(CommandCode.DeleteID, userId);
        }

        public async Task<BasicResponse> MatchOneToOneAsync(int userId)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            var captureResult = await CaptureFingerprintPatternAsync<BasicResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<BasicResponse>(CommandCode.Verify, userId);
        }

        public async Task<BasicResponse> MatchOneToOneAsync(int userId, byte[] template)
        {
            // TODO: Implement data packet commands
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            var captureResult = await CaptureFingerprintPatternAsync<BasicResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<BasicResponse>(CommandCode.VerifyTemplate, userId);
        }

        public async Task<MatchOneToNResponse> MatchOneToN()
        {
            var captureResult = await CaptureFingerprintPatternAsync<MatchOneToNResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<MatchOneToNResponse>(CommandCode.Identify);
        }

        public async Task<MatchOneToNResponse> MatchOneToN(byte[] template)
        {
            // TODO: Implement data packet commands
            var captureResult = await CaptureFingerprintPatternAsync<MatchOneToNResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<MatchOneToNResponse>(CommandCode.IdentifyTemplate);
        }

        public async Task<MatchOneToNResponse> MatchOneToN2(byte[] template)
        {
            // TODO: Implement data packet commands
            var captureResult = await CaptureFingerprintPatternAsync<MatchOneToNResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<MatchOneToNResponse>(CommandCode.IdentifyTemplate2, 500);
        }
        
        public async Task<TemplateResponse> MakeTemplateAsync()
        {
            var captureResult = await CaptureFingerprintPatternAsync<TemplateResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<TemplateResponse>(CommandCode.MakeTemplate);
        }

        public async Task<GetFingerprintImageResponse> GetImageAsync()
        {
            var captureResult = await CaptureFingerprintPatternAsync<GetFingerprintImageResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<GetFingerprintImageResponse>(CommandCode.GetImage);
        }

        public async Task<GetRawImageResponse> GetRawImageAsync()
        {
            var captureResult = await CaptureFingerprintPatternAsync<GetRawImageResponse>();
            if (!captureResult.IsSuccessful)
                return captureResult;

            return await GetResponseAsync<GetRawImageResponse>(CommandCode.GetRawImage);
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

        private Task<BasicResponse> CaptureFingerprintAsync()
        {
            return GetResponseAsync<BasicResponse>(CommandCode.CaptureFinger);
        }

        public Task<TemplateResponse> GetTemplateAsync(int userId)
        {
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<TemplateResponse>(CommandCode.GetTemplate, userId);
        }

        public Task<BasicResponse> SetTemplateAsync(int userId, byte[] template)
        {
            // TODO: Implement data packet commands
            if (userId < 0 || userId > FingerprintCapacity)
                throw new ArgumentOutOfRangeException($"{nameof(userId)} must be a number between 0 and {FingerprintCapacity}.");

            return GetResponseAsync<BasicResponse>(CommandCode.SetTemplate, userId);
        }

        public Task<BasicResponse> EnterStandByMode()
        {
            // TODO: Implement to wake up
            return GetResponseAsync<BasicResponse>(CommandCode.EnterStandbyMode);
        }

        public Task<BasicResponse> SetSecurityLevelAsync(int level)
        {
            if (level < 1 || level > 5)
                throw new ArgumentOutOfRangeException($"{nameof(level)} must be a number between 1 and 5.");

            return GetResponseAsync<BasicResponse>(CommandCode.SetSecurityLevel, level);
        }

        public Task<GetSecurityLevelResponse> GetSecurityLevelAsync()
        {
            return GetResponseAsync<GetSecurityLevelResponse>(CommandCode.GetSecurityLevel);
        }

        #endregion

        #region Write-Read

        private Task<T> GetResponseAsync<T>(CommandCode command, int parameter = 0)
            where T : ResponseBase
        {
            return GetResponseAsync<T>(command, parameter, DefaultTimeout);
        }

        private async Task<T> GetResponseAsync<T>(CommandCode command, int parameter, TimeSpan responseTimeout, CancellationToken ct = default(CancellationToken))
            where T : ResponseBase
        {
            var header = new byte[] { 0x55, 0xAA, 0x01, 0x00 };
            var cmd = BitConverter.GetBytes((UInt16)command);
            var param = BitConverter.GetBytes(parameter);

            var crc = BitConverter.GetBytes(
                        Convert.ToUInt16(
                                header.Sum(d => Convert.ToUInt32(d)) +
                                cmd.Sum(d => Convert.ToUInt32(d)) +
                                param.Sum(d => Convert.ToUInt32(d))));

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(cmd);
                Array.Reverse(param);
                Array.Reverse(crc);
            }

            var payload = header.Concat(param).Concat(cmd).Concat(crc).ToArray();
            
            await WriteAsync(payload, ct);

            var response = await ReadAsync(DefaultTimeout, ct);
            if (response == null || response.Length == 0)
                return ResponseBase.GetUnsuccessfulResponse<T>(ErrorCode.CommErr);

            return Activator.CreateInstance(typeof(T), response) as T;
        }

        private async Task WriteAsync(byte[] payload, CancellationToken ct = default(CancellationToken))
        {
            if (_serialPort == null || _serialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(Open)} method before attempting communication");

            _serialPortDone.Wait();
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

        private async Task<byte[]> ReadAsync(TimeSpan timeout, CancellationToken ct = default(CancellationToken))
        {
            if (_serialPort == null || _serialPort.IsOpen == false)
                throw new InvalidOperationException($"Call the {nameof(Open)} method before attempting communication");

            _serialPortDone.Wait();
            _serialPortDone.Reset();

            try
            {
                var data = new List<byte>();
                var readed = new byte[1024];
                var startTime = DateTime.Now;

                while (data.Count < ResponseBase.BaseResponseLenght || _serialPort.BytesToRead > 0)
                {
                    if (_serialPort.BytesToRead > 0)
                    {
                        var bytesRead = await _serialPort.BaseStream.ReadAsync(readed, 0, readed.Length, ct);
                        if (bytesRead > 0)
                            data.AddRange(readed.Take(bytesRead));
                    }

                    if (DateTime.Now.Subtract(startTime) > timeout)
                        return null;

                    await Task.Delay(10);
                }

                $"Read: {String.Join(" ", data.Select(d => d.ToString("X2")))}".Info();
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
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close().Wait();
                    _serialPortDone.Dispose();
                }

                _serialPortDone = null;
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
