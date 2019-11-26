using System.Collections.Generic;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;

namespace FolderStats
{
    internal class StatisticsView : StackLayoutView
    {
        public StatisticsView(string title, string categoryName, Dictionary<string, FileStatistics> statistics)
        {
            ContentSpan Span(string content) => new ContentSpan(content);

            Add(new ContentView("\n"));
            Add(new ContentView(title));
            Add(new ContentView("\n"));

            var tableView = new TableView<KeyValuePair<string, FileStatistics>> {
                Items = statistics
                    .OrderByDescending(p => (p.Value.Lines > 0, p.Value.Size, p.Value.Count))
                    .ToList(),
            };
            tableView.AddColumn(
                p => Span(p.Key), 
                new ContentView(Span(categoryName)));
            tableView.AddColumn(
                p => Span($"{p.Value.Size / 1024.0:N3}"),
                new ContentView(Span($"Total Size, KB")));
            tableView.AddColumn(
                p => Span($"{p.Value.Lines / 1000.0:N3}"),
                new ContentView(Span($"Line Count, K")));
            tableView.AddColumn(
                p => Span($"{p.Value.Count:N0}"),
                new ContentView(Span($"File Count")));

            Add(tableView);
        }
    }
}
