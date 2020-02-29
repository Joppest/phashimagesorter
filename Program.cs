using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Shipwreck.Phash;
using System.Collections.Concurrent;
using System.Drawing;
using System.Xml.Serialization;
using System.Timers;

namespace phashsorter
{
    class Program
    {
        static string text = "phashsorter:\n" +
            " Sorts images in a directory by their phashes.\n" +
            "Usage:\n" +
            " phashsorter inputdir outputdir\n";
      
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Sort();
            }
            if (args.Length == 1)
            {
                Sort(args[0]);
            }
            if (args.Length == 2)
            {
                Sort(args[0], args[1]);
            }
            if (args.Length > 2)
            {
                Console.WriteLine(text);
            }
        }

        static void Sort(string inputdir = "", string outputdir = ".\\output")
        {
            Console.WriteLine("Hashing " + inputdir);
            System.Timers.Timer t = new System.Timers.Timer(5000);
            t.Elapsed += T_Elapsed;
            DateTime start = DateTime.Now;
            t.Start();

            (ConcurrentDictionary<string, Digest> gilePathsToHashes, ConcurrentDictionary<Digest, HashSet<string>> hashesToFiles) =
    GetHashes(
        dirPath: inputdir,
        searchPattern: "*.png");

            t.Stop();
            DateTime stop = DateTime.Now;
            Console.WriteLine("Hashed " + hashesToFiles.Count + "images in " + (start.Ticks - stop.Ticks) / 1000 + "s.");

            Console.WriteLine("Sorting " + inputdir);
            t.Elapsed -= T_Elapsed;
            t.Elapsed += T_Elapsed1;
            t.Start();
            start = DateTime.Now;
            SortedList<string, Digest> sortedPaths = new System.Collections.Generic.SortedList<string, Digest>(gilePathsToHashes.Count);
            foreach (var item in gilePathsToHashes)
            {
                sortedPaths.Add(item.Key.ToString(), item.Value);
            }
            stop = DateTime.Now;
            t.Stop();
            Console.WriteLine("Sorted " + sortedPaths.Count + "images in " + (start.Ticks - stop.Ticks) / 1000 + "s.");

            Console.WriteLine("Writing output files " + inputdir);
            t.Elapsed -= T_Elapsed1;
            t.Elapsed += T_Elapsed2;
            t.Start();
            start = DateTime.Now;
            int i = 1;
            foreach (var item in sortedPaths)
            {
                if (File.Exists(outputdir + "\\" + i + ".png"))
                    File.Delete(outputdir + "\\" + i + ".png");
                File.Copy(item.Key, outputdir +"\\"+ i++ + ".png");
            }
            stop = DateTime.Now;
            t.Stop();
            Console.WriteLine("Copied " + sortedPaths.Count + "images in " + (start.Ticks - stop.Ticks) / 1000 + "s.");



        }

        private static void T_Elapsed2(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Copying " + e.SignalTime);
        }

        private static void T_Elapsed1(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Sorting " + e.SignalTime);
        }

        private static void T_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("Hashing " + e.SignalTime);
        }

        public static (ConcurrentDictionary<string, Digest> filePathsToHashes, ConcurrentDictionary<Digest, HashSet<string>> hashesToFiles) GetHashes(string dirPath, string searchPattern)
        {
            var filePathsToHashes = new ConcurrentDictionary<string, Digest>();
            var hashesToFiles = new ConcurrentDictionary<Digest, HashSet<string>>();

            var files = Directory.GetFiles(dirPath, searchPattern);

            Parallel.ForEach(files, (currentFile) =>
            {
                var bitmap = Bitmap.FromFile(currentFile);


                MemoryStream ms = new MemoryStream();
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                byte[] bitmapData = ms.ToArray();


                Shipwreck.Phash.Imaging.ByteImage bi = new Shipwreck.Phash.Imaging.ByteImage(bitmap.Width, bitmap.Height, bitmapData);
                var hash = ImagePhash.ComputeDigest(bi);
                filePathsToHashes[currentFile] = hash;

                HashSet<string> currentFilesForHash;

                lock (hashesToFiles)
                {
                    if (!hashesToFiles.TryGetValue(hash, out currentFilesForHash))
                    {
                        currentFilesForHash = new HashSet<string>();
                        hashesToFiles[hash] = currentFilesForHash;
                    }
                }

                lock (currentFilesForHash)
                {
                    currentFilesForHash.Add(currentFile);
                }
            });

            return (filePathsToHashes, hashesToFiles);
        }
    }
}
