using System.Collections.Concurrent;
using System.Collections.Immutable;
using ThreadedNASZipper;

public class FileSearcher
{
    private bool isRunning = false;

    public Action<FileFoundEventArgs>? FileFoundAction;
    public Action<SearchCompletedEventArgs>? SearchCompletedAction;

    private readonly string[] _searchPatterns;
    private readonly ConcurrentBag<string> filesBag;

    private Dictionary<char, List<string>> directoriesByDrive;
    public FileSearcher()
    {
        directoriesByDrive = new Dictionary<char, List<string>>();
        _searchPatterns = IniSettings.SearchPattern.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string dir in IniSettings.SourceDirectory.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            AddDirectory(dir);
        filesBag = new ConcurrentBag<string>();
    }

    private void AddDirectory(string dir)
    {
        char drive = dir.Substring(0, 1).ToUpper()[0];
        if (!directoriesByDrive.ContainsKey(drive))
            directoriesByDrive.Add(drive, new List<string>());
        directoriesByDrive[drive].Add(dir);
    }

    public async Task StartSearch()
    {
        if (isRunning)
            return;
        isRunning = true;
        List<Task> tasks = new List<Task>();

        foreach (char drive in directoriesByDrive.Keys)
        {
            tasks.Add(Task.Run(() => SearchFilesInDirectories(directoriesByDrive[drive])));
        }
        await Task.WhenAll(tasks);
        isRunning = false;
        OnSearchCompleted();

    }

    private void SearchFilesInDirectories(List<string> searchDirs)
    {
        var options = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true,
        };
        foreach (string sourceDir in searchDirs)
        {
            Parallel.ForEach(_searchPatterns, searchPattern =>
            {
                try
                {
                    IEnumerable<string> patternFiles = Directory.EnumerateFiles(sourceDir, searchPattern, options);
                    foreach (string filePath in patternFiles)
                    {
                        filesBag.Add(filePath);
                        OnFileFound(filePath);
                    }
                }

                catch (UnauthorizedAccessException ex)
                {
                    Logger.Log($"Unauthorized access to {sourceDir}: {ex.Message}");
                }
                catch (PathTooLongException ex)
                {
                    Logger.Log($"Path too long in {sourceDir}: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Logger.Log($"Error in {sourceDir}: {ex.Message}");
                }
               
            });
        }
    }

    protected virtual void OnFileFound(string filePath)
    {
        FileFoundAction?.Invoke(new FileFoundEventArgs(filePath));
    }

    protected virtual void OnSearchCompleted()
    {
        SearchCompletedAction?.Invoke(new SearchCompletedEventArgs(filesBag.ToImmutableArray()));
    }

    public IEnumerable<string> GetFileList()
    {
        return filesBag.ToImmutableArray();
    }
}

public class FileFoundEventArgs : EventArgs
{
    public string FilePath { get; }

    public FileFoundEventArgs(string filePath)
    {
        FilePath = filePath;
    }
}

public class SearchCompletedEventArgs : EventArgs
{
    public IEnumerable<string> Results { get; }

    public SearchCompletedEventArgs(IEnumerable<string> results)
    {
        Results = results;
    }
}