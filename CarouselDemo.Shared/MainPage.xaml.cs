using System;
using System.Threading.Tasks;
using Windows.UI;
using System.Diagnostics;

#if NETFX_CORE
using Microsoft.Toolkit.Uwp.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
#else
using CommunityToolkit.WinUI.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
#endif

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CarouselDemo
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
            CreateTestItemsColors();
            DataContext = PageViewModel;
            this.Loaded += MainPage_Loaded;
            this.InitializeComponent();
            this.PointerWheelChanged += MainPage_PointerWheelChanged;
#if NETFX_CORE
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
#else
            this.KeyDown += MainPage_KeyDown;
#endif
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            myCarousel.Focus(FocusState.Keyboard);
        }

        private void MainPage_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            try
            {
                Input.WheelChanged(myCarousel, e.GetCurrentPoint(myCarousel));
            }
            catch { Debug.WriteLine("Mouse wheel input failed!"); }
        }

        private async void CoreWindow_KeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs args)
        {
            try
            {
                args.Handled = true;
                Debug.WriteLine("Key Pressed");
                await FocusManager.TryFocusAsync(myCarousel, FocusState.Keyboard);
                Input.KeyDown(myCarousel, args.VirtualKey);
                var e = FocusManager.GetFocusedElement();
                Debug.WriteLine(e.ToString());
            }
            catch { Console.Write($"Key input {args.VirtualKey} failed!"); }
        }

        private void MainPage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                Input.KeyDown(myCarousel, e.Key);
            }
            catch { Debug.WriteLine($"Key input {e.Key} failed!"); }
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
