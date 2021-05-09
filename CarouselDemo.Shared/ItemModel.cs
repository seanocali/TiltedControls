#if NETFX_CORE
using Windows.UI.Xaml.Media;
#else
using Microsoft.UI.Xaml.Media;
#endif

namespace CarouselDemo
{
    public class ItemModel
    {
        public string ImageSourcePath { get; set; }

        public Brush BackgroundColor { get; set; }

        public string Text { get; set; }
    }
}
