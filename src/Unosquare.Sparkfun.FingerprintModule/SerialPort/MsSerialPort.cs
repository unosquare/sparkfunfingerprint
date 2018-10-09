#if NET452
namespace Unosquare.Sparkfun.FingerprintModule.SerialPort
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.IO.Ports;

    internal class MsSerialPort : ISerialPort
    {
        private SerialPort _serialPort;

        public MsSerialPort(string portName, int baudRate)
        {
            PortName = portName;

            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public string PortName { get; }
        public bool IsOpen { get; }
        public int BytesToRead { get; }
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