
namespace RemoteForAndroidTV;
public static class Values
{

    public static class Pairing{
         private static readonly byte[] _firstPayloadMessage = 
        [8, 2, 16, 200, 1, 82, 43, 10, 21, 105, 110, 102, 111, 46, 107, 111, 100, 111, 110, 111, 46, 97, 115, 115, 105, 115, 116, 97, 110, 116, 18, 13, 105, 110, 116, 101, 114, 102, 97, 99, 101, 32, 119, 101, 98];
        private static readonly byte[] _secondPayloadMessage = 
        [8, 2, 16, 200, 1, 162, 1, 8, 10, 4, 8, 3, 16, 6, 24, 1];
        private static readonly byte[] _thirdPayloadMessage = 
        [8, 2, 16, 200, 1, 242, 1, 8, 10, 4, 8, 3, 16, 6, 16, 1];

        public static byte[] FirstPayloadMessage => _firstPayloadMessage;
        public static byte[] SecondPayloadMessage => _secondPayloadMessage;
        public static byte[] ThirdPayloadMessage => _thirdPayloadMessage;
    
    }

    public static class RemoteConnect{
        private static readonly byte[] _secondPayload = [18, 3, 8, 238, 4];
        public static byte[] SecondPayload => _secondPayload;
        private static readonly byte[] _pong = [74, 2, 8, 25];
        public static byte[] Pong => _pong;
        public static readonly int _maxAttempsToConnect = 1;
        public static readonly int _checkinternetConnectionEvery = 3; // in seconds

    }


}