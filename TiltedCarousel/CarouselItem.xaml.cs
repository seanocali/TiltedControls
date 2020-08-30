using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Tilted
{
    public sealed partial class CarouselItem : UserControl
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public CarouselItem()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Margin for carousel items is not supported at this time. Setting it will stomp visual layer properties, so the base class property is being hidden.
        /// Use ItemGap or Padding.
        /// </summary>
        public new Thickness Margin
        {
            get { return (Thickness)GetValue(MarginProperty); }
            set { SetValue(MarginProperty, value); }
        }

        public new static readonly DependencyProperty MarginProperty = DependencyProperty.Register(nameof(Thickness), typeof(Grid), typeof(Carousel),
            new PropertyMetadata(null));
    }
}
