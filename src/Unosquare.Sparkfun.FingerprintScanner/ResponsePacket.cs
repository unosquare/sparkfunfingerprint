namespace Unosquare.Sparkfun.FingerprintScanner
{
    using System;

    public class ResponsePacket
    {
        protected ResponsePacket()
        {
        }

        public int Parameter { get; private set; }

        public ushort Response { get; private set; }

        public virtual bool IsSuccessful => Response == (ushort)ResponseCode.Ack;

        public ErrorCode ErrorCode => !IsSuccessful ? (ErrorCode)Parameter : ErrorCode.NoError;

        public static ResponsePacket FromByteArray(byte[] frame)
        {
            var packet = new ResponsePacket();
            if (frame[0] == 0x55 && frame[1] == 0xAA)
            {
                packet.Parameter = BitConverter.ToInt32(frame, 4);
                packet.Response = BitConverter.ToUInt16(frame, 8);
            }

            return packet;
        }

        public static ResponsePacket ErrorPacket()
        {
            var packet = new ResponsePacket();
            packet.Response = (ushort)ResponseCode.Nack;
            packet.Parameter = (int)ErrorCode.Timeout;

            return packet;
        }
    }
}
