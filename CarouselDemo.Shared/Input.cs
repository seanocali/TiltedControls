using System;
using TiltedControls;
using Windows.System;
#if NETFX_CORE
using Windows.UI.Xaml.Input;
using Windows.UI.Input;
#else
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Input;
#endif
using static TiltedControls.Common;

namespace CarouselDemo
{
    public static class Input
    {

        public static void Grid_ManipulationStarted(Carousel carousel, object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (carousel != null && carousel.AreItemsLoaded)
            {
                //GitTest
                carousel.StartManipulationMode();
            }
        }

        public static void Grid_ManipulationCompleted(Carousel carousel, object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (carousel != null && carousel.AreItemsLoaded)
            {
                carousel.StopManipulationMode();
            }
        }

        public static void Grid_ManipulationDelta(Carousel carousel, object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (carousel != null && carousel.AreItemsLoaded && carousel.ItemsSource != null)
            {
                double value = 0;
                switch (carousel.CarouselType)
                {
                    case CarouselTypes.Wheel:
                        switch (carousel.WheelAlignment)
                        {
                            case WheelAlignments.Right:
                                value = -(e.Delta.Translation.Y / 4);
                                break;
                            case WheelAlignments.Left:
                                value = e.Delta.Translation.Y / 4;
                                break;
                            case WheelAlignments.Top:
                                value = -(e.Delta.Translation.X / 4);
                                break;
                            case WheelAlignments.Bottom:
                                value = e.Delta.Translation.X / 4;
                                break;
                        }
                        carousel.CarouselRotationAngle += Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Column:
                        value = e.Cumulative.Translation.Y * 2;
                        carousel.CarouselPositionY = Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Row:
                        value = e.Cumulative.Translation.X * 2;
                        carousel.CarouselPositionX = Convert.ToSingle(value);
                        break;
                }
            }

        }

#if NETFX_CORE
        public static void WheelChanged(Carousel carousel, PointerPoint point)
#else
        public static void WheelChanged(Carousel carousel, Microsoft.UI.Input.Experimental.ExpPointerPoint point)
#endif
        {
            if (carousel != null && carousel.AreItemsLoaded)
            {
                switch (point.Properties.MouseWheelDelta)
                {
                    case 120:
                        carousel.ChangeSelection(true);
                        break;
                    case -120:
                        carousel.ChangeSelection(false);
                        break;
                }
            }
        }

        internal static void KeyDown(Carousel carousel, VirtualKey key)
        {
            if (carousel != null && carousel.AreItemsLoaded)
            {
                switch (key)
                {
                    case Windows.System.VirtualKey.Up:
                    case Windows.System.VirtualKey.Down:
                        switch (carousel.CarouselType)
                        {
                            case CarouselTypes.Wheel:
                                switch (carousel.WheelAlignment)
                                {
                                    case WheelAlignments.Right:
                                    case WheelAlignments.Left:
                                        carousel.ChangeSelection(key == Windows.System.VirtualKey.Up);
                                        break;
                                }
                                break;
                            case CarouselTypes.Column:
                                carousel.ChangeSelection(key == Windows.System.VirtualKey.Up);
                                break;
                        }
                        break;
                    case Windows.System.VirtualKey.Left:
                    case Windows.System.VirtualKey.Right:
                        switch (carousel.CarouselType)
                        {
                            case CarouselTypes.Wheel:
                                switch (carousel.WheelAlignment)
                                {
                                    case WheelAlignments.Top:
                                    case WheelAlignments.Bottom:
                                        carousel.ChangeSelection(key == Windows.System.VirtualKey.Left);
                                        break;
                                }
                                break;
                            case CarouselTypes.Row:
                                carousel.ChangeSelection(key == Windows.System.VirtualKey.Left);
                                break;
                        }
                        break;
                    case Windows.System.VirtualKey.Enter:
                        carousel.AnimateSelection();
                        break;
                }
            }

        }
    }
}
