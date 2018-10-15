#if NET452
namespace Unosquare.Sparkfun.FingerprintModule.SerialPort
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.IO.Ports;

    internal class MsSerialPort : ISerialPort
    {
        private readonly SerialPort _serialPort;

        public MsSerialPort(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        }

        public string PortName => _serialPort?.PortName;

        public bool IsOpen => _serialPort?.IsOpen == true;

        public int BytesToRead => _serialPort?.BytesToRead ?? throw new InvalidOperationException("Serial port is not open.");

        public static string[] GetPortNames() => SerialPort.GetPortNames();

        public void Open() => _serialPort?.Open();

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _serialPort?.BaseStream.WriteAsync(buffer, offset, count, cancellationToken);

        public Task FlushAsync(CancellationToken cancellationToken) =>
            _serialPort?.BaseStream.FlushAsync(cancellationToken);

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _serialPort?.BaseStream.ReadAsync(buffer, offset, count, cancellationToken);

        public void Close() => _serialPort?.Close();

        void IDisposable.Dispose() => _serialPort?.Dispose();
    }
}
#endif