using Ghosts.NetOfficeProvider;
using System.Runtime.InteropServices;

namespace Ghosts.MarshalProvider
{
    public static class MarshalProvider
    {

        public static void ReleaseComObject(IMSOfficeApplication wordApplication)
        {
            Marshal.ReleaseComObject(wordApplication);
        }
        public static void FinalReleaseComObject(IMSOfficeApplication wordApplication)
        {
            Marshal.FinalReleaseComObject(wordApplication);
        }
    }
    
}
