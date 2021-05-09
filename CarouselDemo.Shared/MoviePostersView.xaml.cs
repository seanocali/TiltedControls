using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

#if NETFX_CORE
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
#else
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
#endif

namespace CarouselDemo
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
            FileInfo postersFile;
#if NETFX_CORE
            postersFile = new FileInfo("TestData/MoviePosterLinks.txt");
#else
            var dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            var parent = dir.Parent.FullName;
            postersFile = new FileInfo(Path.Combine(parent, "TestData/MoviePosterLinks.txt"));
#endif
            if (postersFile.Exists)
            {
                var posterLinks = await File.ReadAllLinesAsync(postersFile.FullName);
                foreach (var link in posterLinks)
                {
                    var item = new ItemModel
                    {
                        ImageSourcePath = link
                    };
                    PageViewModel.Items.Add(item);
                }
            }
            else
            {
                throw new FileNotFoundException("File not found", postersFile.FullName);
            }
        }

        private void CloseButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(MainPage));
        }
    }
}
