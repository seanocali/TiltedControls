using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace TiltedCarouselDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MoviePostersView : Page
    {
        MainPageViewModel PageViewModel;
        Input _input;

        public MoviePostersView()
        {
            PageViewModel = new MainPageViewModel();
            CreateTestItems();
            DataContext = PageViewModel;
            this.Loaded += MoviePostersView_Loaded;
            this.InitializeComponent();
        }

        private void MoviePostersView_Loaded(object sender, RoutedEventArgs e)
        {
            _input = new Input(this.myCarousel);
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


        private void CreateTestItems()
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

        }

        //private void CreateTestItems()
        //{
        //    foreach (var prop in typeof(Colors).GetProperties())
        //    {
        //        if (prop.GetValue(null) is Color color)
        //        {
        //            var file = new FileInfo("TestData/MoviePosterLinks.txt");
        //            PageViewModel.Items.Add(new ItemModel { BackgroundColor = new SolidColorBrush(color), Text = prop.Name });
        //        }
        //    }
        //}
    }
}
