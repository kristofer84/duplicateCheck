using System.Linq;

namespace DuplicateCheck.Classes
{
    public class ImageInfo
    {
        public ImageInfo(string filename, /*DateTime? dateTaken, DateTime fileModified, */ short[] hashPixels)
        {
            Filename = filename;
            //DateTaken = dateTaken;
            //FileModified = fileModified;
            HashPixels = hashPixels;
        }

        public string GetHash => string.Join("", HashPixels.Select(s => s));
        public string Filename { get; }
        //public DateTime? DateTaken { get; }
        //public DateTime FileModified { get; }
        private short[] HashPixels { get; }
        public bool Delete { get; set; } = false;
    }
}