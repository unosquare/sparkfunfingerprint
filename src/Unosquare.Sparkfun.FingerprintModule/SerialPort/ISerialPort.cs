namespace Unosquare.Sparkfun.FingerprintModule.SerialPort
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <inheritdoc />
    /// <summary>
    /// Interface to wrap any Serial Port implementation.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface ISerialPort : IDisposable
    {
        /// <summary>
        /// Gets the name of the port.
        /// </summary>
        /// <value>
        /// The name of the port.
        /// </value>
        string PortName { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is open.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is open; otherwise, <c>false</c>.
        /// </value>
        bool IsOpen { get; }

        /// <summary>
        /// Gets the bytes to read.
        /// </summary>
        /// <value>
        /// The bytes to read.
        /// </value>
        int BytesToRead { get; }

        /// <summary>
        /// Opens this instance.
        /// </summary>
        void Open();

        /// <summary>
        /// Closes this instance.
        /// </summary>
        void Close();

        /// <summary>
        /// Writes the asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the write action.</returns>
        Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);

        /// <summary>
        /// Flushes the asynchronous.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the flush action.</returns>
        Task FlushAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Reads the asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the count of bytes from the read.</returns>
        Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken);
    }
}
