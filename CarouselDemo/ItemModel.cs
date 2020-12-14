using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace TiltedCarouselDemo
{
    public class ItemModel
    {
        public string ImageSourcePath { get; set; }

        public Brush BackgroundColor { get; set; }

        public string Text { get; set; }
    }
}
