using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FolderStats 
{
    public class Statistics
    {
        public static string DirectoryExtension = "(directory)";
        public HashSet<string> TextExtensions { get; }
        public HashSet<string> IncludeExtensions { get; }

        public Dictionary<string, FileStatistics> ByDirectory { get; } = 
            new Dictionary<string, FileStatistics>();
        public Dictionary<string, FileStatistics> ByExtension { get; } =
            new Dictionary<string, FileStatistics>();
        protected object Lock { get; } = new object();

        public Statistics(IEnumerable<string> includeExtensions, IEnumerable<string> textExtensions)
        {
            IncludeExtensions = includeExtensions.ToHashSet();
            TextExtensions = textExtensions.ToHashSet();
        }

        public async Task<FileStatistics> ProcessDir(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new DirectoryInfo(path);
            var dirs = info.GetDirectories();
            var files = info.GetFiles();
            if (IncludeExtensions.Count > 0)
                files = files
                    .Where(p => IncludeExtensions.Contains(Path.GetExtension(p.Name)))
                    .ToArray();

            var dirTasks = dirs.Select(p => Task.Run(
                () => ProcessDir(Path.Combine(path, p.Name), cancellationToken), 
                cancellationToken));
            var fileTasks = files.Select(p => Task.Run(
                () => ProcessFile(Path.Combine(path, p.Name), cancellationToken), 
                cancellationToken));
            var allTasks = dirTasks.Concat(fileTasks).ToArray();
            await Task.WhenAll(allTasks).ConfigureAwait(false);

            var statistics = new FileStatistics() {
                Count = 0, 
                Size = 0, 
                Lines = files.Length
            };
            statistics = allTasks.Aggregate(statistics, (acc, task) => acc.CombineWith(task.Result));
            
            AddFileStatistics(path, true, statistics);
            return statistics;
        }

        public async Task<FileStatistics> ProcessFile(string path, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var info = new FileInfo(path);
            long lines = 0;
            if (TextExtensions.Contains(Path.GetExtension(path))) {
                using var r = info.OpenText();
                while (true) {
                    if ((lines & 127) == 0) 
                        cancellationToken.ThrowIfCancellationRequested();
                    var line = await r.ReadLineAsync().ConfigureAwait(false);
                    if (line == null)
                        break;
                    lines++;
                }
            }

            var statistics = new FileStatistics() {
                Count = 1, 
                Size = info.Length, 
                Lines = lines
            };
            AddFileStatistics(path, false, statistics);

            return statistics;
        }

        public void AddFileStatistics(string path, bool isDirectory, FileStatistics statistics)
        {
            var extension = isDirectory ? DirectoryExtension : Path.GetExtension(path).ToLowerInvariant();
            lock (Lock) {
                var s = statistics.CombineWith(ByExtension.GetValueOrDefault(extension));
                if (s.Size > 0)
                    ByExtension[extension] = s;
                if (isDirectory) {
                    s = statistics.CombineWith(ByDirectory.GetValueOrDefault(path));
                    if (s.Size > 0)
                        ByDirectory[path] = s;
                }
            }
        }
    }
}
