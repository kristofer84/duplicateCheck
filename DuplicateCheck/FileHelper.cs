using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DuplicateCheck
{
    public class FileHelper
    {
        public static List<FileInfo> GetFiles(string path)
        {
            var dir = new DirectoryInfo(path);
            var fileInfo = dir.EnumerateFiles();

            var extensions = new[]
            {
                ".jpg",
                ".jpeg",
                //".cr2",
                //".mp4",
                //".3gp"
            };

            var files = fileInfo.AsParallel().Where(f => extensions.Contains(f.Extension.ToLower()));
            return files.ToList();
        }
    }
}