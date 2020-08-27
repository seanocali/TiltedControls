using Microsoft.Toolkit.Uwp.UI.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static Tilted.Carousel;
using static Tilted.Common;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TiltedCarouselDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        MainPageViewModel PageViewModel { get; set; }

        public MainPage()
        {
            PageViewModel = new MainPageViewModel();
            this.InitializeComponent();
            DataContext = PageViewModel;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            await Task.Delay(1500);
            var focusableElement = myCarousel.FindDescendant<ContentControl>();
            focusableElement.Focus(FocusState.Programmatic);
        }

        private void Grid_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            myCarousel.StartManipulationMode();
        }

        private void Grid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            myCarousel.StopManipulationMode();
        }

        private void Grid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (myCarousel.ItemsSource != null)
            {
                double value = 0;
                switch (myCarousel.CarouselType)
                {
                    case CarouselTypes.Wheel:
                        switch (myCarousel.WheelAlignment)
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
                        myCarousel.CarouselRotationAngle += Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Column:
                        value = e.Cumulative.Translation.Y * 2;
                        myCarousel.CarouselPositionY = Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Row:
                        value = e.Cumulative.Translation.X * 2;
                        myCarousel.CarouselPositionX = Convert.ToSingle(value);
                        break;
                }
            }

        }

        private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);
            switch (point.Properties.MouseWheelDelta)
            {
                case 120:
                    myCarousel.ChangeSelection(true);
                    break;
                case -120:
                    myCarousel.ChangeSelection(false);
                    break;
            }
        }

        private async void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case Windows.System.VirtualKey.Up:
                case Windows.System.VirtualKey.Down:
                    switch (myCarousel.CarouselType)
                    {
                        case CarouselTypes.Wheel:
                            switch (myCarousel.WheelAlignment)
                            {
                                case WheelAlignments.Right:
                                case WheelAlignments.Left:
                                    myCarousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Up);
                                    break;
                            }
                            break;
                        case CarouselTypes.Column:
                            myCarousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Up);
                            break;
                    }
                    break;
                case Windows.System.VirtualKey.Left:
                case Windows.System.VirtualKey.Right:
                    switch (myCarousel.CarouselType)
                    {
                        case CarouselTypes.Wheel:
                            switch (myCarousel.WheelAlignment)
                            {
                                case WheelAlignments.Top:
                                case WheelAlignments.Bottom:
                                    myCarousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Left);
                                    break;
                            }
                            break;
                        case CarouselTypes.Row:
                            myCarousel.ChangeSelection(args.VirtualKey == Windows.System.VirtualKey.Left);
                            break;
                    }
                    break;
                case Windows.System.VirtualKey.Enter:
                    myCarousel.AnimateSelection();
                    break;
            }
        }
    }
}
