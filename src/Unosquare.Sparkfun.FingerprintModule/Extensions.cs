namespace Unosquare.Sparkfun.FingerprintModule
{
    using System;
    using System.Collections.Generic;

    internal static class Extensions
    {
        internal static UInt16 ComputeChecksum(this IList<byte> payload)
        {
            if (payload == null || payload.Count == 0)
                throw new ArgumentException($"'{nameof(payload)}' must not be empty.");

            return payload.ComputeChecksum(0, payload.Count - 1);
        }

        internal static UInt16 ComputeChecksum(this IList<byte> payload, int startIndex, int endIndex)
        {
            if (payload == null || payload.Count < endIndex + 1)
                throw new ArgumentException($"'{nameof(payload)}' hast to be at least {endIndex + 1} bytes long.");

            UInt16 checksum = payload[startIndex];
            for (var i = startIndex + 1; i <= endIndex; i++)
            {
                checksum = (UInt16)(checksum + payload[i]);
            }

            return checksum;
        }

        internal static bool ValidateChecksum(this byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                throw new ArgumentException($"'{nameof(payload)}' must not be empty.");

            return payload.ValidateChecksum(0, payload.Length - 1);
        }

        internal static bool ValidateChecksum(this byte[] payload, int startIndex, int endIndex)
        {
            if (payload == null || payload.Length < endIndex + 1)
                throw new ArgumentException($"'{nameof(payload)}' hast to be at least {endIndex + 1} bytes long.");

            UInt16 checksum = payload.ComputeChecksum(startIndex, endIndex - 2);
            UInt16 currChecksum = payload.LittleEndianArrayToUInt16(endIndex - 1);

            return checksum == currChecksum;
        }

        internal static byte[] ToLittleEndianArray(this UInt16 value)
        {
            var result = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }

        internal static UInt16 LittleEndianArrayToUInt16(this byte[] data) => data.LittleEndianArrayToUInt16(0);

        internal static UInt16 LittleEndianArrayToUInt16(this byte[] data, int startIndex)
        {
            var result = new byte[2];
            Array.Copy(data, startIndex, result, 0, 2);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return BitConverter.ToUInt16(result, 0);
        }

        internal static byte[] ToLittleEndianArray(this int value)
        {
            var result = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }

        internal static int LittleEndianArrayToInt(this byte[] data) => data.LittleEndianArrayToInt(0);

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
