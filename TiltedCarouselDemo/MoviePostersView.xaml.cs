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
using static Tilted.Common;

namespace TiltedCarouselDemo
{
    public sealed partial class MoviePostersView : Page
    {
        MainPageViewModel PageViewModel;

        public MoviePostersView()
        {
            PageViewModel = new MainPageViewModel();
            Load();
        }

        async void Load()
        {
            await CreateTestItemsPosters();
            DataContext = PageViewModel;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
            this.InitializeComponent();
        }
        private void Grid_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            try
            {
                Input.Grid_ManipulationStarted(myCarousel, sender, e);
            }
            catch { }
        }

        private void Grid_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            try
            {
                Input.Grid_ManipulationCompleted(myCarousel, sender, e);
            }
            catch { }
        }

        private void Grid_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            try
            {
                Input.Grid_ManipulationDelta(myCarousel, sender, e);
            }
            catch { }
        }

        private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                Input.Canvas_PointerWheelChanged(myCarousel, sender, e);
            }
            catch { }
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            try
            {
                Input.CoreWindow_KeyDown(myCarousel, sender, args);
            }
            catch { }
        }


        private async Task CreateTestItemsPosters()
        {
            PageViewModel.Items = new List<ItemModel>();
            var posterLinks = await File.ReadAllLinesAsync("TestData/MoviePosterLinks.txt");
            foreach (var link in posterLinks)
            {
                var item = new ItemModel
                {
                    ImageSourcePath = link
                };
                PageViewModel.Items.Add(item);
            }
        }

        private void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}
