namespace Unosquare.Sparkfun.FingerprintModule.SerialPort
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal interface ISerialPort : IDisposable
    {
        string PortName { get; }

        bool IsOpen { get; }

        int BytesToRead { get; }

        void Open();

        void Close();

        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        Task FlushAsync(CancellationToken cancellationToken);

        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}
