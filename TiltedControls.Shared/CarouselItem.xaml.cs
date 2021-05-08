#if NETFX_CORE
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#else
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace TiltedControls
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
