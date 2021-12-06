// See https://aka.ms/new-console-template for more information
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ReplaysToCSV
{
    public class Program
    {

        private static readonly CsvConfiguration config = new(CultureInfo.InvariantCulture)
        {
            NewLine = Environment.NewLine,
            Encoding = Encoding.UTF8
        };

        public static void Main(string[] args)
        {
            // path to the folder of replays to parse
            string path;
            // incude subdirectories of said folder
            bool includeSubDirectories = false;

            // need 1 or 2 arguments
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
                Console.WriteLine(@"Example: ./ReplaysToCSV.exe ""path/to/folder"" ""true""");
                return;
            }

            Console.WriteLine($"Path: {path}");
            Console.WriteLine($"Include subdirectories: {includeSubDirectories}");

            // get all .wotreplay files in the folder
            IEnumerable<string> filePaths = Directory.EnumerateFiles(
                path,
                "*.wotreplay",
                includeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            // concurrentbag because we're adding replays in parrallel
            ConcurrentBag<ReplayInfo> replays = new();
            int failedReplays = 0;

            // hahaha numbers go brrrr
            Stopwatch stopwatch = new();
            stopwatch.Start();

            int replayCount = 0;

            // all available tanks
            var tankDict = GameData.GetTankDictionary();
            var mapDict = GameData.GetMapDictionary();

            // get a ReplayInfo object for each file
            var result = Parallel.ForEach(filePaths, filePath =>
                {
                    var replay = ReplayReader.ReadReplayFile(filePath, tankDict, mapDict);
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
                // Create a CSV file with all replays in it
                string csvPath = @$"{path}\{DateTime.Now.ToFileTime()}.csv";
                using (var writer = new StreamWriter(csvPath))
                using (var csv = new CsvWriter(writer, config))
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