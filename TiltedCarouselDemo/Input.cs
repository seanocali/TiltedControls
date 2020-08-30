using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tilted;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using static Tilted.Common;

namespace TiltedCarouselDemo
{
    public static class Input
    {

        public static void Grid_ManipulationStarted(Carousel carousel, object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (carousel != null && carousel.AreItemsLoaded)
            {
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

        public static void Canvas_PointerWheelChanged(Carousel carousel, object sender, PointerRoutedEventArgs e)
        {
            if (carousel != null && carousel.AreItemsLoaded)
            {
                var point = e.GetCurrentPoint(carousel);
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

        public static void CoreWindow_KeyDown(Carousel carousel, CoreWindow sender, KeyEventArgs args)
        {
            if (carousel != null && carousel.AreItemsLoaded)
            {
                switch (args.VirtualKey)
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
                                        carousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Up);
                                        break;
                                }
                                break;
                            case CarouselTypes.Column:
                                carousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Up);
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
                                        carousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Left);
                                        break;
                                }
                                break;
                            case CarouselTypes.Row:
                                carousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Left);
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
