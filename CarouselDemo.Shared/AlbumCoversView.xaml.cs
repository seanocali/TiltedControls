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
            FileInfo postersFile;
#if NETFX_CORE
            postersFile = new FileInfo("TestData/AlbumCoverLinks.txt");
#else
            var dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            var parent = dir.Parent.FullName;
            postersFile = new FileInfo(Path.Combine(parent, "TestData/AlbumCoverLinks.txt"));
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
