using System;
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


        public enum GamepadInputTypes
        {
            A = 0,
            B = 1,
            X = 2,
            Y = 3,
            View = 4,
            Menu = 5,
            LeftShoulder = 6,
            RightShoulder = 7,
            LeftRightShoulder = 8,
            LeftTrigger = 9,
            RightTrigger = 10,
            LeftRightTrigger = 11,
            DPad = 12,
            DPadUp = 13,
            DPadDown = 14,
            DPadLeft = 15,
            DPadRight = 16,
            DPadUpLeft = 17,
            DPadDownRight = 18,
            DPadDownLeft = 19,
            DPadUpRight = 20,
            LeftThumbstick = 21,
            LeftThumbstickClockwise = 22,
            LeftThumbstickCounterclockwise = 23,
            LeftThumbstickUp = 24,
            LeftThumbstickDown = 25,
            LeftThumbstickLeft = 26,
            LeftThumbstickRight = 27,
            LeftThumbstickUpLeft = 28,
            LeftThumbstickDownRight = 29,
            LeftThumbstickDownLeft = 30,
            LeftThumbstickUpRight = 31,
            LeftThumbstickLeftRight = 32,
            LeftThumbstickUpDown = 33,
            LeftThumbstickButton = 34,
            RightThumbstick = 35,
            RightThumbstickClockwise = 36,
            RightThumbstickCounterclockwise = 37,
            RightThumbstickUp = 38,
            RightThumbstickDown = 39,
            RightThumbstickLeft = 40,
            RightThumbstickRight = 41,
            RightThumbstickUpLeft = 42,
            RightThumbstickDownRight = 43,
            RightThumbstickDownLeft = 44,
            RightThumbstickUpRight = 45,
            RightThumbstickLeftRight = 46,
            RightThumbstickUpDown = 47,
            RightThumbstickButton = 48,
            HomeButton = 49,
            DPadUpDown = 50,
            DPadLeftRight = 51,
            None
        }
    }
}
