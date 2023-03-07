using System.Diagnostics;

namespace ThreadedNASZipper
{
    class Program
    {
        #region Fields
        // Die Klasseninstanzen für die Dateiverarbeitung        
        static CopyProcessor cp;
        static ThreadedZipperForNAS zipper;
        static FileSearcher fileSearcher;
        // Das AutoResetEvent wird verwendet, um darauf zu warten, dass alle Dateien kopiert wurden.
        static AutoResetEvent allDone = new AutoResetEvent(false);        
        #endregion
        #region constructor
        static Program()
        {
            // Initialisiere die Verarbeitungs-Objekte
            cp = new CopyProcessor();            
            zipper = new ThreadedZipperForNAS();
            fileSearcher = new FileSearcher();

            // Füge Event-Handler hinzu, um auf den Abschluss von Teilprozessen zu reagieren
            cp.CopyCompleted += CopyProcessor_CopyDone;
            zipper.ZipCompleted += Zipper_ZipCompleted;
            zipper.ZipCreated += Zipper_ZipCreated;
            
            fileSearcher.SearchCompletedAction += FileSearcher_Completed;
            fileSearcher.FileFoundAction += FileSearcher_FoundAFile;
        }
        #endregion

        // Region "Event-Handler"
        #region Event-Handler

        // Event-Handler für das Beenden des Kopierprozesses
        private static void CopyProcessor_CopyDone()
        {
            allDone.Set();
        }
        // Event-Handler für das Abschließen des Packvorgangs
        private static void Zipper_ZipCompleted()
        {
            cp.Stop();
        }
        // Event-Handler für das Erstellen eines Zip-Archivs
        private static void Zipper_ZipCreated(ZipCreatedEventArgs e)
        {
            cp.AddData(e.FilePath);
        }
        // Event-Handler für das Auffinden einer Datei
        private static void FileSearcher_FoundAFile(FileFoundEventArgs e)
        {
            zipper.AddFile(e.FilePath);
        }
        // Event-Handler für das Beenden der Suche
        private static void FileSearcher_Completed(SearchCompletedEventArgs e)
        {
            zipper.StopZipper();
        }
        #endregion

        // Region "Hilfsfunktionen"
        #region Hilfsfunktionen
        // Funktion zum Erstellen der Ziel- und temporären Verzeichnisse
        private static bool CreateAllDirectories()
        {
            try
            {
                // Erstelle das Zielverzeichnis, falls es nicht existiert
                if (!Directory.Exists(IniSettings.TargetDirectory))
                {
                    Directory.CreateDirectory(IniSettings.TargetDirectory);
                    Logger.Log("Verzeichnis wurde erstellt: " + IniSettings.TargetDirectory);
                }
                // Erstelle das temporäre Verzeichnis, falls es nicht existiert
                if (!Directory.Exists(IniSettings.TempDirectory))
                {
                    Directory.CreateDirectory(IniSettings.TempDirectory);
                    Logger.Log("Verzeichnis wurde erstellt: " + IniSettings.TempDirectory);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Log("Es konnten nicht alle Verzeichnise erstellt werden(TempDirectory,TargetDirectory)" + e.Message.ToString());
                return false;
            }
        }
        #endregion
        static async Task Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Logger.Log("Programm start!");
            // Erstelle die Verzeichnisse
            if (CreateAllDirectories())
            {
                //starte suche
                fileSearcher.StartSearch();
                // starte packvorgang
                zipper.StartZipper();
                //kopiere gepackte Dateien in das Zielverzeichnis
                cp.Start();

                allDone.WaitOne();
                Logger.Log("Files Found :  " + fileSearcher.GetFileList().Count());
                Logger.Log("Files Zipped : " + zipper.ZippedFilesCount);
            }
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            Logger.Log("Laufzeit:" + ts);
            Logger.Log("Programm beendet!");
            Logger.Close();
        }
    }
}