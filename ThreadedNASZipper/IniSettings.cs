using System.IO.Compression;

namespace ThreadedNASZipper
{
    public static class IniSettings
    {

        private static readonly Lazy<string> lazyIniPath = new Lazy<string>(() =>
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDirectory, "config.ini");
        });
        public static string IniPath => lazyIniPath.Value;
        public static string SourceDirectory { get; } = IniFileHelper.ReadIniValue(IniPath, "Search", "SourceDirectories");
        public static string SearchPattern { get; } = IniFileHelper.ReadIniValue(IniPath, "Search", "SearchPattern");

        public static bool OverwriteZipFiles { get; } = IniFileHelper.ReadIniValue(IniPath, "Zip", "OverwriteZipFiles") == "1" ? true : false;


        private static string? tempDirectory;

        public static string TempDirectory
        {
            get
            {
                if (tempDirectory == null)
                {
                    tempDirectory = IniFileHelper.ReadIniValue(IniPath, "Zip", "TempDirectory");
                    if (string.IsNullOrEmpty(tempDirectory))
                        tempDirectory = Path.Combine(Path.GetTempPath(), "output");
                }
                return tempDirectory;
            }
        }


        private static string? targetDirectory;
        public static string TargetDirectory
        {
            get
            {
                if (targetDirectory == null)
                {
                    targetDirectory = IniFileHelper.ReadIniValue(IniPath, "Zip", "TargetDirectory");
                    if (string.IsNullOrEmpty(targetDirectory))
                    {
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        targetDirectory = Path.Combine(baseDirectory, "output");
                    }
                }
                return targetDirectory;
            }
        }
        public static bool KeepDirectoryStructure { get; } = IniFileHelper.ReadIniValue(IniPath, "Zip","KeepDirectoryStructure")=="1"?true:false;

        public static string ZipPackageName { get; } = IniFileHelper.ReadIniValue(IniPath, "Zip", "ZipPackageName");
       // public static bool ZippingEnable { get; } = IniFileHelper.ReadIniValue(IniPath, "Zip", "Enable") == "1" ? true : false;

        public static bool LoggingEnable { get; } = IniFileHelper.ReadIniValue(IniPath, "Logging", "Enable") == "1" ? true : false;

        private static int? maxFileSize = null;
        public static int MaxFileSize
        {
            get
            {
                if (!maxFileSize.HasValue)
                {
                    if (int.TryParse(IniFileHelper.ReadIniValue(IniPath, "Zip", "MaxFileSize"), out int value))
                    {
                        maxFileSize = value;
                    }
                    else
                    {
                        maxFileSize = 100;
                    }
                }
                return maxFileSize.Value;
            }
        }

        private static int? maxFilesInZip = null;
        public static int MaxFilesInZip
        {
            get
            {
                if (!maxFilesInZip.HasValue)
                {
                    if (int.TryParse(IniFileHelper.ReadIniValue(IniPath, "Zip", "MaxFilesInZip"), out int value))
                    {
                        maxFilesInZip = value;
                    }
                    else
                    {
                        maxFilesInZip = 50;
                    }
                }
                return maxFilesInZip.Value;
            }
        }
        private static int? maxThreads = null;
        public static int MaxThreads
        {
            get
            {
                if (!maxThreads.HasValue)
                {
                    if (int.TryParse(IniFileHelper.ReadIniValue(IniPath, "Thread", "MaxThreads"), out int value))
                    {
                        if (value > 0)
                            maxThreads = value;
                        else
                            maxThreads = 4;
                    }
                    else
                        maxThreads = 4;
                }
                return maxThreads.Value;
            }
        }

        private static int? compressionLevelInt = null;
        public static int CompressionLevelInt
        {
            get
            {
                if (!compressionLevelInt.HasValue)
                {
                    if (int.TryParse(IniFileHelper.ReadIniValue(IniPath, "Zip", "CompressionLevel"), out int value))
                    {
                        compressionLevelInt = value;
                    }
                    else
                    {
                        // Standardwert, falls der Wert in der INI-Datei nicht gefunden oder nicht parsbar ist
                        compressionLevelInt = 0;
                    }
                }
                return compressionLevelInt.Value;
            }
        }

        public static CompressionLevel CompressionLevel
        {
            get
            {
                return (CompressionLevel)CompressionLevelInt;
            }
        }
    }
}
