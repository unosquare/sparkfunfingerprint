namespace Unosquare.Sparkfun.FingerprintScanner
{
    public sealed class UserData : ResponsePacket
    {
        public UserData(int userId, int securityLevel)
        {
            UserId = userId;
            UserPrivilege = securityLevel;
        }

        public override bool IsSuccessful => true;

        public byte[] GT521F { get; }

        public int UserId { get; }

        public int UserPrivilege { get; }
    }
}
