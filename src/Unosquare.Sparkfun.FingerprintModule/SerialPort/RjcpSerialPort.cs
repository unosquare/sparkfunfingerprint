#if !NET452
namespace Unosquare.Sparkfun.FingerprintModule.SerialPort
{
    using RJCP.IO.Ports;
    using Resources;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal class RjcpSerialPort : ISerialPort
    {
        private readonly SerialPortStream _serialPort;

        static RjcpSerialPort()
        {
            if (Utils.Runtime.OS != Utils.OperatingSystem.Windows)
                EmbeddedResources.ExtractAll();
        }

        public RjcpSerialPort(string portName, int baudRate)
        {
            _serialPort = new SerialPortStream(portName, baudRate, 8, Parity.None, StopBits.One);
        }

        public string PortName => _serialPort?.PortName;

        public bool IsOpen => _serialPort?.IsOpen == true;

        public int BytesToRead => _serialPort?.BytesToRead ?? throw new InvalidOperationException("Serial port is not open.");

        public static string[] GetPortNames() => SerialPortStream.GetPortNames();

        public void Open() => _serialPort?.Open();

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _serialPort?.WriteAsync(buffer, offset, count, cancellationToken);

        public Task FlushAsync(CancellationToken cancellationToken) =>
            _serialPort?.FlushAsync(cancellationToken);

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            _serialPort?.ReadAsync(buffer, offset, count, cancellationToken);

        public void Close() =>_serialPort?.Close();

        void IDisposable.Dispose() =>_serialPort?.Dispose();
    }
}
#endif