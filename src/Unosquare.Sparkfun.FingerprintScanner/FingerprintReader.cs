namespace Unosquare.Sparkfun.FingerprintScanner
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
        private const int InitialBaudRate = 9600;
        private const int TargetBaudRate = 115200;

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan EnrollTimeout = TimeSpan.FromSeconds(5);

        private readonly int FingerprintCapacity;
        private static ManualResetEventSlim _serialPortDone = new ManualResetEventSlim(true);
        private SerialPort _serialPort;

        public FingerprintReader(FingerprintReaderModel model)
        {
            FingerprintCapacity = (int)model;
        }

        public int FirmwareVErsion { get; private set; }

        public int IsoAreaMaxSize { get; private set; }

        public byte[] RawSerialNumber { get; private set; }

        public string SerialNumber => string.Join(":", RawSerialNumber.Select(x => x.ToString("X2")));

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
                await SetLetStatus(LedStatus.Off);
                await GetResponseAsync(CommandCode.Close, 0);
                _serialPort.Close();
                await Task.Delay(200);
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
                await SetBaudrate(TargetBaudRate);
            }
            else
            {
                await GetResponseAsync(CommandCode.Open, 1);
                await SetLetStatus(LedStatus.On);
            }
        }

        #region Commands

        public async Task<ResponsePacket> SetBaudrate(int baudrate)
        {
            var response = await GetResponseAsync(CommandCode.ChangeBaudRate, TargetBaudRate);

            // TODO: Handle in a better way baudrate changes response.
            // There are no Ack response when changing baudrate
            //if (response == null || response.IsSuccessful)
            {
                var portName = _serialPort.PortName;

                $"Closing port at {InitialBaudRate}...".Info();
                await Close();
                await Open(portName, TargetBaudRate);
            }

            return response;
        }

        public Task<ResponsePacket> SetLetStatus(LedStatus status)
        {
            return GetResponseAsync(CommandCode.CmosLed, (int)status);
        }

        public async Task<ResponsePacket> AddFingerprint(int iteration, int userId, int userPrivilege)
        {
            if (iteration < 0 || iteration > 3)
                throw new ArgumentException($"{nameof(iteration)} must be a number between 1 and 3");
            if (userId < 1 || userId > 2999)
                throw new ArgumentException($"{nameof(userId)} must be a number between 1 and 2999");
            if (userPrivilege < 0 || userPrivilege > 3)
                throw new ArgumentException($"{nameof(userPrivilege)} must be a number between 1 and 3");

            if (iteration == 1)
            {
                // Start enrollment
                var startResult = await GetResponseAsync(CommandCode.EnrollStart, userId);
                if (!startResult.IsSuccessful)
                    return startResult;
            }

            var result = await Enroll(iteration);

            if (iteration == 3 && result.IsSuccessful)
            {
                result = await GetResponseAsync(CommandCode.SetSecurityLevel, userPrivilege);
            }

            return result;
        }

        private async Task<ResponsePacket> Enroll(int iteration)
        {
            var placeFingerTimeOut = TimeSpan.FromSeconds(10);

            var actionPerformed = await WaitFingerAction(FingerAction.Place, placeFingerTimeOut);
            if (!actionPerformed)
                return null;

            var result = await GetResponseAsync(CommandCode.CaptureFinger);
            if (!result.IsSuccessful) return result;

            var cmd = iteration == 1 ? CommandCode.Enroll1 :
                                       iteration == 2 ? CommandCode.Enroll2 :
                                                        CommandCode.Enroll3;

            return await GetResponseAsync(cmd, 0, EnrollTimeout);
        }

        public async Task<MatchOneToNResponse> MatchOneToN(CancellationToken ct)
        {
            var actionPerformed = await WaitFingerAction(FingerAction.Place);
            if (!actionPerformed)
                return null;

            var result = await GetResponseAsync(CommandCode.CaptureFinger, 0, DefaultTimeout, ct);
            if (!result.IsSuccessful)
                return MatchOneToNResponse.UnsuccessResponse();

            result = await GetResponseAsync(CommandCode.Identify, 0, DefaultTimeout, ct);
            if (!result.IsSuccessful)
                return MatchOneToNResponse.UnsuccessResponse();

            var userId = result.Parameter;

            result = await GetResponseAsync(CommandCode.GetSecurityLevel, 0, DefaultTimeout, ct);
            if (!result.IsSuccessful)
                return MatchOneToNResponse.UnsuccessResponse();

            var securityLevel = result.Parameter;

            return new MatchOneToNResponse(userId, securityLevel);
        }

        public async void GetUserData(int userId)
        {

        }

        public async Task<ResponsePacket> SetUserProperties(int userId, int securityLevel, byte[] template)
        {
            // TODO: Implement function
            throw new NotImplementedException();
        }

        public Task<ResponsePacket> DeleteAllUsers()
        {
            return GetResponseAsync(CommandCode.DeleteAll);
        }

        public Task<ResponsePacket> DeleteUser(int userId)
        {
            return GetResponseAsync(CommandCode.DeleteID, userId);
        }

        public Task<ResponsePacket> CheckEnrolled(int userId)
        {
            return GetResponseAsync(CommandCode.CheckEnrolled, userId);
        }

        public Task<bool> WaitFingerAction(FingerAction action) => WaitFingerAction(action, DefaultTimeout);

        public async Task<bool> WaitFingerAction(FingerAction action, TimeSpan timeout)
        {
            var startTime = DateTime.Now;
            while (true)
            {
                var result = await GetResponseAsync(CommandCode.IsPressFinger);
                if (!result.IsSuccessful)
                    return false;

                if ((action == FingerAction.Place && result.Parameter == 0) ||
                    (action == FingerAction.Remove && result.Parameter != 0))
                    return true;

                if (DateTime.Now.Subtract(startTime) > timeout)
                    return false;

                Thread.Sleep(10);
            }
        }

        #endregion

        #region Write-Read

        private Task<ResponsePacket> GetResponseAsync(CommandCode command, int parameter = 0)
        {
            return GetResponseAsync(command, parameter, DefaultTimeout);
        }

        private async Task<ResponsePacket> GetResponseAsync(CommandCode command, int parameter, TimeSpan responseTimeout, CancellationToken ct = default(CancellationToken))
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

            //$"FPR Command: {command}({(ushort)command:X2})".Info();

            await WriteAsync(payload, ct);

            var readData = await ReadAsync(DefaultTimeout, ct);
            if (readData == null)
                return ResponsePacket.ErrorPacket();

            return ResponsePacket.FromByteArray(readData);
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

                while (data.Count == 0 || _serialPort.BytesToRead > 0)
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
