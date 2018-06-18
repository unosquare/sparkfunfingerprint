namespace Unosquare.Sparkfun.FingerprintScanner
{
    using System;

    internal static class Extensions
    {
        internal static UInt16 ComputeChecksum(this byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                throw new ArgumentException($"'{nameof(payload)}' must not be empty.");

            return payload.ComputeChecksum(0, payload.Length -1);
        }

        internal static UInt16 ComputeChecksum(this byte[] payload, int startIndex, int endIndex)
        {
            if (payload == null || payload.Length < endIndex + 1)
                throw new ArgumentException($"'{nameof(payload)}' hast to be at least {endIndex + 1} bytes long.");

            UInt16 checksum = payload[startIndex];
            for (var i = startIndex + 1; i <= endIndex; i++)
            {
                checksum = (UInt16)(checksum + payload[i]);
            }

            return checksum;
        }

        internal static byte[] ToLittleEndianArray(this UInt16 value)
        {
            var result = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }

        internal static UInt16 LittleEndianArrayToUInt16(this byte[] data)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToUInt16(data, 0);
        }

        internal static byte[] ToLittleEndianArray(this int value)
        {
            var result = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }

        internal static int LittleEndianArrayToInt(this byte[] data)
        {
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToInt32(data, 0);
        }
    }
}
