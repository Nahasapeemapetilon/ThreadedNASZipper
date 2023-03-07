using System.Collections.Concurrent;
using System.IO.Compression;
using System.Runtime.ConstrainedExecution;
using ThreadedNASZipper;

//ThreadedZipperForNAS, die das Komprimieren von Dateien in mehreren Threads
public class ThreadedZipperForNAS
{
    // Eine Queue zum Speichern von Dateipfaden, die verarbeitet werden sollen
    private ConcurrentQueue<string> filesToZip;
    // Ein Objekt, das zur Synchronisierung von Threads verwendet wird
    private static readonly object lockObject = new object();
    // Die maximale Größe eines Batchs von Dateien, die in eine Zip-Datei gepackt werden können
    private long maxBatchSize = 0;
    // Die ID der Zip-Datei, die erstellt wurde
    private int zipID;
    // Die Anzahl der gepackten Dateien
    private int zippedFiles;
    // Ein Ereignis, das ausgelöst wird, wenn alle Dateien gepackt wurden
    public event Action? ZipCompleted;
    // Ein Ereignis, das ausgelöst wird, wenn eine neue Zip-Datei erstellt wurde
    public event Action<ZipCreatedEventArgs>? ZipCreated;
    // Ein CancellationTokenSource-Objekt, das zum Abbrechen des Verarbeitungsvorgangs verwendet wird
    private CancellationTokenSource cancellationTokenSource;
    // Ein ManualResetEvent-Objekt, das zur Benachrichtigung des Thread-Starts verwendet wird
    private ManualResetEvent newFileAdded;
    // Ein Flag, das angibt, ob das Verarbeitungsmodul ausgeführt wird oder nicht
    private bool isRunning;
    // Ein Konstruktor für die Klasse ThreadedZipperForNAS
    public ThreadedZipperForNAS()
    {
        #region Initialisierung Fields
        // Setze die maximale Batch-Größe auf die maximale Dateigröße, die in die Zip-Datei gepackt werden kann
        this.maxBatchSize = (long)IniSettings.MaxFileSize * 1024l * 1024l;
        this.filesToZip = new ConcurrentQueue<string>();
        zipID = 0;
        cancellationTokenSource = new CancellationTokenSource();
        newFileAdded = new ManualResetEvent(false);
        isRunning = false;
        zippedFiles = 0;
        #endregion
    }
    // Fügt eine neue Datei zur Verarbeitung hinzu
    public void AddFile(string filePath)
    {
        filesToZip.Enqueue(filePath);        
        if (filesToZip.Count > IniSettings.MaxFilesInZip)
            newFileAdded.Set();

    }
    private int getNextID()
    {
        return Interlocked.Increment(ref zipID);
    }
    // Gibt die Anzahl der gepackten Dateien zurück
    public int ZippedFilesCount
    {
        get
            {
            return zippedFiles; 
             }
    }
    // Erhöht die Anzahl der gepackten Dateien um 1
    private int AddZippedFileCount()
    {
        return Interlocked.Increment(ref zippedFiles);
    }
    // Startet den Verarbeitungsvorgang
    public async Task StartZipper()
    {
        if (isRunning)
            return;
        isRunning = true;
        List<Task> tasks = new List<Task>();
        for (int i = 0; i < IniSettings.MaxThreads; i++)                   
            tasks.Add(Task.Run(() => ProcessFilesAsync()));                              
        await Task.WhenAll(tasks);        
        isRunning = false;
        OnZipCompleted();
    }
    //setzt Stoppsignal ab
    public void StopZipper()
    {        
        cancellationTokenSource.Cancel();
        newFileAdded.Set();
    }
    // Event wenn das Packen beendet wurde
    protected virtual void OnZipCompleted()
    {
        ZipCompleted?.Invoke();
    }
    // Signal wenn ein Zip erstellt wurde
    protected virtual void OnZipCreated(string FilePath)
    {
        ZipCreated?.Invoke(new ZipCreatedEventArgs(FilePath));
    }

    //Thread zum verarbeiten der Daten
    private async Task ProcessFilesAsync()
    {     
        while (!filesToZip.IsEmpty || !cancellationTokenSource.IsCancellationRequested)
        {
            List<string> filesToProcess = new List<string>();
           
            if (filesToZip.Count == 0 && !cancellationTokenSource.IsCancellationRequested)
                newFileAdded.WaitOne(Timeout.Infinite, false);
            lock(lockObject)
            {     
                long currentBatchSize = 0l;
                while (filesToProcess.Count < IniSettings.MaxFilesInZip && currentBatchSize < maxBatchSize &&
                      (!cancellationTokenSource.IsCancellationRequested || !filesToZip.IsEmpty))
                {
                    if (!filesToZip.IsEmpty)
                    {
                        filesToZip.TryDequeue(out string? filePath);
                        currentBatchSize += new FileInfo(filePath).Length;                                     
                        filesToProcess.Add(filePath);
                    }
                    if (!cancellationTokenSource.IsCancellationRequested &&
                         filesToProcess.Count < IniSettings.MaxFilesInZip &&
                         filesToZip.IsEmpty)
                        newFileAdded.WaitOne(Timeout.Infinite, false);
                    
                }                                
            }

            if (filesToProcess.Count > 0)
            {
                string zipFilePath = Path.Combine(IniSettings.TempDirectory, IniSettings.ZipPackageName + getNextID().ToString() + ".zip");                
                //              using (MemoryStream memoryStream = new MemoryStream())                                
                    //                    using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Update, true))
                    using (ZipArchive archive = new ZipArchive(new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true), ZipArchiveMode.Create))
                    {
                        foreach (string fileToProcess in filesToProcess)
                        {
                            string entryName = Path.GetFileName(fileToProcess);
                            //  string entryName = fileToProcess.Replace(IniSettings.SourceDirectory + "\\", ""); 
                            ZipArchiveEntry entry = archive.CreateEntry(entryName, IniSettings.CompressionLevel);
                            using (Stream entryStream = entry.Open())

                            using (FileStream fileStream = new FileStream(fileToProcess, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
                            {
                                await fileStream.CopyToAsync(entryStream);
                            }
                            AddZippedFileCount();
                        }
                        
                    }
                //  using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                //  {
                //      memoryStream.Seek(0, SeekOrigin.Begin);
                //      await memoryStream.CopyToAsync(zipToOpen);
                //  }
                Logger.Log("Datei:" + zipFilePath + " wurde erstellt");                
                OnZipCreated(zipFilePath);
            }            
        }        
    }
}
public class ZipCreatedEventArgs : EventArgs
{
    public string FilePath { get; }

    public ZipCreatedEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}
