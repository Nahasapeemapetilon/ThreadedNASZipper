using System.Collections.Concurrent;

namespace ThreadedNASZipper
{
    //kopiert und löscht erstellten temporären Zip-Pakete
    public class CopyProcessor
    {
        private ConcurrentQueue<string> dataQueue = new ConcurrentQueue<string>();

        private bool isRunning = false;
        private AutoResetEvent dataAvailableEvent = new AutoResetEvent(false);

        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public event Action? CopyCompleted;
        public bool removeSourceFiles;
        public CopyProcessor()
        {
            removeSourceFiles = true;
        }
        public bool RemoveSourceFiles
        {
            get { return removeSourceFiles; }
            set { removeSourceFiles = value; }
        }
        public async Task Start()
        {
            if (isRunning)
            {
                return;
            }
            isRunning = true;
            await Task.Run(() => ProcessData(), cancellationTokenSource.Token);
            OnCopyComplete();
        }

        public void Stop()
        {
            isRunning = false;
            cancellationTokenSource.Cancel();
            dataAvailableEvent.Set();
        }

        public void AddData(string data)
        {
            dataQueue.Enqueue(data);
            dataAvailableEvent.Set();
        }
        private string? DequeueData()
        {
            string? output = string.Empty;
            if (dataQueue.TryDequeue(out output))
                return output;
            return null;

        }
        private bool DataIsEmpty()
        {
            return dataQueue.IsEmpty;
        }
        private void ProcessData()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested || !DataIsEmpty())
            {
                string? data = null;
                data = DequeueData();
                if (data == null && isRunning)
                    dataAvailableEvent.WaitOne();
                if (data != null)
                {
                    if (File.Exists(data))
                    {
                        try
                        {
                            File.Copy(data, Path.Combine(IniSettings.TargetDirectory, Path.GetFileName(data)), IniSettings.OverwriteZipFiles);
                            Logger.Log("Datei wurde kopiert: " + data);
                            if (RemoveSourceFiles)
                                File.Delete(data);
                            Logger.Log("Datei wurde gelöscht: " + data);
                        }
                        catch (Exception e)
                        {
                            Logger.Log("Es ist ein Fehler aufgetreten (löschen/kopieren) der Temporären Zip. " + e.Message.ToString());
                        }
                    }
                }
            }
        }
        protected virtual void OnCopyComplete()
        {
            CopyCompleted?.Invoke();
        }
    }
}
