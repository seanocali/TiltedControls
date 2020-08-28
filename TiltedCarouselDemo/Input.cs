using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Input;
using static Tilted.Common;

namespace TiltedCarouselDemo
{
    public class Input
    {
        Tilted.Carousel _myCarousel;

        public Input(Tilted.Carousel myCarousel)
        {
            _myCarousel = myCarousel;
        }
        public void Grid_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _myCarousel.StartManipulationMode();
        }

        public void Grid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            _myCarousel.StopManipulationMode();
        }

        public void Grid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (_myCarousel.ItemsSource != null)
            {
                double value = 0;
                switch (_myCarousel.CarouselType)
                {
                    case CarouselTypes.Wheel:
                        switch (_myCarousel.WheelAlignment)
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
                        _myCarousel.CarouselRotationAngle += Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Column:
                        value = e.Cumulative.Translation.Y * 2;
                        _myCarousel.CarouselPositionY = Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Row:
                        value = e.Cumulative.Translation.X * 2;
                        _myCarousel.CarouselPositionX = Convert.ToSingle(value);
                        break;
                }
            }

        }

        public void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_myCarousel);
            switch (point.Properties.MouseWheelDelta)
            {
                case 120:
                    _myCarousel.ChangeSelection(true);
                    break;
                case -120:
                    _myCarousel.ChangeSelection(false);
                    break;
            }
        }

        public void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case Windows.System.VirtualKey.Up:
                case Windows.System.VirtualKey.Down:
                    switch (_myCarousel.CarouselType)
                    {
                        case CarouselTypes.Wheel:
                            switch (_myCarousel.WheelAlignment)
                            {
                                case WheelAlignments.Right:
                                case WheelAlignments.Left:
                                    _myCarousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Up);
                                    break;
                            }
                            break;
                        case CarouselTypes.Column:
                            _myCarousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Up);
                            break;
                    }
                    break;
                case Windows.System.VirtualKey.Left:
                case Windows.System.VirtualKey.Right:
                    switch (_myCarousel.CarouselType)
                    {
                        case CarouselTypes.Wheel:
                            switch (_myCarousel.WheelAlignment)
                            {
                                case WheelAlignments.Top:
                                case WheelAlignments.Bottom:
                                    _myCarousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Left);
                                    break;
                            }
                            break;
                        case CarouselTypes.Row:
                            _myCarousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Left);
                            break;
                    }
                    break;
                case Windows.System.VirtualKey.Enter:
                    _myCarousel.AnimateSelection();
                    break;
            }
        }
    }
}
