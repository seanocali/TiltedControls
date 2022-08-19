using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TiltedControls
{
    public static partial class Common
    {
        public static T GetParent<T>(this Element element) where T : Element
        {
            if (element is T)
            {
                return element as T;
            }
            else
            {
                if (element.Parent != null)
                {
                    return element.Parent.GetParent<T>();
                }

                return default(T);
            }
        }

        public static IEnumerable<T> FindDescendants<T>(this Element element) where T : Element
        {
            var properties = element.GetType().GetRuntimeProperties();

            // try to parse the Content property
            var contentProperty = properties.FirstOrDefault(w => w.Name == "Content");
            if (contentProperty != null)
            {
                var content = contentProperty.GetValue(element) as Element;
                if (content != null)
                {
                    if (content is T)
                    {
                        yield return content as T;
                    }
                    foreach (var child in content.FindDescendants<T>())
                    {
                        yield return child;
                    }
                }
            }
            else
            {
                // try to parse the Children property
                var childrenProperty = properties.FirstOrDefault(w => w.Name == "Children");
                if (childrenProperty != null)
                {
                    // loop through children
                    IEnumerable<T> children = childrenProperty.GetValue(element) as IEnumerable<T>;
                    foreach (var child in children)
                    {
                        var childVisualElement = child as Element;
                        if (childVisualElement != null)
                        {
                            // return match
                            if (childVisualElement is T)
                            {
                                yield return childVisualElement as T;
                            }

                            // return recursive results of children
                            foreach (var childVisual in childVisualElement.FindDescendants<T>())
                            {
                                yield return childVisual;
                            }
                        }
                    }
                }
            }
        }

        public enum PickerModes
        {
            File,
            Folder
        }

        public enum PickerViews
        {
            Grid,
            List
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
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
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

        public enum MonochromeModes
        {
            None = 0,
            WhenAvailable = 1,
            Force = 2
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
