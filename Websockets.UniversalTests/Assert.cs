using System.Diagnostics;

namespace Websockets.UniversalTests
{
    public class Assert
    {
        public static bool True(bool isTrue)
        {
            if (!isTrue)
            {
                Debug.WriteLine("Test failed");
                return false;
            }
            return true;
        }
    }
}
