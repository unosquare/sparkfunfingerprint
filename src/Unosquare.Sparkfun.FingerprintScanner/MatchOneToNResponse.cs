namespace Unosquare.Sparkfun.FingerprintScanner
{
    public class MatchOneToNResponse : ResponsePacket
    {
        private readonly bool _isSuccessful;

        public MatchOneToNResponse(int userId, int securityLevel)
        {
            UserId = userId;
            UserPrivilege = securityLevel;
            _isSuccessful = true;
        }

        private MatchOneToNResponse()
        {
            _isSuccessful = false;
        }

        public override bool IsSuccessful => _isSuccessful;

        public int UserId { get; }

        public int UserPrivilege { get; }

        public static MatchOneToNResponse UnsuccessResponse()
        {
            return new MatchOneToNResponse();
        }
    }
}
