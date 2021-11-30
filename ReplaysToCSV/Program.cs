// See https://aka.ms/new-console-template for more information
using CsvHelper;
using ReplaysToCSV;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;

namespace ReplaysToCSV
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string path;
            bool includeSubDirectories = false;

            if (args.Length == 1 || args.Length == 2)
            {
                path = args[0];
                if (!Directory.Exists(path))
                {
                    Console.WriteLine("Invalid path.");
                    return;
                }
                if (args.Length == 2)
                {
                    if (bool.TryParse(args[1], out bool value))
                    {
                        includeSubDirectories = value;
                    }
                    else
                    {
                        Console.WriteLine("Invalid value for argument 2. Please enter true or false.");
                        return;
                    }
                }
            }

            else
            {
                Console.WriteLine("Please enter two arguments, separated by a single space.");
                Console.WriteLine("Arguments: Folder path, Include Subdirectiories (true/false)");
                return;
            }

            Console.WriteLine($"Path: {path}");
            Console.WriteLine($"Include subdirectories: {includeSubDirectories}");

            var replayReader = new ReplayReader();

            IEnumerable<string> filePaths = Directory.EnumerateFiles(
                path,
                "*.wotreplay",
                includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            ConcurrentBag<ReplayInfo> replays = new();
            int failedReplays = 0;

            Stopwatch stopwatch = new();
            stopwatch.Start();

            int replayCount = 0;
            var result = Parallel.ForEach(filePaths, filePath =>
            {
                var replay = replayReader.ReadReplayFile(filePath);
                if (replay is null)
                {
                    failedReplays++;
                    return;
                }
                replays.Add(replay);
                replayCount++;
            });
            stopwatch.Stop();

            Console.WriteLine($"{failedReplays} replay(s) failed to parse.");

            Console.WriteLine($"Loaded {replays.Count} replays in {stopwatch.ElapsedMilliseconds}ms");

            if (!replays.IsEmpty)
            {
                //File.Create($@"{path}")
                string csvPath = @$"{path}\{DateTime.Now.ToFileTime()}.csv";
                using (var writer = new StreamWriter(csvPath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(replays);
                }
                Console.WriteLine($"File created at {csvPath}");
            }
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}