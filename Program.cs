using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Rendering;
using System.CommandLine.Rendering.Views;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FolderStats
{
    class Program
    {
        public const string DefaultExtensions = 
            "txt,md,template,tpl," +
            "json,yaml,yml," +
            "xml," +
            "html,css," +
            "js,jsx,ts,tsx," +
            "cs,cshtml," +
            "py," +
            "sql," +
            "bat,cmd,sh,ps1,psm1," +
            "tf,tfvar," +
            "cfg,config,conf,properties,options,secrets";

        /// <summary>Prints directory statistics.</summary>
        /// <param name="text">Comma-separated list of text file extensions (files to count lines for) -- e.g. "js,py,cs".</param>
        /// <param name="only">Comma-separated list of file extensions to include.</param>
        /// <param name="args">Directories to print statistics for.</param>
        static async Task Main(InvocationContext ctx, string? text, string? only, string[] args)
        {
            var dirs = args ?? new[] { "." };
            var includeExtensions = (only ?? "")
                .Split(",")
                .Where(e => !string.IsNullOrEmpty(e))
                .Select(e => "." + e.Trim())
                .ToArray();
            var textExtensions = (text ?? DefaultExtensions)
                .Split(",")
                .Where(e => !string.IsNullOrEmpty(e))
                .Select(e => "." + e.Trim())
                .ToArray();

            var cancelCts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) => cancelCts.Cancel();
            var cancellationToken = cancelCts.Token;

            var s = new Statistics(includeExtensions, textExtensions);
            var tasks = dirs.Select(p => Task.Run(
                () => s.ProcessDir(p, cancellationToken), 
                cancellationToken));
            await Task.WhenAll(tasks);

            var output = new StackLayoutView() {
                new StackLayoutView() {
                    new ContentView($"Directories: {string.Join(", ", dirs)}"),
                    new ContentView($"Extensions:  {string.Join(" ", textExtensions)}"),
                },
                new StatisticsView("Statistics by directory:", "Directory", s.ByDirectory),
                new StatisticsView("Statistics by extension:", "Extension", s.ByExtension),
            };

            var terminal = ctx.Console as TerminalBase;
            var renderer = new ConsoleRenderer(ctx.Console, OutputMode.PlainText, true);
            var region = new Region(0, 0, int.MaxValue, int.MaxValue);

            terminal?.HideCursor();
            output.Render(renderer, region);
            terminal?.ShowCursor();
        }
    }
}
