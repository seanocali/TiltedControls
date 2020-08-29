using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tilted
{
    public static class Common
    {
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
