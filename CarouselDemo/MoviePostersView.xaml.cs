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
            this.KeyDown += MoviePostersView_KeyDown;
            this.PointerWheelChanged += MoviePostersView_PointerWheelChanged;
            this.InitializeComponent();
        }

        private void MoviePostersView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                Input.KeyDown(myCarousel, e.Key);
            }
            catch { }
        }

        private void MoviePostersView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                Input.WheelChanged(myCarousel, e.GetCurrentPoint(myCarousel));
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
