using System.Runtime.InteropServices;
using System.Text;

namespace ThreadedNASZipper
{
    public static class IniFileHelper
    {
        public static string ReadIniValue(string iniFilePath, string section, string key)
        {
            StringBuilder sb = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", sb, 255, iniFilePath);
            return sb.ToString();
        }

        [DllImport("kernel32.dll")]
        private static extern long GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);
    }
}
