using System;

namespace OcvFrames
{
    public static class DataSets
    {
        public static byte[] Random(int size)
        {
            var rnd = new Random();
            var data = new byte[size];
            rnd.NextBytes(data);
            return data;
        }

        public static byte[] ScatterReverse(int size)
        {
            var rnd = new Random();
            var data = new byte[size];
            rnd.NextBytes(data);
            var f = 256.0 / size;

            for (int i = 0; i < size; i++)
            {
                var rev = (size - i) * f;
                data[i] = (byte) (((rev * 7) + data[i]) / 8);
            }
            
            return data;
        }
        
        public static byte[] Sorted(int size)
        {
            var data = new byte[size];
            var f = 256.0 / size;

            for (int i = 0; i < size; i++)
            {
                data[i] = (byte) (i*f);
            }
            
            return data;
        }
    }
}