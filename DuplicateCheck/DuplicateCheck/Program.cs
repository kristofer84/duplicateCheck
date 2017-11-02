using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DuplicateCheck.Classes;

namespace DuplicateCheck
{
    class Program
    {
        private static readonly ConcurrentDictionary<string, byte[]> ByteCache = new ConcurrentDictionary<string, byte[]>();
        private const double Tolerance = 0.001;
        private static readonly int ConsoleStart = Console.CursorTop;
        private const int ConsoleReservedRows = 3;

        static void Main(string[] args)
        {
            //_path = args.Any() ? args[0] : @"C:\Users\Kristofer\Dropbox\Bilder\mobilbilder\2017";
            var path = args.Any() ? args[0] : @"C:\Users\Kristofer\Downloads\2016";
            //var path = args.Any() ? args[0] : @"C:\Users\Kristofer\Dropbox\Bilder\mobilbilder\2016";
            var manualCheck = args.Length > 1 ? bool.Parse(args[1]) : false;

            var dict = new ConcurrentDictionary<string, ConcurrentBag<ImageInfo>>();

            var files = FileHelper.GetFiles(path);
            var tot = files.Count;

            var totalSize = (float)files.Sum(s => s.Length);
            long processedSize = 0;
            var start = DateTime.Now;

            var count = 0;
#if DEBUG
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 1 };
#else
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 4 };
#endif
            Parallel.ForEach(files, parallelOptions, file =>
            {
                var sw = new Stopwatch();
                sw.Start();

                var bitmap = ImageHelper.GetNormalizedImage(file.FullName, false);

                //var dateTime = dateTaken?.ToLocalTime() ?? file.LastWriteTimeUtc.ToLocalTime();
                var testc = (long)0;

                for (int i = 0; i < 1000; i++)
                {
                    var sw0 = new Stopwatch();
                    sw0.Start();
                    var test = ImageHelper.GetHash2(bitmap);

                    testc += sw0.ElapsedMilliseconds;
                    
                }
                Console.WriteLine(testc/1000);
                Console.ReadKey();

                return;

                var hashCode = ImageHelper.GetHash(bitmap);

                var imageinfo = new ImageInfo(file.FullName, hashCode);

                var hash = imageinfo.GetHash;

                if (!dict.ContainsKey(hash))
                {
                    if (!dict.TryAdd(hash, new ConcurrentBag<ImageInfo>()))
                    {
                        throw new Exception("Kunde ej lägga till nyckel i ordlista");
                    }
                }
                else
                {
                    ImageHelper.Rotate(bitmap);
                    if (!ByteCache.TryAdd(imageinfo.Filename, ImageHelper.GetBytes(bitmap)))
                    {
                        throw new Exception("Kunde ej lägga till bytearray i ordlista");
                    }
                }

                dict[hash].Add(imageinfo);

                var curSeconds = (DateTime.Now - start).TotalSeconds;
                var processed = Interlocked.Add(ref processedSize, file.Length) / totalSize;
                var eta = curSeconds / processed - curSeconds;
                var ts = TimeSpan.FromSeconds(eta);

                Log($"Step 1/2: {Interlocked.Increment(ref count):D5}/{tot:D5}: {sw.ElapsedMilliseconds:D5} ms. Time left: {ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}", null, 0);
            });

            var totComp = dict.Count;
            var countComp = 0;

            Parallel.ForEach(dict, parallelOptions, hash =>
            {
                var sw = new Stopwatch();
                sw.Start();
                var sbLog = new StringBuilder();
                var sbConsole = new StringBuilder();

                var imageInfos = hash.Value.OrderBy(i => i.Filename).ToList();

                if (imageInfos.Count > 1)
                {
                    if (manualCheck)
                    {
                        var hashValue = hash.Key;
                        MoveAllFiles(imageInfos, path, hashValue);
                    }
                    else
                    {
                        CompareAndDeleteFiles(imageInfos, sbLog, sbConsole);
                    }
                }

                Log($"Step 2/2: {Interlocked.Increment(ref countComp):D5}/{totComp:D5}: {sw.ElapsedMilliseconds:D5} ms.", null, 1);
                Log(sbConsole.ToString(), sbLog.ToString());
            });

            File.WriteAllText("C:\\temp\\log.txt", Sb.ToString());
        }

        private static void MoveAllFiles(IEnumerable<ImageInfo> imageInfos, string path, string hashValue)
        {
            var dictInfo = Directory.CreateDirectory(Path.Combine(path, "tasBort", hashValue));
            foreach (var imageInfo in imageInfos)
            {
                var newFilename = Path.Combine(dictInfo.FullName, Path.GetFileName(imageInfo.Filename));
                File.Move(imageInfo.Filename, newFilename);
            }
        }

        private static void CompareAndDeleteFiles(IReadOnlyList<ImageInfo> imageInfos, StringBuilder sbLog, StringBuilder sbConsole)
        {
            sbLog.AppendLine("----------------");
            sbLog.AppendLine("Same hash:");

            foreach (var imageInfo in imageInfos)
            {
                sbLog.AppendLine(Path.GetFileName(imageInfo.Filename));
            }

            sbLog.AppendLine();

            for (var i = 1; i < imageInfos.Count; i++)
            {
                Compare(i, imageInfos, sbLog);
            }

            foreach (var imageInfo in imageInfos.Where(s => s.Delete))
            {
                var filename = imageInfo.Filename;
                var filenameNew = Path.Combine(Path.GetDirectoryName(filename), "tasBort",
                    Path.GetFileName(filename));

                File.Move(filename, filenameNew);
                sbConsole.AppendLine($"Deleting {filename}");
                sbLog.AppendLine($"Deleting {filename}");
            }
        }

        private static readonly StringBuilder Sb = new StringBuilder();
        private static void Log(string strConsole, string strLog, int? row = null)
        {
            lock (Sb)
            {
                if (!string.IsNullOrEmpty(strConsole))
                {
                    if (row != null)
                    {
                        var prev = new Point(Console.CursorLeft, Console.CursorTop);

                        //Goto specified row
                        Console.SetCursorPosition(0, ConsoleStart + (int)row);

                        //Clear the row
                        Console.Write(new string(' ', Console.WindowWidth));

                        //Goto specified row again
                        Console.SetCursorPosition(0, ConsoleStart + (int)row);

                        //Write line
                        Console.WriteLine(strConsole);

                        //Reset cursor
                        Console.SetCursorPosition(prev.X, prev.Y);
                    }
                    else
                    {
                        if (Console.CursorTop < ConsoleStart + ConsoleReservedRows)
                        {
                            Console.SetCursorPosition(0, ConsoleStart + ConsoleReservedRows);
                        }

                        Console.WriteLine(strConsole.TrimEnd(Environment.NewLine.ToCharArray()));
                    }
                }

                if (!string.IsNullOrEmpty(strLog)) Sb.AppendLine(strLog.TrimEnd(Environment.NewLine.ToCharArray()));
            }
        }

        private static void Compare(int toValue, IReadOnlyList<ImageInfo> imageInfos, StringBuilder sb)
        {
            for (var j = 0; j < toValue; j++)
            {
                var imageInfoI = imageInfos[toValue];
                var imageInfoJ = imageInfos[j];

                if (imageInfoJ.Delete) continue;

                var bytesI = GetBytes(imageInfoI);
                var bytesJ = GetBytes(imageInfoJ);

                if (ByteHelper.Equals(bytesI, bytesJ, Tolerance, out var diff)) imageInfoI.Delete = true;

                var diffStr = diff + (diff < Tolerance ? $" < {Tolerance} (duplicate)" : $" >= {Tolerance}");
                sb.AppendLine($"Diff {Path.GetFileName(imageInfoI.Filename)} <-> {Path.GetFileName(imageInfoJ.Filename)}: {diffStr}");

                if (!imageInfoI.Delete) continue;

                ByteCache.TryRemove(imageInfoI.Filename, out byte[] _);
                return;
            }
        }

        private static byte[] GetBytes(ImageInfo imageInfo)
        {
            byte[] bytes;
            if (ByteCache.ContainsKey(imageInfo.Filename))
            {
                bytes = ByteCache[imageInfo.Filename];
            }
            else
            {
                bytes = ImageHelper.GetBytes(imageInfo.Filename);
                ByteCache.TryAdd(imageInfo.Filename, bytes);
            }
            return bytes;
        }
    }
}