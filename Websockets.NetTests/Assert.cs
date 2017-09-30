using System.Diagnostics;

namespace Websockets.NetTests
{
    public class Assert
    {
        public static bool True(bool isTrue)
        {
            if (!isTrue)
            {
                Trace.WriteLine("Test failed");
                return false;
            }
            return true;
        }
    }
}
