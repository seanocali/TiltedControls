using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace CarouselDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AlbumCoversView : Page
    {
        MainPageViewModel PageViewModel;

        public AlbumCoversView()
        {
            PageViewModel = new MainPageViewModel();
            this.KeyDown += AlbumCoversView_KeyDown;
            this.PointerWheelChanged += AlbumCoversView_PointerWheelChanged;
            Load();
        }

        private void AlbumCoversView_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            Input.WheelChanged(myCarousel, e.GetCurrentPoint(myCarousel));
        }

        private void AlbumCoversView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            Input.KeyDown(myCarousel, e.Key);
        }

        async void Load()
        {
            await CreateTestItemsAlbums();
            DataContext = PageViewModel;
            this.InitializeComponent();
        }

        private async Task CreateTestItemsAlbums()
        {
            PageViewModel.Items = new List<ItemModel>();
            var posterLinks = await File.ReadAllLinesAsync("TestData/AlbumCoverLinks.txt");
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
