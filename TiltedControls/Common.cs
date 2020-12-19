﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace TiltedControls
{
    public static partial class Common
    {
        public static async Task<byte[]> AsByteArray(this StorageFile file)
        {
            IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read);
            var reader = new Windows.Storage.Streams.DataReader(fileStream.GetInputStreamAt(0));
            await reader.LoadAsync((uint)fileStream.Size);

            byte[] pixels = new byte[fileStream.Size];

            reader.ReadBytes(pixels);

            return pixels;
        }

        public enum CarouselTypes
        {
            Wheel, Column, Row
        }

        public enum WheelAlignments
        {
            Right, Left, Top, Bottom
        }

        internal static int Modulus(int num, int mod)
        {
            int result = num % mod;
            if (result < 0)
            {
                result += mod;
            }
            return result;
        }

        internal static Task AsTask(this CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<object>();
            cancellationToken.Register(() => tcs.TrySetCanceled(),
                useSynchronizationContext: false);
            return tcs.Task;
        }

        internal static int Mod(this int x, int m)
        {
            if (m < 0) m = -m;
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        internal static int ModularDistance(int a, int b, int m)
        {
            return System.Math.Min(Mod(a - b, m), Mod(b - a, m));
        }

        internal static double DegreesToRadians(double degrees)
        {
            return (degrees * (Math.PI / 180));
        }
    }
}