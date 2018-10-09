#if !NET452
namespace Unosquare.Sparkfun.FingerprintModule.SerialPort
{
    using RJCP.IO.Ports;
    using Resources;
    using System;
    using System.Collections.Generic;
    using System.Text;
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
            PortName = portName;

            _serialPort = new SerialPortStream(portName, baudRate, 8, Parity.None, StopBits.One);
        }

        public void Dispose()
        {
            _serialPort?.Dispose();
        }

        public string PortName { get; }

        public bool IsOpen => _serialPort?.IsOpen ?? throw new InvalidOperationException("Serial Port is not initialized");

        public int BytesToRead => _serialPort?.BytesToRead ?? throw new InvalidOperationException("Serial Port is not initialized");

        public void Open()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task FlushAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
#endif