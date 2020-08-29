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
        Input _input;

        public MainPage()
        {
            PageViewModel = new MainPageViewModel();
            CreateTestItemsColors();
            DataContext = PageViewModel;
            this.Loaded += MainPage_Loaded;
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _input = new Input(this.myCarousel);
            await Task.Delay(1500);
            var focusableElement = myCarousel.FindDescendant<ContentControl>();
            focusableElement.Focus(FocusState.Programmatic);
        }

        private void Grid_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _input.Grid_ManipulationStarted(sender, e);
        }

        private void Grid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            _input.Grid_ManipulationCompleted(sender, e);
        }

        private void Grid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            _input.Grid_ManipulationDelta(sender, e);
        }

        private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            _input.Canvas_PointerWheelChanged(sender, e);
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            _input.CoreWindow_KeyDown(sender, args);
        }


        private void CreateTestItemsColors()
        {
            foreach (var prop in typeof(Colors).GetProperties())
            {
                if (prop.GetValue(null) is Color color)
                {
                    PageViewModel.Items.Add(new ItemModel { BackgroundColor = new SolidColorBrush(color), Text = prop.Name });
                }
            }
        }


        private void PostersButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(MoviePostersView));
        }

        private void AlbumsButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(AlbumCoversView));
        }
    }
}
