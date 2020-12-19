using Microsoft.Toolkit.Uwp.UI.Animations.Expressions;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static TiltedControls.Common;
using System.Diagnostics;
using Windows.UI.Xaml.Media.Media3D;
using Windows.UI.Core;
using Windows.ApplicationModel.SocialInfo;
using Windows.Devices.Bluetooth;
using Windows.UI.Xaml.Data;

namespace TiltedControls
{
    /// <summary>
    /// UI control to visually present a collection of data.
    /// </summary>
    /// <remarks>
    /// Visual tree contains an empty ContentControl for tab indexing and keyboard focus.
    /// </remarks>
    public class Carousel : Grid
    {
        #region CONSTRUCTOR & INITIALIZATION METHODS

        /// <summary>
        /// The class constructor.
        /// </summary>
        public Carousel()
        {
            _delayedRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _delayedRefreshTimer.Tick += _delayedRefreshTimer_Tick;
            _delayedZIndexUpdateTimer = new DispatcherTimer();
            _delayedZIndexUpdateTimer.Tick += _delayedZIndexUpdateTimer_Tick;
            this.Background = new SolidColorBrush(Colors.Transparent);
        }

        void Refresh()
        {
            if (this.DataContext != null && (IsLoaded || (!Double.IsNaN(Width) && !Double.IsNaN(Height))))
            {
                _inputAllowed = false;
                AreItemsLoaded = false;
                _delayedRefreshTimer.Start();
            }
        }

        void LoadNewCarousel()
        {
            _uIItemsCreated = false;
            _width = Double.IsNaN(Width) ? ActualWidth : Width;
            _height = Double.IsNaN(Height) ? ActualHeight : Height;
            _cancelTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancelTokenSource.Token;
            CreateContainers();
            _elementsToLoadCount = Density;
            var items = new FrameworkElement[Density];
            if (Items != null && Items.Count() > 0)
            {
                try
                {
                    // Fill the UIElementCollection with empty elements so we can use insert later
                    _itemsLayerGrid.Children.Clear();

                    for (int i = 0; i < Density; i++)
                    {
                        _itemsLayerGrid.Children.Add(new ContentControl());
                    }

                    for (int i = 0; i < Density / 2; i++)
                    {
                        var idx1 = (i + (Density / 2)) % Density;
                        var idx2 = Density - 1 - idx1;
                        var fe1 = AddCarouselItemToUI(idx1);
                        items[idx1] = fe1;
                        if (cancellationToken.IsCancellationRequested) { break; }
                        var fe2 = AddCarouselItemToUI(idx2);
                        if (cancellationToken.IsCancellationRequested) { break; }
                        items[idx2] = fe2;
                    }

                    _uIItemsCreated = true;
                    if (cancellationToken.IsCancellationRequested) { return; }

                    UpdateZIndices();
                    SetHitboxSize();
                    InitializeAnimations(items);
                    OnItemsCreated();
                }
                catch (Exception ex)
                {
                    _cancelTokenSource.Cancel();
                    Debug.WriteLine(ex.Message);
                }
            }
            _inputAllowed = true;
        }

        protected virtual void InitializeAnimations(FrameworkElement[] items)
        {
            for (int i = 0; i < Density / 2; i++)
            {
                var idx1 = (i + (Density / 2)) % Density;
                var idx2 = Density - 1 - idx1;

                StartExpressionItemAnimations(items[idx1], idx1);
                StartExpressionItemAnimations(items[idx2], idx2);
            }
        }

        FrameworkElement AddCarouselItemToUI(int idx)
        {
            int playlistIdx = (idx + this.currentStartIndexBackwards) % Items.Count();
            var itemElement = CreateCarouselItemElement(idx, playlistIdx);
            if (itemElement != null)
            {
                itemElement.Loaded += ItemElement_Loaded;
                _itemsLayerGrid.Children[Density - 1 - idx] = itemElement;
            }
            else
            {
                throw new Exception("Failed to create Carousel Item");
            }
            return itemElement;
        }

        private void ItemElement_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                element.Loaded -= ItemElement_Loaded;
                _elementsToLoadCount--;
                if (_elementsToLoadCount < 1)
                {
                    OnItemsLoaded();
                }
            }
        }

        void CreateContainers()
        {
            this.Children.Clear();
            _currentRowXPosTick = 0;
            _currentWheelTick = 0;
            _carouselInsertPosition = 0;

            _dynamicContainerGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            _dynamicContainerGrid.Width = _width;
            _dynamicContainerGrid.Height = _height;
            _dynamicGridVisual = ElementCompositionPreview.GetElementVisual(_dynamicContainerGrid);
            _dynamicGridVisual.CenterPoint = new Vector3(Convert.ToSingle(_width / 2), Convert.ToSingle(_height / 2), 0);
            ElementCompositionPreview.SetIsTranslationEnabled(_dynamicContainerGrid, true);
            AddImplicitWheelRotationAnimation(_dynamicGridVisual);
            _itemsLayerGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            _focusableDummyElement = new ContentControl { IsTabStop = true };
            this.Children.Add(_focusableDummyElement);
            _dynamicContainerGrid.Children.Add(_itemsLayerGrid);
            this.Children.Add(_dynamicContainerGrid);
            if (_hitbox != null)
            {
                _hitbox.ManipulationStarted -= _hitbox_ManipulationStarted;
                _hitbox.ManipulationCompleted -= _hitbox_ManipulationCompleted;
                _hitbox.ManipulationDelta -= _hitbox_ManipulationDelta;
            }
            if (UseGestures)
            {
                _hitbox = new Canvas();
                _hitbox.Background = new SolidColorBrush(Colors.Transparent);
                _hitbox.HorizontalAlignment = HorizontalAlignment.Center;
                _hitbox.VerticalAlignment = VerticalAlignment.Center;
                _hitbox.ManipulationMode = ManipulationModes.All;
                _hitbox.ManipulationStarted += _hitbox_ManipulationStarted;
                _hitbox.ManipulationCompleted += _hitbox_ManipulationCompleted;
                _hitbox.ManipulationDelta += _hitbox_ManipulationDelta;
                this.Children.Add(_hitbox);
            }

            ElementCompositionPreview.SetIsTranslationEnabled(this, true);
        }

        private void _hitbox_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (AreItemsLoaded && ItemsSource != null)
            {
                double value = 0;
                switch (CarouselType)
                {
                    case CarouselTypes.Wheel:
                        switch (WheelAlignment)
                        {
                            case WheelAlignments.Right:
                                value = -(e.Delta.Translation.Y / 4);
                                break;
                            case WheelAlignments.Left:
                                value = e.Delta.Translation.Y / 4;
                                break;
                            case WheelAlignments.Top:
                                value = -(e.Delta.Translation.X / 4);
                                break;
                            case WheelAlignments.Bottom:
                                value = e.Delta.Translation.X / 4;
                                break;
                        }
                        CarouselRotationAngle += Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Column:
                        value = e.Cumulative.Translation.Y * 2;
                        CarouselPositionY = Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Row:
                        value = e.Cumulative.Translation.X * 2;
                        CarouselPositionX = Convert.ToSingle(value);
                        break;
                }
            }
        }

        private void _hitbox_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            StopManipulationMode();
        }

        private void _hitbox_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            StartManipulationMode();
        }

        void SetHitboxSize()
        {
            if (UseGestures)
            {
                switch (CarouselType)
                {
                    default:
                        _hitbox.Width = _width;
                        _hitbox.Height = _height;
                        break;
                    case CarouselTypes.Wheel:
                        float ws = 0;
                        switch (WheelAlignment)
                        {
                            case WheelAlignments.Bottom:
                            case WheelAlignments.Top:
                                ws = WheelSize + (_maxItemHeight * Convert.ToSingle(SelectedItemScale));
                                break;
                            case WheelAlignments.Left:
                            case WheelAlignments.Right:
                                ws = WheelSize + (_maxItemWidth * Convert.ToSingle(SelectedItemScale));
                                break;
                        }
                        _hitbox.Width = ws;
                        _hitbox.Height = ws;
                        break;
                    case CarouselTypes.Column:
                        _hitbox.Height = _height;
                        _hitbox.Width = _maxItemWidth * SelectedItemScale;
                        if (WarpIntensity != 0 && WarpCurve != 0 && this.SelectedItemElement != null)
                        {
                            _hitbox.Width += (int)(Math.Abs((-WarpCurve) + Math.Abs(WarpIntensity)));
                        }
                        break;
                    case CarouselTypes.Row:
                        _hitbox.Width = _width;
                        _hitbox.Height = _maxItemHeight * SelectedItemScale;
                        if (WarpIntensity != 0 && WarpCurve != 0 && this.SelectedItemElement != null)
                        {
                            _hitbox.Height += (int)(Math.Abs((-WarpCurve) + Math.Abs(WarpIntensity)));
                        }
                        break;
                }

            }
        }


        #endregion

        #region FIELDS

        volatile bool _inputAllowed;
        double _width;
        double _height;
        DispatcherTimer _delayedRefreshTimer;
        DispatcherTimer _delayedZIndexUpdateTimer = new DispatcherTimer();
        CancellationTokenSource _cancelTokenSource;
        protected int _maxItemWidth;
        protected int _maxItemHeight;
        int _previousSelectedIndex;
        volatile float _scrollValue = 0;
        volatile float _scrollSnapshot = 0;
        volatile int _carouselInsertPosition;
        protected Visual _dynamicGridVisual;
        Grid _dynamicContainerGrid;
        Grid _itemsLayerGrid;
        Canvas _hitbox;
        ContentControl _focusableDummyElement;
        volatile float _currentWheelTick;
        volatile float _currentWheelTickOffset;
        long _currentColumnYPosTick;
        long _currentRowXPosTick;
        bool _manipulationStarted;
        bool _selectedIndexSetInternally;
        bool _deltaDirectionIsReverse;
        protected bool _uIItemsCreated;
        volatile int _elementsToLoadCount;

        #endregion

        #region PRIVATE PROPERTIES

        protected bool isXaxisNavigation
        {
            get
            {
                if (CarouselType == CarouselTypes.Wheel)
                {
                    switch (WheelAlignment)
                    {
                        case WheelAlignments.Bottom:
                        case WheelAlignments.Top:
                            return true;
                        case WheelAlignments.Left:
                        case WheelAlignments.Right:
                            return false;
                    }
                }
                else if (CarouselType == CarouselTypes.Row)
                {
                    return true;
                }
                else if (CarouselType == CarouselTypes.Column)
                {
                    return false;
                }
                return true;
            }
        }

        int itemsToScale
        {
            get
            {
                if (AdditionalItemsToScale > Density / 2)
                {
                    return Density / 2;
                }
                return AdditionalItemsToScale;
            }
        }

        int itemsToWarp
        {
            get
            {
                if (AdditionalItemsToWarp > Density / 2)
                {
                    return Density / 2;
                }
                return AdditionalItemsToWarp;
            }
        }

        protected bool useFliptych
        {
            get
            {
                return this.FliptychDegrees > 1 || this.FliptychDegrees < -1;
            }
        }

        int displaySelectedIndex
        {
            get
            {
                return (_carouselInsertPosition + (Density / 2)) % Density;
            }
        }

        float degrees
        {
            get
            {
                return 360.0f / Density;
            }
        }
        int currentStartIndexForwards
        {
            get
            {
                return Items != null ? (currentStartIndexBackwards + (Density - 1)) % Items.Count() : 0;
            }
        }

        int currentStartIndexBackwards
        {
            get
            {
                return Items != null ? Modulus((SelectedIndex - (Density / 2)), Items.Count()) : 0;
            }
        }

        #endregion

        #region PROPERTIES

        public bool IsCarouselMoving { get; private set; }

        public bool AreItemsLoaded { get; set; }

        /// <summary>
        /// The original object of the selected item in Items.
        /// </summary>
        public object SelectedItem { get; private set; }

        /// <returns>
        /// Returns the FrameworkElement of the SelectedItem.
        /// </returns>
        public FrameworkElement SelectedItemElement { get; private set; }

        /// <returns>
        /// Returns a collection of items generated from the ItemsSource.
        /// </returns>
        public IList<object> Items { get; private set; }

        /// <returns>
        /// Returns an integer value of the wheel diameter in pixels.
        /// </returns>
        public int WheelSize
        {
            get
            {
                var maxDimension = (_height > _width) ? _height : _width;
                return Convert.ToInt32(maxDimension);
            }
        }

        #endregion

        #region DEPENDENCY PROPERTIES

        ///// <summary>
        ///// Use this element's Manipulation event handlers for gesture controls.
        ///// </summary>
        //public Canvas Hitbox
        //{
        //    get { return (Canvas)GetValue(HitboxProperty); }
        //    set { SetValue(HitboxProperty, value); }
        //}

        //public static readonly DependencyProperty HitboxProperty = DependencyProperty.Register(nameof(Hitbox), typeof(Canvas), typeof(Carousel),
        //    new PropertyMetadata(null));

        public bool UseGestures
        {
            get
            {
                return (bool)base.GetValue(UseGesturesProperty);
            }
            set
            {
                base.SetValue(UseGesturesProperty, value);
            }
        }

        public static readonly DependencyProperty UseGesturesProperty = DependencyProperty.Register(nameof(UseGestures), typeof(bool), typeof(Carousel),
        new PropertyMetadata(true, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));


        /// <summary>
        /// Assign or bind the data source you wish to present to this property.
        /// </summary>
        public object ItemsSource
        {
            get
            {
                return (object)base.GetValue(ItemsSourceProperty);
            }
            set
            {
                base.SetValue(ItemsSourceProperty, value);
            }
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(Carousel),
            new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                if (e.NewValue is IEnumerable<object> newValue)
                {
                    control.Items = newValue.ToArray();
                }
                else
                {
                    control.Items = null;
                }
                control.Refresh();
            })));

        /// <summary>
        /// Assign a DataTempate here that will be used to present each item in your data collection.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(Carousel),
            new PropertyMetadata(null, OnCaptionPropertyChanged));


        /// <summary>
        /// Add a style to the style's first matching target type in the ItemTemplate's visual tree
        /// </summary>
        public Style ItemContentStyle
        {
            get { return (Style)GetValue(ItemContentStyleProperty); }
            set { SetValue(ItemContentStyleProperty, value); }
        }

        public static readonly DependencyProperty ItemContentStyleProperty = DependencyProperty.Register(nameof(ItemContentStyle), typeof(Style), typeof(Carousel),
            new PropertyMetadata(null, OnCaptionPropertyChanged));



        /// <summary>
        /// The index of the currently selected item in Items.
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return (int)base.GetValue(SelectedIndexProperty);
            }
            set
            {
                base.SetValue(SelectedIndexProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(Carousel),
    new PropertyMetadata(0, OnSelectedIndexChanged));

        /// <summary>
        /// This can be used to animate a CompositionBrush property of a custom control.
        /// The control must have a container parent with a name that starts with "CarouselItemMedia."
        /// The child element's property to animate must be of type CompositionColorBrush or CompositionLinearGradientBrush.
        /// These types can be found in the namespace Windows.UI.Composition for legacy UWP, or Microsoft.UI.Composition for WinUI.
        /// 
        /// Set your control's brush property to what you want for the deselected state. For the selected state, set 
        /// this this property.
        /// 
        /// An expression animation will be added which will create a smooth animated transition between the two brushes as
        /// items animate in and out of the selected item position.
        /// 
        /// Win2D may be required to create a custom control that allows text with a CompositionBrush.
        /// </summary>
        public Brush SelectedItemForegroundBrush
        {
            get
            {
                return (Brush)base.GetValue(SelectedItemForegroundBrushProperty);
            }
            set
            {
                base.SetValue(SelectedItemForegroundBrushProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedItemForegroundBrushProperty = DependencyProperty.Register(nameof(SelectedItemForegroundBrush), typeof(Brush), typeof(Carousel),
            new PropertyMetadata(null, OnCaptionPropertyChanged));


        /// <summary>
        /// MVVM: Bind this to a property and you can call the SelectionAnimation() method whenever you change its value to anything other than null.
        /// </summary>
        /// <example>
        /// Bind a property like this (One-Way) and call OnPropertyChanged to trigger it.
        /// <code>
        /// private bool _animationTrigger;
        /// public bool AnimationTrigger
        /// {
        ///     get { return !_animationTrigger;}
        /// }
        /// </code>
        /// </example>
        public object TriggerSelectionAnimation
        {
            get
            {
                return (object)base.GetValue(TriggerSelectionAnimationProperty);
            }
            set
            {
                base.SetValue(TriggerSelectionAnimationProperty, value);
            }
        }

        public static readonly DependencyProperty TriggerSelectionAnimationProperty = DependencyProperty.Register(nameof(TriggerSelectionAnimation), typeof(object), typeof(Carousel),
            new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
            {
                if (e.NewValue != null)
                {
                    var control = s as Carousel;
                    control.AnimateSelection();
                }
            })));

        /// <summary>
        /// The duration in milliseconds for the selection change animation.
        /// </summary>
        public int NavigationSpeed
        {
            get
            {
                return (int)base.GetValue(NavigationSpeedProperty);
            }
            set
            {
                base.SetValue(NavigationSpeedProperty, value);
            }
        }

        public static readonly DependencyProperty NavigationSpeedProperty = DependencyProperty.Register(nameof(NavigationSpeed), typeof(int), typeof(Carousel),
        new PropertyMetadata(500, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            if (e.NewValue is int newValue && newValue > 1)
            {
                control._delayedZIndexUpdateTimer.Interval = TimeSpan.FromMilliseconds(newValue / 2);
            }
            control.Refresh();
        })));

        /// <summary>
        /// For carousel configurations with overlapping items. When this is disabled, the ZIndex of items update immediately upon interaction. 
        /// Enable this so that it updates only after the animation is halfway complete (NavigationSpeed / 2).
        /// </summary>
        public bool ZIndexUpdateWaitsForAnimation
        {
            get
            {
                return (bool)base.GetValue(ZIndexUpdateWaitsForAnimationProperty);
            }
            set
            {
                base.SetValue(ZIndexUpdateWaitsForAnimationProperty, value);
            }
        }

        public static readonly DependencyProperty ZIndexUpdateWaitsForAnimationProperty = DependencyProperty.Register(nameof(ZIndexUpdateWaitsForAnimation), typeof(bool), typeof(Carousel),
        new PropertyMetadata(false));

        /// <summary>
        /// Sets the scale of the selected item to make it more prominent. The Framework's UIElement scaling does not do vector scaling of text or SVG images, unfortunately, so keep that in mind when using this.
        /// </summary>
        public double SelectedItemScale
        {
            get
            {
                return (double)base.GetValue(SelectedItemScaleProperty);
            }
            set
            {
                base.SetValue(SelectedItemScaleProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedItemScaleProperty = DependencyProperty.Register(nameof(SelectedItemScale), typeof(double), typeof(Carousel),
        new PropertyMetadata(1.0, OnCaptionPropertyChanged));

        /// <summary>
        /// Set the number of additional items surrounding the selected item to also scale, creating a 'falloff' effect.
        /// This value also applies to Fliptych.
        /// </summary>
        public int AdditionalItemsToScale
        {
            get
            {
                return (int)base.GetValue(AditionalItemsToScaleProperty);
            }
            set
            {
                base.SetValue(AditionalItemsToScaleProperty, value);
            }
        }

        public static readonly DependencyProperty AditionalItemsToScaleProperty = DependencyProperty.Register(nameof(AdditionalItemsToScale), typeof(int), typeof(Carousel),
         new PropertyMetadata(0, OnCaptionPropertyChanged));

        /// <summary>
        /// Set the number of additional items surrounding the selected item to also warp, creating a 'falloff' effect.
        /// </summary>
        public int AdditionalItemsToWarp
        {
            get
            {
                return (int)base.GetValue(AdditionalItemsToWarpProperty);
            }
            set
            {
                base.SetValue(AdditionalItemsToWarpProperty, value);
            }
        }

        public static readonly DependencyProperty AdditionalItemsToWarpProperty = DependencyProperty.Register(nameof(AdditionalItemsToWarp), typeof(int), typeof(Carousel),
            new PropertyMetadata(4, OnCaptionPropertyChanged));

        /// <summary>
        /// Sets the type of carousel. Chose a Column, a Row, or a Wheel.
        /// </summary>
        public CarouselTypes CarouselType
        {
            get
            {
                return (CarouselTypes)base.GetValue(CarouselTypeProperty);
            }
            set
            {
                base.SetValue(CarouselTypeProperty, value);
            }
        }
        public static readonly DependencyProperty CarouselTypeProperty = DependencyProperty.Register(nameof(CarouselType), typeof(CarouselTypes), typeof(Carousel),
        new PropertyMetadata(CarouselTypes.Row, OnCaptionPropertyChanged));

        /// <summary>
        /// When CarouselType is set to Wheel, use this property to set the conainer edge the wheel should be aligned to.
        /// </summary>
        public WheelAlignments WheelAlignment
        {
            get
            {
                return (WheelAlignments)base.GetValue(WheelOrientationProperty);
            }
            set
            {
                base.SetValue(WheelOrientationProperty, value);
            }
        }

        public static readonly DependencyProperty WheelOrientationProperty = DependencyProperty.Register(nameof(WheelAlignment), typeof(WheelAlignments), typeof(Carousel),
            new PropertyMetadata(WheelAlignments.Right, OnCaptionPropertyChanged));

        /// <summary>
        /// Then number of presented items to be in the UI at once. Increasing this value may help hide visual lag associated with the lazy loading while scrolling. Decreasing it may improve performance.
        /// When CarouselType is set to wheel, use this property to adjust the space between items.
        /// </summary>
        public int Density
        {
            get
            {
                var val = (int)base.GetValue(DensityProperty);
                int newValue = val;
                // min and max values
                if (val < 12)
                {
                    newValue = 12;
                }
                else if (val > 72)
                {
                    newValue = 72;
                }
                // ensure it's always divisible by 4.
                else if (val % 12 == 0)
                {
                    newValue = val;
                }
                else
                {
                    newValue = (val % 12) + val;
                }
                return newValue;
            }
            set
            {
                base.SetValue(DensityProperty, value);
            }
        }

        public static readonly DependencyProperty DensityProperty = DependencyProperty.Register(nameof(Density), typeof(int), typeof(Carousel),
        new PropertyMetadata(36, OnCaptionPropertyChanged));

        /// <summary>
        /// Amount of 3-D rotation to apply to deselected items, creating a fliptych or "Coverflow" type of effect.
        /// </summary>
        public double FliptychDegrees
        {
            get
            {
                return (double)base.GetValue(FliptychDegreesProperty);
            }
            set
            {
                base.SetValue(FliptychDegreesProperty, value);
            }
        }

        public static readonly DependencyProperty FliptychDegreesProperty = DependencyProperty.Register(nameof(FliptychDegrees), typeof(double), typeof(Carousel),
        new PropertyMetadata(0.0, OnCaptionPropertyChanged));

        /// <summary>
        /// For Row and Column mode only. Use this in combination with WarpCurve and AdditionalItemsToWarp to create an effect where the selected item juts out from the rest.
        /// </summary>
        public int WarpIntensity
        {
            get
            {
                return (int)base.GetValue(WarpIntensityProperty);
            }
            set
            {
                base.SetValue(WarpIntensityProperty, value);
            }
        }
        public static readonly DependencyProperty WarpIntensityProperty = DependencyProperty.Register(nameof(WarpIntensity), typeof(int), typeof(Carousel),
        new PropertyMetadata(0, OnCaptionPropertyChanged));

        /// <summary>
        /// For Row and Column mode only. Use this in combination with WarpIntensity and AdditionalItemsToWarp to create an effect where the selected item juts out from the rest.
        /// </summary>
        public double WarpCurve
        {
            get
            {
                return (double)base.GetValue(WarpCurveProperty);
            }
            set
            {
                base.SetValue(WarpCurveProperty, value);
            }
        }
        public static readonly DependencyProperty WarpCurveProperty = DependencyProperty.Register(nameof(WarpCurve), typeof(double), typeof(Carousel),
        new PropertyMetadata(.002, OnCaptionPropertyChanged));

        /// <summary>
        /// For Row and Column mode only. Increase or decrease (overlap) space between items.
        /// </summary>
        public int ItemGap
        {
            get
            {
                return (int)base.GetValue(ItemGapProperty);
            }
            set
            {
                base.SetValue(ItemGapProperty, value);
            }
        }

        public static readonly DependencyProperty ItemGapProperty = DependencyProperty.Register(nameof(ItemGap), typeof(int), typeof(Carousel),
        new PropertyMetadata(0, OnCaptionPropertyChanged));

        /// <summary>
        /// For MVVM. Bind this to a property and you can call the SelectNext() method whenever you change its value to anything other than null.
        /// </summary>
        /// <example>
        /// Bind to a property like this (One-Way) and call OnPropertyChanged to trigger it.
        /// <code>
        /// private bool _selectNextTrigger;
        /// public bool SelectNextTrigger
        /// {
        ///     get { return !_selectNextTrigger;}
        /// }
        /// </code>
        /// </example>
        public object SelectNextTrigger
        {
            get
            {
                return base.GetValue(SelectNextTriggerProperty);
            }
            set
            {
                base.SetValue(SelectNextTriggerProperty, value);
            }
        }

        public static readonly DependencyProperty SelectNextTriggerProperty = DependencyProperty.Register(nameof(SelectNextTrigger), typeof(object), typeof(Carousel),
        new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
        {
            if (e.NewValue != null)
            {
                var control = s as Carousel;
                control.ChangeSelection(false);
            }
        })));

        /// <summary>
        /// For MVVM. Bind this to a property and you can call the SelectPrevious() method whenever you change its value to anything other than null.
        /// </summary>
        /// <example>
        /// Bind to a property like this (One-Way) and call OnPropertyChanged to trigger it.
        /// <code>
        /// private bool _selectPreviousTrigger;
        /// public bool SelectPreviousTrigger
        /// {
        ///     get { return !_selectPreviousTrigger;}
        /// }
        /// </code>
        /// </example>
        public object SelectPreviousTrigger
        {
            get
            {
                return base.GetValue(SelectPreviousTriggerProperty);
            }
            set
            {
                base.SetValue(SelectPreviousTriggerProperty, value);
            }
        }

        public static readonly DependencyProperty SelectPreviousTriggerProperty = DependencyProperty.Register(nameof(SelectPreviousTrigger), typeof(object), typeof(Carousel),
        new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
        {
            if (e.NewValue != null)
            {
                var control = s as Carousel;
                control.ChangeSelection(true);
            }
        })));

        /// <summary>
        /// For MVVM. Bind this to a property and you can call the ManipulationStarted() method whenever you change its value to anything other than null.
        /// </summary>
        /// <example>
        /// Bind to a property like this (One-Way) and call OnPropertyChanged to trigger it.
        /// <code>
        /// private bool _manipulationStartedTrigger;
        /// public bool ManipulationStartedTrigger
        /// {
        ///     get { return !_manipulationStartedTrigger;}
        /// }
        /// </code>
        /// </example>

        public object ManipulationStartedTrigger
        {
            get
            {
                return base.GetValue(ManipulationStartedTriggerProperty);
            }
            set
            {
                base.SetValue(ManipulationStartedTriggerProperty, value);
            }
        }

        public static readonly DependencyProperty ManipulationStartedTriggerProperty = DependencyProperty.Register(nameof(ManipulationStartedTrigger), typeof(object), typeof(Carousel),
        new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
        {
            if (e.NewValue != null)
            {
                var control = s as Carousel;
                control.StartManipulationMode();
            }
        })));

        /// <summary>
        /// For MVVM. Bind this to a property and you can call the ManipulationCompleted() method whenever you change its value to anything other than null.
        /// </summary>
        /// <example>
        /// Bind to a property like this (One-Way) and call OnPropertyChanged to trigger it.
        /// <code>
        /// private bool _manipulationCompletedTrigger;
        /// public bool ManipulationCompletedTrigger
        /// {
        ///     get { return !_manipulationCompletedTrigger;}
        /// }
        /// </code>
        /// </example>
        public object ManipulationCompletedTrigger
        {
            get
            {
                return base.GetValue(ManipulationCompletedTriggerProperty);
            }
            set
            {
                base.SetValue(ManipulationCompletedTriggerProperty, value);
            }
        }

        public static readonly DependencyProperty ManipulationCompletedTriggerProperty = DependencyProperty.Register(nameof(ManipulationCompletedTrigger), typeof(object), typeof(Carousel),
        new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
        {
            if (e.NewValue != null)
            {
                var control = s as Carousel;
                control.StopManipulationMode();
            }
        })));

        /// <summary>
        /// Use a ManipulationDelta event to update this value to control the carousel with a dragging gesture or analog stick of a gamepade.
        /// It is important to call StartManipulationMode() and StopManipulationMode before and after (respectively) updating this with a ManipulationDelta.
        /// Use ManipulationStarted and ManipulationCompleted events accordingly.
        /// 
        /// This is a float value and thus cannot be set directly in XAML, data binding or a converter is required.
        /// </summary>
        public float CarouselRotationAngle
        {
            get { return (float)GetValue(CarouselRotationAngleProperty); }
            set { SetValue(CarouselRotationAngleProperty, value); }
        }

        public static readonly DependencyProperty CarouselRotationAngleProperty = DependencyProperty.Register(nameof(CarouselRotationAngle), typeof(float), typeof(Carousel),
            new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                if (control._inputAllowed && control.IsCarouselMoving && e.NewValue is float v)
                {
                    control.UpdateWheelRotation(v);
                }
            })));

        /// <summary>
        /// Use a ManipulationDelta event to update this value to control the carousel with a dragging gesture or analog stick of a gamepade.
        /// It is important to call StartManipulationMode() and StopManipulationMode before and after (respectively) updating this with a ManipulationDelta.
        /// Use ManipulationStarted and ManipulationCompleted events accordingly.
        /// </summary>
        public double CarouselPositionY
        {
            get { return (double)GetValue(CarouselPositionYProperty); }
            set { SetValue(CarouselPositionYProperty, value); }
        }

        public static readonly DependencyProperty CarouselPositionYProperty = DependencyProperty.Register(nameof(CarouselPositionY), typeof(double), typeof(Carousel),
            new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                if (control._inputAllowed && control.IsCarouselMoving && e.NewValue is double v)
                {
                    control.UpdateCarouselVerticalScrolling(Convert.ToSingle(v));
                }
            })));

        /// <summary>
        /// Use a ManipulationDelta event to update this value to control the carousel with a dragging gesture or analog stick of a gamepade.
        /// It is important to call StartManipulationMode() and StopManipulationMode before and after (respectively) updating this with a ManipulationDelta.
        /// Use ManipulationStarted and ManipulationCompleted events accordingly.
        /// </summary>
        public double CarouselPositionX
        {
            get { return (double)GetValue(CarouselPositionXProperty); }
            set { SetValue(CarouselPositionXProperty, value); }
        }

        public static readonly DependencyProperty CarouselPositionXProperty = DependencyProperty.Register(nameof(CarouselPositionX), typeof(double), typeof(Carousel),
            new PropertyMetadata(null, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                if (control._inputAllowed && control.IsCarouselMoving && e.NewValue is double v)
                {
                    control.UpdateCarouselHorizontalScrolling(Convert.ToSingle(v));
                }
            })));

        #endregion

        #region TIMER METHODS

        private void _delayedRefreshTimer_Tick(object sender, object e)
        {
            _delayedRefreshTimer.Stop();
            LoadNewCarousel();
        }

        private void _delayedZIndexUpdateTimer_Tick(object sender, object e)
        {
            _delayedZIndexUpdateTimer.Stop();
            UpdateZIndices();
        }

        #endregion

        #region ANIMATION METHODS
        private void AddImplicitWheelSnapToAnimation(Visual visual)
        {
            if (NavigationSpeed != 0)
            {
                ImplicitAnimationCollection implicitAnimations = visual.Compositor.CreateImplicitAnimationCollection();
                visual.ImplicitAnimations = implicitAnimations;
                int duration = (NavigationSpeed / 2 < 500) ? NavigationSpeed / 2 : 500;
                var animationRotate = visual.Compositor.CreateScalarKeyFrameAnimation();
                var easing = animationRotate.Compositor.CreateLinearEasingFunction();
                animationRotate.InsertExpressionKeyFrame(1f, "this.FinalValue", easing);
                animationRotate.Target = "RotationAngleInDegrees";
                animationRotate.Duration = TimeSpan.FromMilliseconds(duration);
                implicitAnimations["RotationAngleInDegrees"] = animationRotate;
                visual.ImplicitAnimations = implicitAnimations;
            }
        }

        private void ClearImplicitOffsetAnimations(float xDiff, float yDiff, bool clearAll = false)
        {
            for (int i = (Density - 1); i > -1; i--)
            {
                int idx = Modulus(((Density - 1) - i), Density);
                if (_itemsLayerGrid.Children[idx] is FrameworkElement itemElement)
                {
                    var itemElementVisual = ElementCompositionPreview.GetElementVisual(itemElement);
                    if (itemElementVisual.ImplicitAnimations != null)
                    {
                        if (clearAll)
                        {
                            itemElementVisual.ImplicitAnimations.Clear();
                        }
                        else
                        {
                            itemElementVisual.ImplicitAnimations.Remove("Offset");
                        }
                    }
                    itemElementVisual.Offset = new Vector3(itemElementVisual.Offset.X + xDiff, itemElementVisual.Offset.Y + yDiff, itemElementVisual.Offset.Z);
                }
            }
        }

        protected void StartExpressionItemAnimations(FrameworkElement element, int? slotNum)
        {
            if (element == null) { return; }
            var visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = Window.Current.Compositor;
            ScalarNode distanceAsPercentOfSelectionAreaThreshold = null;
            ScalarNode scaleThresholdDistanceRaw = null;
            BooleanNode distanceIsNegativeValue = null;
            BooleanNode isWithinScaleThreshold = null;
            float selectionAreaThreshold = 0;

            if (CarouselType == CarouselTypes.Wheel && slotNum != null)
            {
                selectionAreaThreshold = itemsToScale == 0 ? degrees : degrees * itemsToScale;

                var slotDegrees = ((slotNum + (Density / 2)) % Density) * degrees;

                float wheelDegreesWhenItemIsSelected = Convert.ToSingle(slotDegrees);

                using (ScalarNode wheelAngle = _dynamicGridVisual.GetReference().RotationAngleInDegrees)
                {
                    switch (WheelAlignment)
                    {
                        case WheelAlignments.Right:
                            scaleThresholdDistanceRaw = ExpressionFunctions.Mod((wheelAngle - wheelDegreesWhenItemIsSelected), 360);
                            break;
                        case WheelAlignments.Top:
                            scaleThresholdDistanceRaw = ExpressionFunctions.Mod((wheelAngle - wheelDegreesWhenItemIsSelected), 360);
                            break;
                        case WheelAlignments.Left:
                            scaleThresholdDistanceRaw = ExpressionFunctions.Mod((wheelAngle + wheelDegreesWhenItemIsSelected), 360);
                            break;
                        case WheelAlignments.Bottom:
                            scaleThresholdDistanceRaw = ExpressionFunctions.Mod((wheelAngle + wheelDegreesWhenItemIsSelected), 360);
                            break;
                    }

                    using (ScalarNode distanceToZero = ExpressionFunctions.Abs(scaleThresholdDistanceRaw))
                    using (ScalarNode distanceTo360 = 360 - distanceToZero)
                    using (BooleanNode isClosestToZero = distanceToZero <= distanceTo360)
                    using (ScalarNode distanceInDegrees = ExpressionFunctions.Conditional(isClosestToZero, distanceToZero, distanceTo360))
                    {
                        distanceAsPercentOfSelectionAreaThreshold = distanceInDegrees / selectionAreaThreshold;
                        switch (WheelAlignment)
                        {
                            case WheelAlignments.Top:
                            case WheelAlignments.Bottom:
                                distanceIsNegativeValue = ExpressionFunctions.Abs(scaleThresholdDistanceRaw) < 180;
                                break;
                            case WheelAlignments.Left:
                                distanceIsNegativeValue = ExpressionFunctions.Abs(ExpressionFunctions.Mod((wheelAngle + wheelDegreesWhenItemIsSelected - 90), 360)) < 180;
                                break;
                            case WheelAlignments.Right:
                                distanceIsNegativeValue = ExpressionFunctions.Abs(ExpressionFunctions.Mod((wheelAngle - wheelDegreesWhenItemIsSelected + 90), 360)) < 180;
                                break;
                        }

                        isWithinScaleThreshold = distanceInDegrees < selectionAreaThreshold;
                        StartScaleAnimation(element, distanceAsPercentOfSelectionAreaThreshold, isWithinScaleThreshold);
                    }
                }
            }
            else if (CarouselType == CarouselTypes.Row || CarouselType == CarouselTypes.Column)
            {
                selectionAreaThreshold = isXaxisNavigation ? AdditionalItemsToScale * (_maxItemWidth + ItemGap) : AdditionalItemsToScale * (_maxItemHeight + ItemGap);
                if (selectionAreaThreshold == 0)
                {
                    selectionAreaThreshold = isXaxisNavigation ? _maxItemWidth + ItemGap : _maxItemHeight + ItemGap;
                }

                using (Vector3Node offset = visual.GetReference().Offset)
                {
                    scaleThresholdDistanceRaw = this.isXaxisNavigation ? offset.X / selectionAreaThreshold : offset.Y / selectionAreaThreshold;

                    distanceAsPercentOfSelectionAreaThreshold = ExpressionFunctions.Abs(scaleThresholdDistanceRaw);

                    distanceIsNegativeValue = scaleThresholdDistanceRaw < 0;
                    isWithinScaleThreshold = isXaxisNavigation ? offset.X > -selectionAreaThreshold & offset.X < selectionAreaThreshold : offset.Y > -selectionAreaThreshold & offset.Y < selectionAreaThreshold;

                    StartScaleAnimation(element, distanceAsPercentOfSelectionAreaThreshold, isWithinScaleThreshold);

                    if (WarpIntensity != 0)
                    {
                        var warpItemsthreshold = isXaxisNavigation ? AdditionalItemsToWarp * (_maxItemWidth + ItemGap) : AdditionalItemsToWarp * (_maxItemHeight + ItemGap);
                        if (warpItemsthreshold == 0)
                        {
                            warpItemsthreshold = isXaxisNavigation ? _maxItemWidth + ItemGap : _maxItemHeight + ItemGap;
                        }
                        using (ScalarNode warpThresholdDistanceRaw = this.isXaxisNavigation ? offset.X / warpItemsthreshold : offset.Y / warpItemsthreshold)
                        using (ScalarNode distanceAsPercentOfWarpThreshold = ExpressionFunctions.Abs(warpThresholdDistanceRaw))
                        using (BooleanNode isWithinWarpThreshold = isXaxisNavigation ? offset.X > -warpItemsthreshold & offset.X < warpItemsthreshold : offset.Y > -warpItemsthreshold & offset.Y < warpItemsthreshold)
                        using (ScalarNode y = WarpIntensity - (distanceAsPercentOfWarpThreshold * WarpIntensity))
                        using (ScalarNode WarpOffset = Convert.ToSingle(-WarpCurve) * warpThresholdDistanceRaw * warpThresholdDistanceRaw + WarpIntensity)
                        using (ScalarNode finalWarpValue = ExpressionFunctions.Conditional(isWithinWarpThreshold, y * ExpressionFunctions.Abs(y) * (float)WarpCurve, 0))
                        {
                            if (isXaxisNavigation)
                            {
                                visual.StartAnimation("Translation.Y", finalWarpValue);
                            }
                            else
                            {
                                visual.StartAnimation("Translation.X", finalWarpValue);
                            }
                        }
                    }
                }
            }

            UIElement child = null;

            // Fliptych
            if (useFliptych && CarouselType != CarouselTypes.Wheel) // TODO: Implement Fliptych on Wheel
            {
                if (element.Transform3D == null)
                {
                    element.Transform3D = new PerspectiveTransform3D { Depth = 1000 };
                }
                if (element is UserControl uc && uc.Content is UIElement ucElement)
                {
                    child = ucElement;
                }
                else if (element is ContentControl cc && cc.Content is UIElement ccElement)
                {
                    child = ccElement;
                }
                else if (element is Grid g && g.Children.Count == 1 && g.Children.First() is UIElement gElement)
                {
                    child = gElement;
                }
                else if (element is Panel p && p.Children.Count == 1 && p.Children.First() is UIElement pElement)
                {
                    child = pElement;
                }
                else if (element is Canvas c && c.Children.Count == 1 && c.Children.First() is UIElement cElement)
                {
                    child = cElement;
                }

                if (child != null)
                {
                    var childVisual = ElementCompositionPreview.GetElementVisual(child);
                    var fliptychDegrees = isXaxisNavigation ? Convert.ToSingle(FliptychDegrees) : Convert.ToSingle(-FliptychDegrees);
                    if (CarouselType == CarouselTypes.Wheel) { fliptychDegrees *= -1; }
                    childVisual.RotationAxis = isXaxisNavigation ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
                    childVisual.CenterPoint = new Vector3(_maxItemWidth / 2, _maxItemHeight / 2, 0);
                    using (ScalarNode rotatedValue = ExpressionFunctions.Conditional(distanceIsNegativeValue, fliptychDegrees, -fliptychDegrees))
                    using (ScalarNode finalValue = ExpressionFunctions.Conditional(isWithinScaleThreshold, distanceAsPercentOfSelectionAreaThreshold * rotatedValue, rotatedValue))
                    {
                        childVisual.StartAnimation("RotationAngleInDegrees", finalValue);
                    }
                }
            }
            SetColorAnimation(distanceAsPercentOfSelectionAreaThreshold, isWithinScaleThreshold, element);

            distanceAsPercentOfSelectionAreaThreshold.Dispose();
            scaleThresholdDistanceRaw.Dispose();
            distanceIsNegativeValue.Dispose();
            isWithinScaleThreshold.Dispose();
            scaleThresholdDistanceRaw = null;
            distanceIsNegativeValue = null;
            isWithinScaleThreshold = null;
        }

        /// <summary>
        /// Override this to access your items' SpriteVisual so you can animate Size instead of Scale, maintaining quality of 
        /// vector or higher resolution graphics.
        /// </summary>
        protected virtual void StartScaleAnimation(UIElement element, ScalarNode distanceAsPercentOfScaleThreshold, BooleanNode isWithinScaleThreshold)
        {
            if (SelectedItemScale != 1)
            {
                var visual = ElementCompositionPreview.GetElementVisual(element);
                var scaleRange = (float)SelectedItemScale - 1;
                using (ScalarNode scalePercent = scaleRange * (1 - distanceAsPercentOfScaleThreshold) + 1)
                using (ScalarNode finalScaleValue = ExpressionFunctions.Conditional(isWithinScaleThreshold, scalePercent, 1))
                using (Vector3Node finalScaleValueVector3 = ExpressionFunctions.Vector3(finalScaleValue, finalScaleValue, 1))
                {
                    visual.StartAnimation(nameof(Visual.Scale), finalScaleValueVector3);
                }

            }
        }

        /// <summary>
        /// For better performance it may be worth extending Carousel and overriding this method with one that targets the specific class and property you want.
        /// </summary>
        protected virtual void SetColorAnimation(ScalarNode distanceAsPercentOfScaleThreshold, BooleanNode isWithinScaleThreshold, FrameworkElement fe)
        {
            List<FrameworkElement> elements = fe.FindDescendants<FrameworkElement>().ToList();
            elements.Add(fe);
            foreach (var element in elements)
            {
                var t = element.GetType();
                var props = t.GetProperties();
                foreach (var prop in props)
                {
                    if (prop.PropertyType == typeof(CompositionColorBrush) || prop.PropertyType == typeof(CompositionLinearGradientBrush))
                    {
                        if (SelectedItemForegroundBrush is SolidColorBrush solidColorBrush)
                        {
                            if (prop.GetValue(element) is CompositionColorBrush compositionSolid)
                            {
                                using (ColorNode deselectedColor = ExpressionFunctions.ColorRgb(compositionSolid.Color.A,
                                   compositionSolid.Color.R, compositionSolid.Color.G, compositionSolid.Color.B))
                                using (ColorNode selectedColor = ExpressionFunctions.ColorRgb(solidColorBrush.Color.A,
                                   solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B))
                                using (ColorNode colorLerp = ExpressionFunctions.ColorLerp(selectedColor, deselectedColor, distanceAsPercentOfScaleThreshold))
                                using (ColorNode finalColorExp = ExpressionFunctions.Conditional(isWithinScaleThreshold, colorLerp, deselectedColor))
                                {
                                    compositionSolid.StartAnimation("Color", finalColorExp);
                                }

                            }
                            else if (prop.GetValue(element) is CompositionLinearGradientBrush compositionGradient)
                            {
                                for (int i = 0; i < compositionGradient.ColorStops.Count; i++)
                                {
                                    using (CompositionColorGradientStop targetStop = compositionGradient.ColorStops[i])
                                    using (ColorNode deselectedColor = ExpressionFunctions.ColorRgb(targetStop.Color.A, targetStop.Color.R, targetStop.Color.G, targetStop.Color.B))
                                    using (ColorNode selectedColor = ExpressionFunctions.ColorRgb(solidColorBrush.Color.A, solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B))
                                    using (ColorNode colorLerp = ExpressionFunctions.ColorLerp(selectedColor, deselectedColor, distanceAsPercentOfScaleThreshold))
                                    using (ColorNode finalColorExp = ExpressionFunctions.Conditional(isWithinScaleThreshold, colorLerp, deselectedColor))
                                    {
                                        targetStop.StartAnimation("Color", finalColorExp);
                                    }
                                }
                            }
                        }
                        else if (SelectedItemForegroundBrush is LinearGradientBrush linearGradientBrush && prop.GetValue(element) is CompositionLinearGradientBrush compositionGradient)
                        {
                            if (linearGradientBrush.GradientStops.Count == compositionGradient.ColorStops.Count)
                            {
                                for (int i = 0; i < compositionGradient.ColorStops.Count; i++)
                                {
                                    using (CompositionColorGradientStop targetStop = compositionGradient.ColorStops[i])
                                    using (ColorNode deselectedColor = ExpressionFunctions.ColorRgb(targetStop.Color.A, targetStop.Color.R, targetStop.Color.G, targetStop.Color.B))
                                    {
                                        GradientStop sourceStop = linearGradientBrush.GradientStops[i];
                                        using (ColorNode selectedColor = ExpressionFunctions.ColorRgb(sourceStop.Color.A, sourceStop.Color.R, sourceStop.Color.G, sourceStop.Color.B))
                                        using (ColorNode colorLerp = ExpressionFunctions.ColorLerp(selectedColor, deselectedColor, distanceAsPercentOfScaleThreshold))
                                        using (ColorNode finalColorExp = ExpressionFunctions.Conditional(isWithinScaleThreshold, colorLerp, deselectedColor))
                                        {
                                            targetStop.StartAnimation("Color", finalColorExp);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AddStandardImplicitItemAnimation(Visual visual)
        {
            AddStandardImplicitItemAnimation(visual, NavigationSpeed, false);
        }

        protected virtual void AddStandardImplicitItemAnimation(Visual visual, int durationMilliseconds, bool rotation)
        {
            if (NavigationSpeed != 0)
            {
                if (visual.ImplicitAnimations == null)
                {
                    visual.ImplicitAnimations = visual.Compositor.CreateImplicitAnimationCollection();
                }

                var scaleAnimation = visual.Compositor.CreateVector3KeyFrameAnimation();
                var scaleEasing = scaleAnimation.Compositor.CreateLinearEasingFunction();
                scaleAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", scaleEasing);
                scaleAnimation.Target = nameof(visual.Scale);
                scaleAnimation.Duration = TimeSpan.FromMilliseconds(durationMilliseconds);
                if (!visual.ImplicitAnimations.ContainsKey(nameof(visual.Scale)))
                {
                    visual.ImplicitAnimations[nameof(visual.Scale)] = scaleAnimation;
                }

                var offsetAnimation = visual.Compositor.CreateVector3KeyFrameAnimation();
                var offsetEasing = offsetAnimation.Compositor.CreateLinearEasingFunction();
                offsetAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", offsetEasing);
                offsetAnimation.Target = nameof(visual.Offset);
                offsetAnimation.Duration = TimeSpan.FromMilliseconds(durationMilliseconds);
                if (!visual.ImplicitAnimations.ContainsKey(nameof(visual.Offset)))
                {
                    visual.ImplicitAnimations[nameof(visual.Offset)] = offsetAnimation;
                }

                var opacityAnimation = visual.Compositor.CreateScalarKeyFrameAnimation();
                var opacityEasing = opacityAnimation.Compositor.CreateLinearEasingFunction();
                opacityAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", opacityEasing);
                opacityAnimation.Target = nameof(visual.Opacity);
                opacityAnimation.Duration = TimeSpan.FromMilliseconds(durationMilliseconds);
                if (!visual.ImplicitAnimations.ContainsKey(nameof(visual.Opacity)))
                {
                    visual.ImplicitAnimations[nameof(visual.Opacity)] = opacityAnimation;
                }

                if (rotation)
                {
                    var rotateAnimation = visual.Compositor.CreateScalarKeyFrameAnimation();
                    var rotationEasing = rotateAnimation.Compositor.CreateLinearEasingFunction();
                    rotateAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", rotationEasing);
                    rotateAnimation.Target = nameof(visual.RotationAngleInDegrees);
                    rotateAnimation.Duration = TimeSpan.FromMilliseconds(durationMilliseconds);
                    if (!visual.ImplicitAnimations.ContainsKey(nameof(visual.RotationAngleInDegrees)))
                    {
                        visual.ImplicitAnimations[nameof(visual.RotationAngleInDegrees)] = rotateAnimation;
                    }
                }
            }
        }

        private void AddImplicitWheelRotationAnimation(Visual visual)
        {
            if (NavigationSpeed > 0)
            {
                ImplicitAnimationCollection implicitAnimations = visual.Compositor.CreateImplicitAnimationCollection();
                var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
                animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
                animation.Target = "RotationAngleInDegrees";
                animation.Duration = TimeSpan.FromMilliseconds(NavigationSpeed);
                implicitAnimations["RotationAngleInDegrees"] = animation;
                visual.ImplicitAnimations = implicitAnimations;
            }
        }

        private void RemoveImplicitWheelRotationAnimation(Visual visual)
        {
            if (visual.ImplicitAnimations != null)
            {
                visual.ImplicitAnimations.Clear();
            }
        }

        #endregion

        #region NAVIGATION METHODS

        /// <summary>
        /// This must be called before updating the carousel with a ManipulationData event. MMVM implementations can use the trigger property to call it.
        /// </summary>
        public void StartManipulationMode()
        {
            _manipulationStarted = true;
            IsCarouselMoving = true;
            RemoveImplicitWheelRotationAnimation(_dynamicGridVisual);
            OnCarouselMovingStateChanged();
        }

        /// <summary>
        /// This must be called after updating the carousel with a ManipulationData event. MMVM implementations can use the trigger property to call it.
        /// </summary>
        public async void StopManipulationMode()
        {
            IsCarouselMoving = false;
            await StopCarouselMoving().ConfigureAwait(false);
            OnCarouselMovingStateChanged();
        }

        void UpdateCarouselVerticalScrolling(float newValue)
        {
            if (Items != null && _itemsLayerGrid.Children.Count == Density)
            {
                _scrollValue = newValue - _scrollSnapshot;
                _scrollSnapshot = newValue;
                var threshold = _maxItemHeight + ItemGap;

                if (_manipulationStarted)
                {
                    _manipulationStarted = false;
                    _deltaDirectionIsReverse = _scrollValue < 0;
                    if (newValue > 0)
                    {
                        Interlocked.Add(ref _currentColumnYPosTick, -threshold / 2);
                    }
                    else if (newValue < 0)
                    {
                        Interlocked.Add(ref _currentColumnYPosTick, threshold / 2);
                    }
                }

                else if ((_deltaDirectionIsReverse && _scrollValue > 0) || (!_deltaDirectionIsReverse && _scrollValue < 0))
                {
                    _deltaDirectionIsReverse = !_deltaDirectionIsReverse;
                    if (_deltaDirectionIsReverse)
                    {
                        Interlocked.Add(ref _currentColumnYPosTick, threshold);
                    }
                    else
                    {
                        Interlocked.Add(ref _currentColumnYPosTick, -threshold);
                    }
                }

                while (newValue > _currentColumnYPosTick + threshold)
                {
                    ChangeSelection(true);
                    Interlocked.Add(ref _currentColumnYPosTick, threshold);
                }

                while (newValue < _currentColumnYPosTick - threshold)
                {
                    ChangeSelection(false);
                    Interlocked.Add(ref _currentColumnYPosTick, -threshold);
                }

                ClearImplicitOffsetAnimations(0, _scrollValue);
            }
        }

        void UpdateCarouselHorizontalScrolling(float newValue)
        {
            if (Items != null && _itemsLayerGrid.Children.Count == Density)
            {
                _scrollValue = newValue - _scrollSnapshot;
                _scrollSnapshot = newValue;
                var threshold = _maxItemWidth + ItemGap;

                if (_manipulationStarted)
                {
                    _manipulationStarted = false;
                    _deltaDirectionIsReverse = _scrollValue < 0;
                    if (newValue > 0)
                    {
                        Interlocked.Add(ref _currentRowXPosTick, -threshold / 2);
                    }
                    else if (newValue < 0)
                    {
                        Interlocked.Add(ref _currentRowXPosTick, threshold / 2);
                    }
                }

                else if ((_deltaDirectionIsReverse && _scrollValue > 0) || (!_deltaDirectionIsReverse && _scrollValue < 0))
                {
                    _deltaDirectionIsReverse = !_deltaDirectionIsReverse;
                    if (_deltaDirectionIsReverse)
                    {
                        Interlocked.Add(ref _currentRowXPosTick, threshold);
                    }
                    else
                    {
                        Interlocked.Add(ref _currentRowXPosTick, -threshold);
                    }
                }

                while (newValue > _currentRowXPosTick + threshold)
                {
                    ChangeSelection(true);
                    Interlocked.Add(ref _currentRowXPosTick, threshold);
                }

                while (newValue < _currentRowXPosTick - threshold)
                {
                    ChangeSelection(false);
                    Interlocked.Add(ref _currentRowXPosTick, -threshold);
                }

                ClearImplicitOffsetAnimations(_scrollValue, 0);
            }
        }

        void UpdateWheelRotation(float newValue)
        {
            if (Items != null && _itemsLayerGrid.Children.Count == Density)
            {
                _scrollValue = newValue - _scrollSnapshot;
                _scrollSnapshot = newValue;
                if (_manipulationStarted)
                {
                    _manipulationStarted = false;
                    _deltaDirectionIsReverse = _scrollValue < 0;
                    if (_deltaDirectionIsReverse)
                    {
                        _currentWheelTickOffset = degrees / 2;
                    }
                    else
                    {
                        _currentWheelTickOffset = -degrees / 2;
                    }
                }
                else if ((_deltaDirectionIsReverse && _scrollValue > 0) || (!_deltaDirectionIsReverse && _scrollValue < 0))
                {
                    _deltaDirectionIsReverse = !_deltaDirectionIsReverse;
                    if (_deltaDirectionIsReverse)
                    {
                        _currentWheelTickOffset += degrees;
                    }
                    else
                    {
                        _currentWheelTickOffset -= degrees;
                    }
                }

                _dynamicGridVisual.RotationAngleInDegrees = newValue;
                while (newValue > _currentWheelTick + degrees + _currentWheelTickOffset)
                {
                    _currentWheelTick += degrees;
                    switch (WheelAlignment)
                    {
                        case WheelAlignments.Right:
                        case WheelAlignments.Top:
                            ChangeSelection(false);
                            break;
                        case WheelAlignments.Left:
                        case WheelAlignments.Bottom:
                            ChangeSelection(true);
                            break;
                    }
                }
                while (newValue < _currentWheelTick - degrees + _currentWheelTickOffset)
                {
                    _currentWheelTick -= degrees;
                    switch (WheelAlignment)
                    {
                        case WheelAlignments.Right:
                        case WheelAlignments.Top:
                            ChangeSelection(true);
                            break;
                        case WheelAlignments.Left:
                        case WheelAlignments.Bottom:
                            ChangeSelection(false);
                            break;
                    }
                }
                ClearImplicitOffsetAnimations(0, 0, true);
            }
        }

        async Task StopCarouselMoving()
        {
            var selectedIdx = Modulus(((Density - 1) - (displaySelectedIndex)), Density);
            if (CarouselType == CarouselTypes.Wheel)
            {
                AddImplicitWheelSnapToAnimation(_dynamicGridVisual);
                _dynamicGridVisual.RotationAngleInDegrees = _currentWheelTick;
                var animation = (ScalarKeyFrameAnimation)_dynamicGridVisual.ImplicitAnimations["RotationAngleInDegrees"];
                await Task.Delay(animation.Duration);
                _dynamicGridVisual.ImplicitAnimations.Clear();
                _currentWheelTick = _currentWheelTick % 360;
                _dynamicGridVisual.RotationAngleInDegrees = _currentWheelTick;
                CarouselRotationAngle = _currentWheelTick;
            }

            var offsetVertical = _maxItemHeight + ItemGap;
            var offsetHorizontal = _maxItemWidth + ItemGap;

            for (int i = -((Density / 2) - 1); i <= (Density / 2); i++)
            {
                int j = Modulus((selectedIdx + i), Density);
                if (_itemsLayerGrid != null && _itemsLayerGrid.Children[j] is FrameworkElement itemElement)
                {
                    var itemElementVisual = ElementCompositionPreview.GetElementVisual(itemElement);
                    AddStandardImplicitItemAnimation(itemElementVisual);
                    if (CarouselType == CarouselTypes.Column)
                    {
                        var currentX = itemElementVisual.Offset.X;
                        itemElementVisual.Offset = new System.Numerics.Vector3(currentX, offsetVertical * -i, (Density - Math.Abs(i)));
                    }
                    else if (CarouselType == CarouselTypes.Row)
                    {
                        var currentY = itemElementVisual.Offset.Y;
                        itemElementVisual.Offset = new System.Numerics.Vector3(offsetHorizontal * -i, currentY, (Density - Math.Abs(i)));
                    }
                }
            }
            AddImplicitWheelRotationAnimation(_dynamicGridVisual);
            CarouselPositionY = 0;
            _currentColumnYPosTick = 0;
            CarouselPositionX = 0;
            _currentRowXPosTick = 0;
            _scrollValue = 0;
            _scrollSnapshot = 0;
        }

        FrameworkElement CreateCarouselItemElement(int i, int playlistIdx)
        {
            if (ItemTemplate != null)
            {
                FrameworkElement element = ItemTemplate.LoadContent() as FrameworkElement;
                if (ItemContentStyle != null)
                {
                    var child = element.FindDescendants<FrameworkElement>().Where(x => x.GetType() == ItemContentStyle.TargetType).FirstOrDefault();
                    if (child != null)
                    {
                        Convert.ChangeType(child, ItemContentStyle.TargetType);
                        child.Style = ItemContentStyle;
                        child.Tag = true;
                    }
                }
                element.DataContext = Items[playlistIdx];
                int w = 0;
                int h = 0;
                if (Double.IsNaN(element.Height) || Double.IsNaN(element.Width))
                {
                    if (!Double.IsInfinity(element.MaxWidth) && !Double.IsInfinity(element.MaxHeight))
                    {
                        w = Convert.ToInt32(element.MaxWidth);
                        h = Convert.ToInt32(element.MaxHeight);
                    }
                    else if (element is UserControl uc && uc.Content is FrameworkElement childElement)
                    {
                        if (Double.IsNaN(childElement.Width) || Double.IsNaN(childElement.Height))
                        {
                            if (!Double.IsInfinity(childElement.MaxWidth) && !Double.IsInfinity(childElement.MaxHeight))
                            {
                                w = Convert.ToInt32(childElement.MaxWidth + childElement.Margin.Left + childElement.Margin.Right);
                                h = Convert.ToInt32(childElement.MaxHeight + childElement.Margin.Top + childElement.Margin.Bottom);
                            }
                        }
                        else
                        {
                            w = Convert.ToInt32(childElement.Width + childElement.Margin.Left + childElement.Margin.Right);
                            h = Convert.ToInt32(childElement.Height + childElement.Margin.Top + childElement.Margin.Bottom);
                        }
                    }
                }
                else
                {
                    w = Convert.ToInt32(element.Width + element.Margin.Left + element.Margin.Right);
                    h = Convert.ToInt32(element.Height + element.Margin.Top + element.Margin.Bottom);
                }

                if (w == 0 || h == 0)
                {
                    Debug.WriteLine("Tilted Carousel: Item Height and Width (or MaxHeight and MaxWidth) must be set.");
                    return null;
                }

                if (_maxItemWidth < element.MaxWidth) { _maxItemWidth = w; }
                if (_maxItemHeight < element.MaxHeight) { _maxItemHeight = h; }
                PositionElement(element, i, (float)w, (float)h);
                return element;
            }
            return null;
        }

        private void Element_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0 && sender is FrameworkElement element)
            {
                element.SizeChanged -= Element_SizeChanged;
                if (_maxItemHeight < element.ActualHeight) { _maxItemHeight = Convert.ToInt32(element.ActualHeight); }
                if (_maxItemWidth < element.ActualWidth) { _maxItemWidth = Convert.ToInt32(element.ActualWidth); }
                if (_uIItemsCreated)
                {
                    bool result = true;
                    for (int i = (Density - 1); i > -1; i--)
                    {
                        int playlistIdx = (i + this.currentStartIndexBackwards) % Items.Count();
                        var context = Items[playlistIdx];
                        FrameworkElement itemElement = null;
                        foreach (var child in _itemsLayerGrid.Children)
                        {
                            if (child is FrameworkElement childElement && childElement.DataContext == context)
                            {
                                itemElement = childElement;
                                break;
                            }
                        }
                        if (itemElement != null)
                        {
                            PositionElement(itemElement, i, (float)itemElement.ActualWidth, (float)itemElement.ActualHeight);
                        }
                        else { result = false; }
                    }
                    OnItemsLoaded();
                }
            }
        }

        void PositionElement(FrameworkElement element, int index, float elementWidth, float elementHeight)
        {
            ElementCompositionPreview.SetIsTranslationEnabled(element, true);
            var elementVisual = ElementCompositionPreview.GetElementVisual(element);
            if (elementVisual.ImplicitAnimations != null) { elementVisual.ImplicitAnimations.Clear(); }
            elementVisual.CenterPoint = new Vector3((elementWidth / 2), (elementHeight / 2), 0);
            var translateX = GetTranslateX(index);
            var translateY = GetTranslateY(index);
            elementVisual.Offset = new Vector3(Convert.ToSingle(translateX), Convert.ToSingle(translateY), 0);
            if (CarouselType == CarouselTypes.Wheel)
            {
                elementVisual.RotationAngleInDegrees = GetRotation(index);
            }
            AddStandardImplicitItemAnimation(elementVisual);
        }

        void UpdateItemInCarouselSlot(int carouselIdx, int sourceIdx, bool loFi)
        {
            int idx = Modulus(((Density - 1) - carouselIdx), Density);
            if (_itemsLayerGrid != null && _itemsLayerGrid.Children[idx] is FrameworkElement element)
            {
                element.DataContext = Items[sourceIdx];

                if (CarouselType != CarouselTypes.Wheel)
                {
                    double translateX = 0;
                    double translateY = 0;
                    var elementVisual = ElementCompositionPreview.GetElementVisual(element);
                    UIElement precedingItemElement = loFi ? _itemsLayerGrid.Children[Modulus(idx - 1, Density)] : _itemsLayerGrid.Children[(idx + 1) % Density];
                    var precedingItemElementVisual = ElementCompositionPreview.GetElementVisual(precedingItemElement);

                    if (elementVisual.ImplicitAnimations != null) { elementVisual.ImplicitAnimations.Clear(); }

                    switch (CarouselType)
                    {
                        case CarouselTypes.Row:
                            if (loFi)
                            {
                                translateX = IsCarouselMoving ? precedingItemElementVisual.Offset.X - (_maxItemWidth + ItemGap)
                                    : translateX - (((Density / 2) * (_maxItemWidth + ItemGap)) + _maxItemWidth + ItemGap);

                            }
                            else
                            {
                                translateX = IsCarouselMoving ? precedingItemElementVisual.Offset.X + _maxItemWidth + ItemGap :
                                    (Density / 2) * (_maxItemWidth + ItemGap);
                            }
                            break;
                        case CarouselTypes.Column:
                            if (loFi)
                            {
                                translateY = IsCarouselMoving ? precedingItemElementVisual.Offset.Y - (_maxItemHeight + ItemGap) :
                                    translateY - (((Density / 2) * (_maxItemHeight + ItemGap)) + _maxItemHeight + ItemGap);
                            }
                            else
                            {
                                translateY = IsCarouselMoving ? precedingItemElementVisual.Offset.Y + _maxItemHeight + ItemGap :
                                    (Density / 2) * (_maxItemHeight + ItemGap);
                            }
                            break;
                    }
                    elementVisual.Offset = new System.Numerics.Vector3((float)translateX, (float)translateY, 0);
                    AddStandardImplicitItemAnimation(elementVisual);

                    if (!IsCarouselMoving)
                    {
                        var precedingItemZIndex = Canvas.GetZIndex(precedingItemElement);
                        Canvas.SetZIndex(element, precedingItemZIndex - 1);
                    }

                }
            }
        }

        /// <summary>
        /// Changes the selction a single step.
        /// </summary>
        /// <param name="reverse"></param>
        public void ChangeSelection(bool reverse)
        {
            if (_inputAllowed)
            {
                _selectedIndexSetInternally = true;
                SelectedIndex = reverse ? Modulus(SelectedIndex - 1, Items.Count()) : (SelectedIndex + 1) % Items.Count();
                ChangeSelection(reverse ? currentStartIndexBackwards : currentStartIndexForwards, reverse);
                if (NavigationSpeed > 1 && ZIndexUpdateWaitsForAnimation)
                {
                    _delayedZIndexUpdateTimer.Interval = TimeSpan.FromMilliseconds(NavigationSpeed / 2);
                    if (_delayedZIndexUpdateTimer.IsEnabled)
                    {
                        UpdateZIndices();
                    }
                    _delayedZIndexUpdateTimer.Start();
                }
                else
                {
                    UpdateZIndices();
                }
                _selectedIndexSetInternally = false;
            }
        }

        void ChangeSelection(int startIdx, bool reverse)
        {
            _carouselInsertPosition = reverse ? Modulus((_carouselInsertPosition - 1), Density) : (_carouselInsertPosition + 1) % Density;
            var carouselIdx = reverse ? _carouselInsertPosition : Modulus((_carouselInsertPosition - 1), Density);
            InsertNewCarouselItem(startIdx, carouselIdx, !reverse, reverse);
        }

        private void InsertNewCarouselItem(int startIdx, int carouselIdx, bool scrollbackwards, bool loFi)
        {
            if (!IsCarouselMoving)
            {
                switch (CarouselType)
                {
                    default:
                        switch (WheelAlignment)
                        {
                            default:
                                RotateWheel(!loFi);
                                break;
                            case WheelAlignments.Left:
                            case WheelAlignments.Bottom:
                                RotateWheel(loFi);
                                break;
                        }
                        UpdateItemInCarouselSlot(carouselIdx, startIdx, loFi);
                        break;
                    case CarouselTypes.Column:
                        UpdateItemInCarouselSlot(carouselIdx, startIdx, loFi);
                        ScrollVerticalColumn(scrollbackwards);
                        break;
                    case CarouselTypes.Row:
                        UpdateItemInCarouselSlot(carouselIdx, startIdx, loFi);
                        ScrollHorizontalRow(scrollbackwards);
                        break;
                }
            }
            else
            {
                UpdateItemInCarouselSlot(carouselIdx, startIdx, loFi);
            }
        }

        public void AnimateToSelectedIndex()
        {
            var count = Items != null && Items.Count >= 0 ? Items.Count() : 0;
            if (count > 0)
            {
                var distance = ModularDistance(_previousSelectedIndex, SelectedIndex, count);
                bool goForward = false;

                if (Common.Mod(_previousSelectedIndex + distance, count) == SelectedIndex)
                {
                    goForward = true;
                }

                var steps = distance > Density ? Density : distance;

                if (goForward)
                {
                    var startIdx = Modulus((SelectedIndex + 1 - steps - (Density / 2)), count);
                    for (int i = 0; i < steps; i++)
                    {
                        ChangeSelection((startIdx + i + (Density - 1)) % Items.Count(), false);
                    }
                }
                else
                {
                    var startIdx = Modulus(SelectedIndex - 1 + steps - (Density / 2), count);
                    for (int i = 0; i < steps; i++)
                    {
                        ChangeSelection(Common.Mod(startIdx - i, count), true);
                    }
                }

                UpdateZIndices();
            }

        }

        /// <summary>
        /// This is used to trigger a storyboard animation for the selected item. 
        /// Add a storyboard to resources of the root element of your ItemTemplate and assign the key "SelectionAnimation".
        /// </summary>
        /// <returns></returns>
        public void AnimateSelection()
        {
            if (this.SelectedItemElement is FrameworkElement selectedItemContent)
            {
                Storyboard sb = null;
                if (selectedItemContent.Resources.ContainsKey("SelectionAnimation"))
                {
                    sb = selectedItemContent.Resources["SelectionAnimation"] as Storyboard;
                }
                else if (selectedItemContent.Parent is FrameworkElement parent && parent.Resources.ContainsKey("SelectionAnimation"))
                {
                    sb = parent.Resources["SelectionAnimation"] as Storyboard;
                }
                if (sb != null)
                {
                    sb.Begin();
                }

            }
        }

        private void RotateWheel(bool clockwise)
        {
            float endAngle = (clockwise) ? degrees : -degrees;
            var newVal = _dynamicGridVisual.RotationAngleInDegrees + endAngle;
            _dynamicGridVisual.RotationAngleInDegrees = newVal;
            CarouselRotationAngle = _dynamicGridVisual.RotationAngleInDegrees;
            _currentWheelTick += endAngle;
        }

        void ScrollVerticalColumn(bool scrollUp)
        {
            long endPosition = (scrollUp) ? -(_maxItemHeight + ItemGap) : (_maxItemHeight + ItemGap);
            for (int i = (Density - 1); i > -1; i--)
            {
                int idx = Modulus(((Density - 1) - i), Density);
                if (_itemsLayerGrid != null && _itemsLayerGrid.Children[idx] is FrameworkElement itemElement)
                {
                    var itemElementVisual = ElementCompositionPreview.GetElementVisual(itemElement);
                    var currentX = itemElementVisual.Offset.X;
                    var currentY = itemElementVisual.Offset.Y;
                    itemElementVisual.Offset = new Vector3(currentX, currentY + endPosition, 0);
                }
            }
        }

        void ScrollHorizontalRow(bool scrollLeft)
        {
            long endPosition = (scrollLeft) ? -(_maxItemWidth + ItemGap) : (_maxItemWidth + ItemGap);
            for (int i = (Density - 1); i > -1; i--)
            {
                int idx = Modulus(((Density - 1) - i), Density);
                if (_itemsLayerGrid != null && _itemsLayerGrid.Children[idx] is FrameworkElement itemElement)
                {
                    var itemElementVisual = ElementCompositionPreview.GetElementVisual(itemElement);
                    var currentX = itemElementVisual.Offset.X;
                    var currentY = itemElementVisual.Offset.Y;
                    itemElementVisual.Offset = new System.Numerics.Vector3(currentX + endPosition, currentY, 0);
                }
            }
        }
        void UpdateZIndices()
        {
            if (Items != null && _itemsLayerGrid != null)
            {
                for (int i = -(Density / 2); i < (Density / 2); i++)
                {
                    var slot = Modulus(((Density - 1) - (displaySelectedIndex + i)), Density);
                    Canvas.SetZIndex((UIElement)_itemsLayerGrid.Children[slot], 10000 - Math.Abs(i));
                    if (i == 0)
                    {
                        SelectedItemElement = _itemsLayerGrid.Children[slot] as FrameworkElement;
                    }
                }
            }
        }

        #endregion

        #region VALUE CONVERTERS

        private double GetTranslateY(int i)
        {
            switch (CarouselType)
            {
                default:
                    switch (WheelAlignment)
                    {
                        default:
                            return -(Math.Sin(DegreesToRadians(degrees * i))) * (WheelSize / 2);
                        case WheelAlignments.Left:
                            return (Math.Sin(DegreesToRadians(360 - (degrees * i)))) * (WheelSize / 2);
                        case WheelAlignments.Top:
                            return (Math.Sin(DegreesToRadians(360 - (degrees * ((i + (Density / 4)) % Density))))) * (WheelSize / 2);
                        case WheelAlignments.Bottom:
                            return -(Math.Sin(DegreesToRadians(360 - (degrees * ((i + (Density / 4)) % Density))))) * (WheelSize / 2);
                    }
                case CarouselTypes.Column:
                    return ((_maxItemHeight + ItemGap) * (i - (Density / 2)));
                case CarouselTypes.Row:
                    return 0;
            }
        }
        private double GetTranslateX(int i)
        {
            switch (CarouselType)
            {
                default:
                    switch (WheelAlignment)
                    {
                        default:
                            return (Math.Cos(DegreesToRadians(degrees * i))) * (WheelSize / 2);
                        case WheelAlignments.Left:
                            return -(Math.Cos(DegreesToRadians(360 - (degrees * i)))) * (WheelSize / 2);
                        case WheelAlignments.Top:
                            return (Math.Cos(DegreesToRadians(360 - (degrees * ((i + (Density / 4)) % Density))))) * (WheelSize / 2);
                        case WheelAlignments.Bottom:
                            return (Math.Cos(DegreesToRadians(360 - (degrees * ((i + (Density / 4)) % Density))))) * (WheelSize / 2);
                    }
                case CarouselTypes.Row:
                    return ((_maxItemWidth + ItemGap) * (i - (Density / 2)));
                case CarouselTypes.Column:
                    return 0;
            }
        }

        private int GetRotation(int i)
        {
            if (CarouselType == CarouselTypes.Wheel)
            {
                switch (WheelAlignment)
                {
                    default:
                        return Convert.ToInt32(((360 - (degrees * i)) + 180) % 360);
                    case WheelAlignments.Left:
                    case WheelAlignments.Bottom:
                        return Convert.ToInt32(((degrees * i) + 180) % 360);
                }
            }
            else
            {
                return 0;
            }
        }

        #endregion

        #region EVENT HANDLERS

        public event EventHandler ItemsCreated;

        void OnItemsCreated()
        {
            AreItemsLoaded = true;
            EventHandler handler = ItemsCreated;
            if (handler != null)
                handler(this, null);
        }

        /// <summary>
        /// Raises when the carousel items are loaded into the visual tree.
        /// </summary>
        public event EventHandler ItemsLoaded;

        void OnItemsLoaded()
        {
            AreItemsLoaded = true;
            EventHandler handler = ItemsLoaded;
            if (handler != null)
                handler(this, null);
        }

        /// <summary>
        /// Raises whenever a hitbox manipulation gesture starts or completes.
        /// </summary>
        public event EventHandler CarouselMovingStateChanged;
        void OnCarouselMovingStateChanged()
        {
            EventHandler handler = CarouselMovingStateChanged;
            if (handler != null)
                handler(this, null);
        }

        /// <summary>
        /// Raises when the selection changes.
        /// </summary>
        public event EventHandler SelectionChanged;

        void OnSelectionChanged()
        {
            EventHandler handler = SelectionChanged;
            if (handler != null)
                handler(this, null);
        }

        private static void OnSelectedIndexChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == SelectedIndexProperty)
            {
                var control = dependencyObject as Carousel;
                if (e.OldValue is int oldVal)
                {
                    control._previousSelectedIndex = oldVal;
                }
                if (e.NewValue is int newVal)
                {
                    if (!control._selectedIndexSetInternally)
                    {
                        control.AnimateToSelectedIndex();
                    }
                    if (control.Items != null && newVal >= 0 && newVal < control.Items.Count)
                    {
                        control.SelectedItem = control.Items[newVal];
                    }
                    control.OnSelectionChanged();
                }
            }
        }

        private static void OnCaptionPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var control = dependencyObject as Carousel;
            control.Refresh();
        }

        #endregion
    }

}
