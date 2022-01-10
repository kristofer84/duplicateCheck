using System;

namespace DuplicateCheck
{
    public class ByteHelper
    {
        public static bool Equals(byte[] bytes1, byte[] bytes2, double tolerance, out float diff)
        {
            if (bytes1.Length != bytes2.Length)
            {
                diff = 1;
                return false;
            }

            float nomatch = 0;

            for (var index = 0; index < bytes1.Length; index++)
            {
                var a1 = bytes1[index];
                var a2 = bytes2[index];
                if (a1 != a2)
                {
                    var pixelDiff = (Math.Abs(a1 - (float)a2) + 1) / 256;
                    nomatch += pixelDiff;
                }
            }
            //var nomatch2 = bytes1.Zip(bytes2, (i, j) => i == j).Count(e => !e);
            diff = nomatch / bytes1.Length;
            return diff < tolerance;
        }
    }
}