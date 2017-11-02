using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace DuplicateCheck
{
    public class ImageHelper
    {
        public static Bitmap GetNormalizedImage(string imagePath, bool rotate)
        {
            using (var img = Image.FromFile(imagePath))
            {
                if (rotate) Rotate(img);
                var bitmap = new Bitmap(img);

                //bitmap.SetPropertyItem(img.GetPropertyItem(36867));
                //var propertyItem = img.GetPropertyItem(274);
                var propertyItem = img.PropertyItems.FirstOrDefault(x => x.Id == 274);
                if (propertyItem != null)
                {
                    bitmap.SetPropertyItem(propertyItem);
                }

                return bitmap;
            }
        }

        public static void Rotate(Image img, Image imgSource = null)
        {
            var pi = (imgSource ?? img).PropertyItems.Select(x => x).FirstOrDefault(x => x.Id == 274);
            if (pi == null) return;

            var o = pi.Value[0];

            if (o == 2) img.RotateFlip(RotateFlipType.RotateNoneFlipX);
            else if (o == 3) img.RotateFlip(RotateFlipType.RotateNoneFlipXY);
            else if (o == 4) img.RotateFlip(RotateFlipType.RotateNoneFlipY);
            else if (o == 5) img.RotateFlip(RotateFlipType.Rotate90FlipX);
            else if (o == 6) img.RotateFlip(RotateFlipType.Rotate90FlipNone);
            else if (o == 7) img.RotateFlip(RotateFlipType.Rotate90FlipY);
            else if (o == 8) img.RotateFlip(RotateFlipType.Rotate90FlipXY);
        }

        public static short[] GetHash(Bitmap bmpSource)
        {
            var dim = 8;
            var retArr = new short[dim * dim];

            //Min minimum value of witdh and height to be able to cut a square
            var minDim = Math.Min(bmpSource.Width, bmpSource.Height);

            //Cut out a square (to be able to rotate correctly, there's a known error in rotate)
            var bmpCut = bmpSource.Clone(new Rectangle(0, 0, minDim, minDim), bmpSource.PixelFormat);

            //Rotate the square if needed
            Rotate(bmpCut, bmpSource);

            //Reduce the size
            var bmpMin = new Bitmap(bmpCut, new Size(dim, dim));
            //bmpMin.Save(@"C:\temp\2.bmp");
            for (var j = 0; j < bmpMin.Height; j++)
            {
                for (var i = 0; i < bmpMin.Width; i++)
                {
                    retArr[i * j] = (short)(bmpMin.GetPixel(i, j).GetBrightness() * 10);
                }
            }
            return retArr;
        }

        public static int[] GetHash2(Bitmap bmpSource)
        {
            var retArr = new int[4];
            var bytes = GetBytes(bmpSource);
            const short bytesPerPixel = 4;

            short byteCounter = 0;
            foreach (var b in bytes)
            {
                retArr[byteCounter] += b;

                if (byteCounter++ == bytesPerPixel - 1)
                {
                    byteCounter = 0;
                }
            }
            return retArr;
        }
        
        public static byte[] GetBytes(string filename)
        {
            var bitmap = GetNormalizedImage(filename, true);
            return GetBytes(bitmap);
        }

        public static byte[] GetBytes(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadOnly, bitmap.PixelFormat);
            var length = bitmapData.Width * bitmapData.Height;

            var bytes = new byte[length];

            Marshal.Copy(bitmapData.Scan0, bytes, 0, length);
            bitmap.UnlockBits(bitmapData);
            return bytes;
        }
    }
}