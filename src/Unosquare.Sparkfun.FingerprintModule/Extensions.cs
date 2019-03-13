namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Computes the checksum of the given payload.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <returns>A <see cref="ushort"/> value representing the computed CRC.</returns>
        /// <exception cref="ArgumentException">payload.</exception>
        internal static ushort ComputeChecksum(this IList<byte> payload)
        {
            if (payload == null || payload.Count == 0)
                throw new ArgumentException($"'{nameof(payload)}' must not be empty.");

            return payload.ComputeChecksum(0, payload.Count - 1);
        }

        /// <summary>
        /// Computes the checksum of the given payload.
        /// </summary>
        /// <param name="payload">The payload.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns>A <see cref="ushort"/> value representing the computed CRC.</returns>
        /// <exception cref="ArgumentException">payload.</exception>
        internal static ushort ComputeChecksum(this IList<byte> payload, int startIndex, int endIndex)
        {
            if (payload == null || payload.Count < endIndex + 1)
                throw new ArgumentException($"'{nameof(payload)}' hast to be at least {endIndex + 1} bytes long.");

            ushort checksum = payload[startIndex];
            for (var i = startIndex + 1; i <= endIndex; i++)
            {
                checksum = (ushort)(checksum + payload[i]);
            }

            return checksum;
        }

        /// <summary>
        /// Validates the checksum for a byte array.
        /// </summary>
        /// <param name="payload">The byte array.</param>
        /// <returns>A <see cref="bool"/> indicating if the CRC is valid. 
        /// <c>true</c> if the CRC is valid; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">payload.</exception>
        internal static bool ValidateChecksum(this byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                throw new ArgumentException($"'{nameof(payload)}' must not be empty.");

            return payload.ValidateChecksum(0, payload.Length - 1);
        }

        /// <summary>
        /// Validates the checksum for a byte array.
        /// </summary>
        /// <param name="payload">The byte array.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns>A <see cref="bool"/> indicating if the CRC is valid. 
        /// <c>true</c> if the CRC is valid; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentException">payload.</exception>
        internal static bool ValidateChecksum(this byte[] payload, int startIndex, int endIndex)
        {
            if (payload == null || payload.Length < endIndex + 1)
                throw new ArgumentException($"'{nameof(payload)}' hast to be at least {endIndex + 1} bytes long.");

            var checksum = payload.ComputeChecksum(startIndex, endIndex - 2);
            var currChecksum = payload.LittleEndianArrayToUInt16(endIndex - 1);

            return checksum == currChecksum;
        }

        /// <summary>
        /// Converts an <see cref="ushort"/> value to a little endian byte array.
        /// </summary>
        /// <param name="value">The <see cref="ushort"/> value.</param>
        /// <returns>A little endian byte array with the converted value.</returns>
        internal static byte[] ToLittleEndianArray(this ushort value)
        {
            var result = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }

        /// <summary>
        /// Converts a little endian array to an <see cref="ushort"/>.
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>An <see cref="ushort"/> with the converted value.</returns>
        internal static ushort LittleEndianArrayToUInt16(this byte[] data, int startIndex)
        {
            var result = new byte[2];
            Array.Copy(data, startIndex, result, 0, 2);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return BitConverter.ToUInt16(result, 0);
        }

        /// <summary>
        /// Converts an <see cref="int"/> value to a little endian byte array.
        /// </summary>
        /// <param name="value">The <see cref="int"/> value.</param>
        /// <returns>A little endian byte array with the converted value.</returns>
        internal static byte[] ToLittleEndianArray(this int value)
        {
            var result = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }

        /// <summary>
        /// Converts a little endian array to an <see cref="int"/>.
        /// </summary>
        /// <param name="data">The byte array.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>An <see cref="int"/> with the converted value.</returns>
        internal static int LittleEndianArrayToInt(this byte[] data, int startIndex)
        {
            var result = new byte[4];
            Array.Copy(data, startIndex, result, 0, 4);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return BitConverter.ToInt32(result, 0);
        }
    }
}
