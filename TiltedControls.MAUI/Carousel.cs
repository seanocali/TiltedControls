
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using static TiltedControls.Common;

namespace TiltedControls
{
    /// MAUI Cheat Sheet
    /// BindableObject = DependencyProperty
    /// Element = UIElement
    /// FrameworkElement = VisualElement
    /// Canvas = VisualElement


    /// <summary>
    /// UI control to visually present a collection of data.
    /// </summary>
    /// <remarks>
    /// Visual tree contains an empty ContentControl for tab indexing and keyboard focus.
    /// </remarks>
    public class Carousel : ContentView
    {
        #region CONSTRUCTOR & INITIALIZATION METHODS

        /// <summary>
        /// The class constructor.
        /// </summary>
        public Carousel()
        {
            _root = new Grid();
            this.Content = _root;
            _delayedRefreshTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
            _delayedZIndexUpdateTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));
            _root.Background = new SolidColorBrush(Colors.Transparent);
        }

        void Refresh()
        {
            if (this.BindingContext != null && (IsLoaded || (!Double.IsNaN(Width) && !Double.IsNaN(Height))))
            {
                _inputAllowed = false;
                AreItemsLoaded = false;
                var t = StartDelayedRefreshTimer();
            }
        }

        void LoadNewCarousel()
        {
            _uIItemsCreated = false;
            _width = Double.IsNaN(Width) ? DesiredSize.Width : Width;
            _height = Double.IsNaN(Height) ? DesiredSize.Height : Height;
            _cancelTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancelTokenSource.Token;
            CreateContainers();
            _elementsToLoadCount = Density;
            var items = new VisualElement[Density];
            if (Items != null && Items.Count() > 0)
            {
                try
                {
                    // Fill the UIElementCollection with empty elements so we can use insert later
                    _itemsLayerGrid.Children.Clear();

                    for (int i = 0; i < Density; i++)
                    {
                        _itemsLayerGrid.Children.Add(new ContentView());
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
                    OnItemsCreated();
                }
                catch (Exception ex)
                {
                    _cancelTokenSource.Cancel();
                    Trace.WriteLine(ex.Message);
                }
            }
            _inputAllowed = true;
        }

        //protected virtual void InitializeAnimations(VisualElement[] items)
        //{
        //    for (int i = 0; i < Density / 2; i++)
        //    {
        //        var idx1 = (i + (Density / 2)) % Density;
        //        var idx2 = Density - 1 - idx1;

        //        StartExpressionItemAnimations(items[idx1], idx1);
        //        StartExpressionItemAnimations(items[idx2], idx2);
        //    }
        //}

        VisualElement AddCarouselItemToUI(int idx)
        {
            int playlistIdx = (idx + this.currentStartIndexBackwards) % Items.Count();
            var itemElement = CreateCarouselItemElement(idx, playlistIdx);
            if (itemElement != null)
            {
                itemElement.BindingContextChanged += ItemElement_BindingContextChanged;
                _itemsLayerGrid.Children[Density - 1 - idx] = itemElement;
            }
            else
            {
                throw new Exception("Failed to create Carousel Item");
            }
            return itemElement;
        }

        private void ItemElement_BindingContextChanged(object sender, EventArgs e)
        {
            if (sender is VisualElement element && element.BindingContext != null)
            {
                element.BindingContextChanged -= ItemElement_BindingContextChanged;
                _elementsToLoadCount--;
                if (_elementsToLoadCount < 1)
                {
                    OnItemsLoaded();
                }
            }
        }


        void CreateContainers()
        {
            _root.Children.Clear();
            _currentRowXPosTick = 0;
            _currentWheelTick = 0;
            _carouselInsertPosition = 0;

            _dynamicContainerGrid = new Grid { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            _dynamicContainerGrid.WidthRequest = _width;
            _dynamicContainerGrid.HeightRequest = _height;
            _itemsLayerGrid = new Grid { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
            _dynamicContainerGrid.Children.Add(_itemsLayerGrid);
            _root.Children.Add(_dynamicContainerGrid);
            if (_hitbox != null)
            {
                if (_hitbox.GestureRecognizers != null)
                {
                    foreach (var gesture in _hitbox.GestureRecognizers)
                    {
                        if (gesture is PanGestureRecognizer panGesture)
                        {
                            panGesture.PanUpdated -= PanGesture_PanUpdated;
                        }
                    }
                }
            }
            if (UseGestures)
            {
                _hitbox = new ContentView();
                PanGestureRecognizer panGesture = new PanGestureRecognizer();
                _hitbox.Background = new SolidColorBrush(Colors.Transparent);
                _hitbox.GestureRecognizers.Add(panGesture);
                panGesture.PanUpdated += PanGesture_PanUpdated;
                _hitbox.VerticalOptions = LayoutOptions.Center;
                _hitbox.HorizontalOptions = LayoutOptions.Center;
                _root.Children.Add(_hitbox);
            }

        }

        private void PanGesture_PanUpdated(object sender, PanUpdatedEventArgs e)
        {

            StartManipulationMode();
            if (AreItemsLoaded && ItemsSource != null)
            {
                double value = 0;
                switch (CarouselType)
                {
                    case CarouselTypes.Wheel:
                        switch (WheelAlignment)
                        {
                            //case WheelAlignments.Right:
                            //    value = -(e.Delta.Translation.Y / 4);
                            //    break;
                            //case WheelAlignments.Left:
                            //    value = e.Delta.Translation.Y / 4;
                            //    break;
                            //case WheelAlignments.Top:
                            //    value = -(e.Delta.Translation.X / 4);
                            //    break;
                            //case WheelAlignments.Bottom:
                            //    value = e.Delta.Translation.X / 4;
                            //    break;
                        }
                        CarouselRotationAngle += Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Column:
                        value = e.TotalY * 2;
                        CarouselPositionY = Convert.ToSingle(value);
                        break;
                    case CarouselTypes.Row:
                        value = e.TotalX * 2;
                        CarouselPositionX = Convert.ToSingle(value);
                        break;
                }
            }


        }

        void SetHitboxSize()
        {
            if (UseGestures)
            {
                switch (CarouselType)
                {
                    default:
                        _hitbox.WidthRequest = _width;
                        _hitbox.HeightRequest = _height;
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
                        _hitbox.WidthRequest = ws;
                        _hitbox.HeightRequest = ws;
                        break;
                    case CarouselTypes.Column:
                        _hitbox.HeightRequest = _height;
                        _hitbox.WidthRequest = _maxItemWidth * SelectedItemScale;
                        if (WarpIntensity != 0 && WarpCurve != 0 && this.SelectedItemElement != null)
                        {
                            _hitbox.WidthRequest += (int)(Math.Abs((-WarpCurve) + Math.Abs(WarpIntensity)));
                        }
                        break;
                    case CarouselTypes.Row:
                        _hitbox.WidthRequest = _width;
                        _hitbox.HeightRequest = _maxItemHeight * SelectedItemScale;
                        if (WarpIntensity != 0 && WarpCurve != 0 && this.SelectedItemElement != null)
                        {
                            _hitbox.HeightRequest += (int)(Math.Abs((-WarpCurve) + Math.Abs(WarpIntensity)));
                        }
                        break;
                }

            }
        }


        #endregion

        #region FIELDS
        Grid _root;
        volatile bool _inputAllowed;
        double _width;
        double _height;
        PeriodicTimer _delayedRefreshTimer;
        CancellationTokenSource _delayedRefreshTimerCTS;
        PeriodicTimer _delayedZIndexUpdateTimer;
        CancellationTokenSource _delayedZIndexUpdateTimerCTS;
        CancellationTokenSource _cancelTokenSource;
        protected int _maxItemWidth;
        protected int _maxItemHeight;
        int _previousSelectedIndex;
        volatile float _scrollValue = 0;
        volatile float _scrollSnapshot = 0;
        volatile int _carouselInsertPosition;
        Grid _dynamicContainerGrid;
        Grid _itemsLayerGrid;
        ContentView _hitbox;
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
        /// Returns the VisualElement of the SelectedItem.
        /// </returns>
        public VisualElement SelectedItemElement { get; private set; }

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

        //public static readonly BindableProperty HitboxProperty = BindableProperty.Create(nameof(Hitbox), typeof(Canvas), typeof(Carousel),
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

        public static readonly BindableProperty UseGesturesProperty = BindableProperty.Create(nameof(UseGestures), typeof(bool), typeof(Carousel), defaultValue: true,
        propertyChanged: OnCaptionPropertyChanged);


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

        public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(object), typeof(Carousel),
            propertyChanged: OnItemsSourceChanged);

        /// <summary>
        /// Assign a DataTempate here that will be used to present each item in your data collection.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(nameof(ItemTemplate), typeof(DataTemplate), typeof(Carousel),
            propertyChanged: OnCaptionPropertyChanged);


        /// <summary>
        /// Add a style to the style's first matching target type in the ItemTemplate's visual tree
        /// </summary>
        public Style ItemContentStyle
        {
            get { return (Style)GetValue(ItemContentStyleProperty); }
            set { SetValue(ItemContentStyleProperty, value); }
        }

        public static readonly BindableProperty ItemContentStyleProperty = BindableProperty.Create(nameof(ItemContentStyle), typeof(Style), typeof(Carousel),
            propertyChanged: OnCaptionPropertyChanged);



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

        public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(Carousel), defaultValue: 0,
        propertyChanged: OnSelectedIndexChanged);

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

        public static readonly BindableProperty SelectedItemForegroundBrushProperty = BindableProperty.Create(nameof(SelectedItemForegroundBrush), typeof(Brush), typeof(Carousel),
            propertyChanged: OnCaptionPropertyChanged);


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

        public static readonly BindableProperty TriggerSelectionAnimationProperty = BindableProperty.Create(nameof(TriggerSelectionAnimation), typeof(object), typeof(Carousel),
            propertyChanged: OnTriggerSelectionAnimation);

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

        public static readonly BindableProperty NavigationSpeedProperty = BindableProperty.Create(nameof(NavigationSpeed), typeof(int), typeof(Carousel),
        propertyChanged: OnNavigationSpeedChanged);

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

        public static readonly BindableProperty ZIndexUpdateWaitsForAnimationProperty = BindableProperty.Create(nameof(ZIndexUpdateWaitsForAnimation), typeof(bool), typeof(Carousel),
        defaultValue: false);

        /// <summary>
        /// Sets the scale of the selected item to make it more prominent. The Framework's Element scaling does not do vector scaling of text or SVG images, unfortunately, so keep that in mind when using this.
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

        public static readonly BindableProperty SelectedItemScaleProperty = BindableProperty.Create(nameof(SelectedItemScale), typeof(double), typeof(Carousel),
        defaultValue: 1.0, propertyChanged: OnCaptionPropertyChanged);

        /// <summary>
        /// Set the number of additional items surrounding the selected item to also scale, creating a 'falloff' effect.
        /// This value also applies to Fliptych.
        /// </summary>
        public int AdditionalItemsToScale
        {
            get
            {
                return (int)base.GetValue(AdditionalItemsToScaleProperty);
            }
            set
            {
                base.SetValue(AdditionalItemsToScaleProperty, value);
            }
        }

        public static readonly BindableProperty AdditionalItemsToScaleProperty = BindableProperty.Create(nameof(AdditionalItemsToScale), typeof(int), typeof(Carousel),
        defaultValue: 0, propertyChanged: OnCaptionPropertyChanged);

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

        public static readonly BindableProperty AdditionalItemsToWarpProperty = BindableProperty.Create(nameof(AdditionalItemsToWarp), typeof(int), typeof(Carousel),
          defaultValue: 4, propertyChanged: OnCaptionPropertyChanged);

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
        public static readonly BindableProperty CarouselTypeProperty = BindableProperty.Create(nameof(CarouselType), typeof(CarouselTypes), typeof(Carousel),
        defaultValue: CarouselTypes.Row, propertyChanged: OnCaptionPropertyChanged);

        /// <summary>
        /// When CarouselType is set to Wheel, use this property to set the conainer edge the wheel should be aligned to.
        /// </summary>
        public WheelAlignments WheelAlignment
        {
            get
            {
                return (WheelAlignments)base.GetValue(WheelAlignmentProperty);
            }
            set
            {
                base.SetValue(WheelAlignmentProperty, value);
            }
        }

        public static readonly BindableProperty WheelAlignmentProperty = BindableProperty.Create(nameof(WheelAlignment), typeof(WheelAlignments), typeof(Carousel),
        defaultValue: WheelAlignments.Right, propertyChanged: OnCaptionPropertyChanged);

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

        public static readonly BindableProperty DensityProperty = BindableProperty.Create(nameof(Density), typeof(int), typeof(Carousel),
        defaultValue: 36, propertyChanged: OnCaptionPropertyChanged);

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

        public static readonly BindableProperty FliptychDegreesProperty = BindableProperty.Create(nameof(FliptychDegrees), typeof(double), typeof(Carousel),
        defaultValue: 0.0, propertyChanged: OnCaptionPropertyChanged);

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
        public static readonly BindableProperty WarpIntensityProperty = BindableProperty.Create(nameof(WarpIntensity), typeof(int), typeof(Carousel),
        defaultValue: 0, propertyChanged: OnCaptionPropertyChanged);

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
        public static readonly BindableProperty WarpCurveProperty = BindableProperty.Create(nameof(WarpCurve), typeof(double), typeof(Carousel),
        defaultValue: .002, propertyChanged: OnCaptionPropertyChanged);

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

        public static readonly BindableProperty ItemGapProperty = BindableProperty.Create(nameof(ItemGap), typeof(int), typeof(Carousel),
        defaultValue: 0, propertyChanged: OnCaptionPropertyChanged);

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

        public static readonly BindableProperty SelectNextTriggerProperty = BindableProperty.Create(nameof(SelectNextTrigger), typeof(object), typeof(Carousel),
        propertyChanged: OnSelectNextTrigger);

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

        public static readonly BindableProperty SelectPreviousTriggerProperty = BindableProperty.Create(nameof(SelectPreviousTrigger), typeof(object), typeof(Carousel),
        propertyChanged: OnSelectPreviousTrigger);

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

        public static readonly BindableProperty ManipulationStartedTriggerProperty = BindableProperty.Create(nameof(ManipulationStartedTrigger), typeof(object), typeof(Carousel),
        propertyChanged: OnPanStartedTrigger);

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

        public static readonly BindableProperty ManipulationCompletedTriggerProperty = BindableProperty.Create(nameof(ManipulationCompletedTrigger), typeof(object), typeof(Carousel),
        propertyChanged: OnPanStoppedTrigger);

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

        public static readonly BindableProperty CarouselRotationAngleProperty = BindableProperty.Create(nameof(CarouselRotationAngle), typeof(float), typeof(Carousel),
            propertyChanged: OnCarouselRotationAngleChanged);

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

        public static readonly BindableProperty CarouselPositionYProperty = BindableProperty.Create(nameof(CarouselPositionY), typeof(double), typeof(Carousel),
            propertyChanged: OnCarouselPositionYChanged);

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

        public static readonly BindableProperty CarouselPositionXProperty = BindableProperty.Create(nameof(CarouselPositionX), typeof(double), typeof(Carousel),
            propertyChanged: OnCarouselPositionXChanged);

        #endregion

        #region TIMER METHODS


        async Task StartDelayedRefreshTimer(bool runOnce = true)
        {
            StopTimer(ref _delayedRefreshTimerCTS);
            _delayedRefreshTimerCTS = new CancellationTokenSource();
            while (_delayedRefreshTimerCTS != null && !_delayedRefreshTimerCTS.IsCancellationRequested)
            {
                await _delayedRefreshTimer.WaitForNextTickAsync(_delayedRefreshTimerCTS.Token);
                if (!_delayedRefreshTimerCTS.IsCancellationRequested)
                {
                    LoadNewCarousel();
                    if (runOnce)
                    {
                        StopTimer(ref _delayedRefreshTimerCTS);
                    }
                }
            }
        }

        async Task StartDelayedZindexUpdateTimer(bool runOnce = true)
        {
            StopTimer(ref _delayedZIndexUpdateTimerCTS);
            _delayedZIndexUpdateTimerCTS = new CancellationTokenSource();
            while (_delayedZIndexUpdateTimerCTS != null && !_delayedZIndexUpdateTimerCTS.IsCancellationRequested)
            {
                await _delayedRefreshTimer.WaitForNextTickAsync(_delayedZIndexUpdateTimerCTS.Token);
                if (!_delayedZIndexUpdateTimerCTS.IsCancellationRequested)
                {
                    UpdateZIndices();
                    if (runOnce)
                    {
                        StopTimer(ref _delayedZIndexUpdateTimerCTS);
                    }
                }
            }
        }



        void StopTimer(ref CancellationTokenSource cts)
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }


        #endregion

        #region ANIMATION METHODS
        //private void AddImplicitWheelSnapToAnimation(Visual visual)
        //{
        //    if (NavigationSpeed != 0)
        //    {
        //        ImplicitAnimationCollection implicitAnimations = visual.Compositor.CreateImplicitAnimationCollection();
        //        visual.ImplicitAnimations = implicitAnimations;
        //        int duration = (NavigationSpeed / 2 < 500) ? NavigationSpeed / 2 : 500;
        //        var animationRotate = visual.Compositor.CreateScalarKeyFrameAnimation();
        //        var easing = animationRotate.Compositor.CreateLinearEasingFunction();
        //        animationRotate.InsertExpressionKeyFrame(1f, "this.FinalValue", easing);
        //        animationRotate.Target = "RotationAngleInDegrees";
        //        animationRotate.Duration = TimeSpan.FromMilliseconds(duration);
        //        implicitAnimations["RotationAngleInDegrees"] = animationRotate;
        //        visual.ImplicitAnimations = implicitAnimations;
        //    }
        //}

        //private void ClearImplicitOffsetAnimations(float xDiff, float yDiff, bool clearAll = false)
        //{
        //    for (int i = (Density - 1); i > -1; i--)
        //    {
        //        int idx = Modulus(((Density - 1) - i), Density);
        //        if (_itemsLayerGrid.Children[idx] is VisualElement itemElement)
        //        {
        //            var itemElementVisual = ElementCompositionPreview.GetElementVisual(itemElement);
        //            if (itemElementVisual.ImplicitAnimations != null)
        //            {
        //                if (clearAll)
        //                {
        //                    itemElementVisual.ImplicitAnimations.Clear();
        //                }
        //                else
        //                {
        //                    itemElementVisual.ImplicitAnimations.Remove("Offset");
        //                }
        //            }
        //            itemElementVisual.Offset = new Vector3(itemElementVisual.Offset.X + xDiff, itemElementVisual.Offset.Y + yDiff, itemElementVisual.Offset.Z);
        //        }
        //    }
        //}

        //protected void StartExpressionItemAnimations(VisualElement element, int? slotNum)
        //{
        //    if (element == null) { return; }
        //    var visual = ElementCompositionPreview.GetElementVisual(element);
        //    var compositor = visual.Compositor;
        //    ScalarNode distanceAsPercentOfSelectionAreaThreshold = null;
        //    ScalarNode scaleThresholdDistanceRaw = null;
        //    BooleanNode distanceIsNegativeValue = null;
        //    BooleanNode isWithinScaleThreshold = null;
        //    Vector3Node offset = null;
        //    float selectionAreaThreshold = 0;

        //    if (CarouselType == CarouselTypes.Wheel && slotNum != null)
        //    {
        //        selectionAreaThreshold = itemsToScale == 0 ? degrees : degrees * itemsToScale;

        //        var slotDegrees = ((slotNum + (Density / 2)) % Density) * degrees;

        //        float wheelDegreesWhenItemIsSelected = Convert.ToSingle(slotDegrees);

        //        using (ScalarNode wheelAngle = _dynamicGridVisual.GetReference().RotationAngleInDegrees)
        //        {
        //            switch (WheelAlignment)
        //            {
        //                case WheelAlignments.Right:
        //                    scaleThresholdDistanceRaw = EF.Mod((wheelAngle - wheelDegreesWhenItemIsSelected), 360);
        //                    break;
        //                case WheelAlignments.Top:
        //                    scaleThresholdDistanceRaw = EF.Mod((wheelAngle - wheelDegreesWhenItemIsSelected), 360);
        //                    break;
        //                case WheelAlignments.Left:
        //                    scaleThresholdDistanceRaw = EF.Mod((wheelAngle + wheelDegreesWhenItemIsSelected), 360);
        //                    break;
        //                case WheelAlignments.Bottom:
        //                    scaleThresholdDistanceRaw = EF.Mod((wheelAngle + wheelDegreesWhenItemIsSelected), 360);
        //                    break;
        //            }

        //            using (ScalarNode distanceToZero = EF.Abs(scaleThresholdDistanceRaw))
        //            using (ScalarNode distanceTo360 = 360 - distanceToZero)
        //            using (BooleanNode isClosestToZero = distanceToZero <= distanceTo360)
        //            using (ScalarNode distanceInDegrees = EF.Conditional(isClosestToZero, distanceToZero, distanceTo360))
        //            {
        //                distanceAsPercentOfSelectionAreaThreshold = distanceInDegrees / selectionAreaThreshold;
        //                switch (WheelAlignment)
        //                {
        //                    case WheelAlignments.Top:
        //                    case WheelAlignments.Bottom:
        //                        distanceIsNegativeValue = EF.Abs(scaleThresholdDistanceRaw) < 180;
        //                        break;
        //                    case WheelAlignments.Left:
        //                        distanceIsNegativeValue = EF.Abs(EF.Mod((wheelAngle + wheelDegreesWhenItemIsSelected - 90), 360)) < 180;
        //                        break;
        //                    case WheelAlignments.Right:
        //                        distanceIsNegativeValue = EF.Abs(EF.Mod((wheelAngle - wheelDegreesWhenItemIsSelected + 90), 360)) < 180;
        //                        break;
        //                }

        //                isWithinScaleThreshold = distanceInDegrees < selectionAreaThreshold;
        //                StartScaleAnimation(element, distanceAsPercentOfSelectionAreaThreshold, isWithinScaleThreshold);
        //            }
        //        }
        //    }
        //    else if (CarouselType == CarouselTypes.Row || CarouselType == CarouselTypes.Column)
        //    {
        //        selectionAreaThreshold = isXaxisNavigation ? AdditionalItemsToScale * (_maxItemWidth + ItemGap) : AdditionalItemsToScale * (_maxItemHeight + ItemGap);
        //        if (selectionAreaThreshold == 0)
        //        {
        //            selectionAreaThreshold = isXaxisNavigation ? _maxItemWidth + ItemGap : _maxItemHeight + ItemGap;
        //        }

        //            offset = visual.GetReference().Offset;
        //            scaleThresholdDistanceRaw = this.isXaxisNavigation ? offset.X / selectionAreaThreshold : offset.Y / selectionAreaThreshold;

        //            distanceAsPercentOfSelectionAreaThreshold = EF.Abs(scaleThresholdDistanceRaw);

        //            distanceIsNegativeValue = scaleThresholdDistanceRaw < 0;
        //            isWithinScaleThreshold = isXaxisNavigation ? offset.X > -selectionAreaThreshold & offset.X < selectionAreaThreshold : offset.Y > -selectionAreaThreshold & offset.Y < selectionAreaThreshold;

        //            StartScaleAnimation(element, distanceAsPercentOfSelectionAreaThreshold, isWithinScaleThreshold);

        //            if (WarpIntensity != 0)
        //            {
        //                var warpItemsthreshold = isXaxisNavigation ? AdditionalItemsToWarp * (_maxItemWidth + ItemGap) : AdditionalItemsToWarp * (_maxItemHeight + ItemGap);
        //                if (warpItemsthreshold == 0)
        //                {
        //                    warpItemsthreshold = isXaxisNavigation ? _maxItemWidth + ItemGap : _maxItemHeight + ItemGap;
        //                }
        //                using (ScalarNode warpThresholdDistanceRaw = this.isXaxisNavigation ? offset.X / warpItemsthreshold : offset.Y / warpItemsthreshold)
        //                using (ScalarNode distanceAsPercentOfWarpThreshold = EF.Abs(warpThresholdDistanceRaw))
        //                using (BooleanNode isWithinWarpThreshold = isXaxisNavigation ? offset.X > -warpItemsthreshold & offset.X < warpItemsthreshold : offset.Y > -warpItemsthreshold & offset.Y < warpItemsthreshold)
        //                using (ScalarNode y = WarpIntensity - (distanceAsPercentOfWarpThreshold * WarpIntensity))
        //                using (ScalarNode WarpOffset = Convert.ToSingle(-WarpCurve) * warpThresholdDistanceRaw * warpThresholdDistanceRaw + WarpIntensity)
        //                using (ScalarNode finalWarpValue = EF.Conditional(isWithinWarpThreshold, y * EF.Abs(y) * (float)WarpCurve, 0))
        //                {
        //                    if (isXaxisNavigation)
        //                    {
        //                        visual.StartAnimation("Translation.Y", finalWarpValue);
        //                    }
        //                    else
        //                    {
        //                        visual.StartAnimation("Translation.X", finalWarpValue);
        //                    }
        //                }
        //            }

        //    }

        //    // Fliptych
        //    if (useFliptych && CarouselType != CarouselTypes.Wheel) // TODO: Implement Fliptych on Wheel
        //    {
        //        Element child = null;

        //        if (element.Transform3D == null)
        //        {
        //            element.Transform3D = new PerspectiveTransform3D { Depth = 1000 };
        //        }
        //        if (element is UserControl uc && uc.Content is Element ucElement)
        //        {
        //            child = ucElement;
        //        }
        //        else if (element is ContentControl cc && cc.Content is Element ccElement)
        //        {
        //            child = ccElement;
        //        }
        //        else if (element is Grid g && g.Children.Count == 1 && g.Children.First() is Element gElement)
        //        {
        //            child = gElement;
        //        }
        //        else if (element is Panel p && p.Children.Count == 1 && p.Children.First() is Element pElement)
        //        {
        //            child = pElement;
        //        }
        //        else if (element is Canvas c && c.Children.Count == 1 && c.Children.First() is Element cElement)
        //        {
        //            child = cElement;
        //        }

        //        if (child != null)
        //        {
        //            var childVisual = ElementCompositionPreview.GetElementVisual(child);
        //            var fliptychDegrees = isXaxisNavigation ? Convert.ToSingle(FliptychDegrees) : Convert.ToSingle(-FliptychDegrees);
        //            if (CarouselType == CarouselTypes.Wheel) { fliptychDegrees *= -1; }
        //            childVisual.RotationAxis = isXaxisNavigation ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
        //            childVisual.CenterPoint = new Vector3(_maxItemWidth / 2, _maxItemHeight / 2, 0);
        //            using (ScalarNode rotatedValue = EF.Conditional(distanceIsNegativeValue, fliptychDegrees, -fliptychDegrees))
        //            using (ScalarNode finalValue = EF.Conditional(isWithinScaleThreshold, distanceAsPercentOfSelectionAreaThreshold * rotatedValue, rotatedValue))
        //            {
        //                childVisual.StartAnimation(nameof(childVisual.RotationAngleInDegrees), finalValue);
        //            }
        //        }
        //    }
        //    SetColorAnimation(distanceAsPercentOfSelectionAreaThreshold, isWithinScaleThreshold, element);

        //    offset?.Dispose();
        //    distanceAsPercentOfSelectionAreaThreshold?.Dispose();
        //    scaleThresholdDistanceRaw?.Dispose();
        //    distanceIsNegativeValue?.Dispose();
        //    isWithinScaleThreshold?.Dispose();
        //}

        ///// <summary>
        ///// Override this to access your items' SpriteVisual so you can animate Size instead of Scale, maintaining quality of 
        ///// vector or higher resolution graphics.
        ///// </summary>
        //protected virtual void StartScaleAnimation(Element element, ScalarNode distanceAsPercentOfScaleThreshold, BooleanNode isWithinScaleThreshold)
        //{
        //    if (SelectedItemScale != 1)
        //    {
        //        var visual = ElementCompositionPreview.GetElementVisual(element);
        //        var scaleRange = (float)SelectedItemScale - 1;
        //        using (ScalarNode scalePercent = scaleRange * (1 - distanceAsPercentOfScaleThreshold) + 1)
        //        using (ScalarNode finalScaleValue = ExpressionFunctions.Conditional(isWithinScaleThreshold, scalePercent, 1))
        //        {
        //            visual.StartAnimation("Scale.X", finalScaleValue);
        //            visual.StartAnimation("Scale.Y", finalScaleValue);
        //        }

        //    }
        //}

        ///// <summary>
        ///// For better performance it may be worth extending Carousel and overriding this method with one that targets the specific class and property you want.
        ///// </summary>
        //protected virtual void SetColorAnimation(ScalarNode distanceAsPercentOfScaleThreshold, BooleanNode isWithinScaleThreshold, VisualElement fe)
        //{
        //    var descendants = fe.FindDescendants();
        //    List<VisualElement> elements = new List<VisualElement>();
        //    foreach (var descendant in descendants)
        //    {
        //        if (descendant is VisualElement cfe)
        //        {
        //            elements.Add(cfe);
        //        }
        //    }
        //    elements.Add(fe);
        //    foreach (var element in elements)
        //    {
        //        var t = element.GetType();
        //        var props = t.GetProperties();
        //        foreach (var prop in props)
        //        {
        //            if (prop.PropertyType == typeof(CompositionColorBrush) || prop.PropertyType == typeof(CompositionLinearGradientBrush))
        //            {
        //                if (SelectedItemForegroundBrush is SolidColorBrush solidColorBrush)
        //                {
        //                    if (prop.GetValue(element) is CompositionColorBrush compositionSolid)
        //                    {
        //                        using (ColorNode deselectedColor = ExpressionFunctions.ColorRgb(compositionSolid.Color.A,
        //                           compositionSolid.Color.R, compositionSolid.Color.G, compositionSolid.Color.B))
        //                        using (ColorNode selectedColor = ExpressionFunctions.ColorRgb(solidColorBrush.Color.A,
        //                           solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B))
        //                        using (ColorNode colorLerp = ExpressionFunctions.ColorLerp(selectedColor, deselectedColor, distanceAsPercentOfScaleThreshold))
        //                        using (ColorNode finalColorExp = ExpressionFunctions.Conditional(isWithinScaleThreshold, colorLerp, deselectedColor))
        //                        {
        //                            compositionSolid.StartAnimation("Color", finalColorExp);
        //                        }

        //                    }
        //                    else if (prop.GetValue(element) is CompositionLinearGradientBrush compositionGradient)
        //                    {
        //                        for (int i = 0; i < compositionGradient.ColorStops.Count; i++)
        //                        {
        //                            using (CompositionColorGradientStop targetStop = compositionGradient.ColorStops[i])
        //                            using (ColorNode deselectedColor = ExpressionFunctions.ColorRgb(targetStop.Color.A, targetStop.Color.R, targetStop.Color.G, targetStop.Color.B))
        //                            using (ColorNode selectedColor = ExpressionFunctions.ColorRgb(solidColorBrush.Color.A, solidColorBrush.Color.R, solidColorBrush.Color.G, solidColorBrush.Color.B))
        //                            using (ColorNode colorLerp = ExpressionFunctions.ColorLerp(selectedColor, deselectedColor, distanceAsPercentOfScaleThreshold))
        //                            using (ColorNode finalColorExp = ExpressionFunctions.Conditional(isWithinScaleThreshold, colorLerp, deselectedColor))
        //                            {
        //                                targetStop.StartAnimation("Color", finalColorExp);
        //                            }
        //                        }
        //                    }
        //                }
        //                else if (SelectedItemForegroundBrush is LinearGradientBrush linearGradientBrush && prop.GetValue(element) is CompositionLinearGradientBrush compositionGradient)
        //                {
        //                    if (linearGradientBrush.GradientStops.Count == compositionGradient.ColorStops.Count)
        //                    {
        //                        for (int i = 0; i < compositionGradient.ColorStops.Count; i++)
        //                        {
        //                            using (CompositionColorGradientStop targetStop = compositionGradient.ColorStops[i])
        //                            using (ColorNode deselectedColor = ExpressionFunctions.ColorRgb(targetStop.Color.A, targetStop.Color.R, targetStop.Color.G, targetStop.Color.B))
        //                            {
        //                                GradientStop sourceStop = linearGradientBrush.GradientStops[i];
        //                                using (ColorNode selectedColor = ExpressionFunctions.ColorRgb(sourceStop.Color.A, sourceStop.Color.R, sourceStop.Color.G, sourceStop.Color.B))
        //                                using (ColorNode colorLerp = ExpressionFunctions.ColorLerp(selectedColor, deselectedColor, distanceAsPercentOfScaleThreshold))
        //                                using (ColorNode finalColorExp = ExpressionFunctions.Conditional(isWithinScaleThreshold, colorLerp, deselectedColor))
        //                                {
        //                                    //CommunityToolkit.WinUI.UI.Animations.Expressions.CompositionExtensions.StartAnimation(targetStop, "Color", finalColorExp);
        //                                    targetStop.StartAnimation("Color", finalColorExp);
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //private void AddStandardImplicitItemAnimation(Visual visual)
        //{
        //    AddStandardImplicitItemAnimation(visual, NavigationSpeed, false);
        //}

        //protected virtual void AddStandardImplicitItemAnimation(Visual visual, int durationMilliseconds, bool rotation)
        //{
        //    if (NavigationSpeed != 0)
        //    {
        //        if (visual.ImplicitAnimations == null)
        //        {
        //            visual.ImplicitAnimations = visual.Compositor.CreateImplicitAnimationCollection();
        //        }

        //        var scaleAnimation = visual.Compositor.CreateVector3KeyFrameAnimation();
        //        var scaleEasing = scaleAnimation.Compositor.CreateLinearEasingFunction();
        //        scaleAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", scaleEasing);
        //        scaleAnimation.Target = nameof(visual.Scale);
        //        scaleAnimation.Duration = TimeSpan.FromMilliseconds(durationMilliseconds);
        //        if (!visual.ImplicitAnimations.ContainsKey(nameof(visual.Scale)))
        //        {
        //            visual.ImplicitAnimations[nameof(visual.Scale)] = scaleAnimation;
        //        }

        //        var offsetAnimation = visual.Compositor.CreateVector3KeyFrameAnimation();
        //        var offsetEasing = offsetAnimation.Compositor.CreateLinearEasingFunction();
        //        offsetAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", offsetEasing);
        //        offsetAnimation.Target = nameof(visual.Offset);
        //        offsetAnimation.Duration = TimeSpan.FromMilliseconds(durationMilliseconds);
        //        if (!visual.ImplicitAnimations.ContainsKey(nameof(visual.Offset)))
        //        {
        //            visual.ImplicitAnimations[nameof(visual.Offset)] = offsetAnimation;
        //        }

        //        var opacityAnimation = visual.Compositor.CreateScalarKeyFrameAnimation();
        //        var opacityEasing = opacityAnimation.Compositor.CreateLinearEasingFunction();
        //        opacityAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", opacityEasing);
        //        opacityAnimation.Target = nameof(visual.Opacity);
        //        opacityAnimation.Duration = TimeSpan.FromMilliseconds(durationMilliseconds);
        //        if (!visual.ImplicitAnimations.ContainsKey(nameof(visual.Opacity)))
        //        {
        //            visual.ImplicitAnimations[nameof(visual.Opacity)] = opacityAnimation;
        //        }

        //        if (rotation)
        //        {
        //            var rotateAnimation = visual.Compositor.CreateScalarKeyFrameAnimation();
        //            var rotationEasing = rotateAnimation.Compositor.CreateLinearEasingFunction();
        //            rotateAnimation.InsertExpressionKeyFrame(1f, "this.FinalValue", rotationEasing);
        //            rotateAnimation.Target = nameof(visual.RotationAngleInDegrees);
        //            rotateAnimation.Duration = TimeSpan.FromMilliseconds(durationMilliseconds);
        //            if (!visual.ImplicitAnimations.ContainsKey(nameof(visual.RotationAngleInDegrees)))
        //            {
        //                visual.ImplicitAnimations[nameof(visual.RotationAngleInDegrees)] = rotateAnimation;
        //            }
        //        }
        //    }
        //}

        //private void AddImplicitWheelRotationAnimation(Visual visual)
        //{
        //    if (NavigationSpeed > 0)
        //    {
        //        ImplicitAnimationCollection implicitAnimations = visual.Compositor.CreateImplicitAnimationCollection();
        //        var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
        //        animation.InsertExpressionKeyFrame(1f, "this.FinalValue");
        //        animation.Target = "RotationAngleInDegrees";
        //        animation.Duration = TimeSpan.FromMilliseconds(NavigationSpeed);
        //        implicitAnimations["RotationAngleInDegrees"] = animation;
        //        visual.ImplicitAnimations = implicitAnimations;
        //    }
        //}

        //private void RemoveImplicitWheelRotationAnimation(Visual visual)
        //{
        //    if (visual.ImplicitAnimations != null)
        //    {
        //        visual.ImplicitAnimations.Clear();
        //    }
        //}

        #endregion

        #region NAVIGATION METHODS

        /// <summary>
        /// This must be called before updating the carousel with a ManipulationData event. MMVM implementations can use the trigger property to call it.
        /// </summary>
        public void StartManipulationMode()
        {
            _manipulationStarted = true;
            IsCarouselMoving = true;
            //RemoveImplicitWheelRotationAnimation(null);
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

                _dynamicContainerGrid.Rotation = newValue;
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
            }
        }

        async Task StopCarouselMoving()
        {
            var selectedIdx = Modulus(((Density - 1) - (displaySelectedIndex)), Density);
            if (CarouselType == CarouselTypes.Wheel)
            {
                _dynamicContainerGrid.Rotation = _currentWheelTick;
                _currentWheelTick = _currentWheelTick % 360;
                _dynamicContainerGrid.Rotation = _currentWheelTick;
                CarouselRotationAngle = _currentWheelTick;
            }

            var offsetVertical = _maxItemHeight + ItemGap;
            var offsetHorizontal = _maxItemWidth + ItemGap;

            for (int i = -((Density / 2) - 1); i <= (Density / 2); i++)
            {
                int j = Modulus((selectedIdx + i), Density);
                if (_itemsLayerGrid != null && _itemsLayerGrid.Children[j] is VisualElement itemElement)
                {
                    if (CarouselType == CarouselTypes.Column)
                    {
                        itemElement.TranslationY = offsetVertical * -i;
                        itemElement.ZIndex = (Density - Math.Abs(i));
                    }
                    else if (CarouselType == CarouselTypes.Row)
                    {
                        itemElement.TranslationX = offsetHorizontal * -i;
                        itemElement.ZIndex = Density - Math.Abs(i);
                    }
                }
            }
            CarouselPositionY = 0;
            _currentColumnYPosTick = 0;
            CarouselPositionX = 0;
            _currentRowXPosTick = 0;
            _scrollValue = 0;
            _scrollSnapshot = 0;
        }

        VisualElement CreateCarouselItemElement(int i, int playlistIdx)
        {
            if (ItemTemplate != null)
            {
                int w = 0;
                int h = 0;
                var element = ItemTemplate.LoadTemplate() as ContentView;
                if (ItemContentStyle != null)
                {
                    var descendants = element.FindDescendants<Element>();
                    var targetTypeChild = descendants.Where(x => x.GetType() == ItemContentStyle.TargetType).FirstOrDefault() as View;
                    if (targetTypeChild != null)
                    {
                        Convert.ChangeType(targetTypeChild, ItemContentStyle.TargetType);
                        targetTypeChild.Style = ItemContentStyle;
                        if (Double.IsNaN(targetTypeChild.WidthRequest) || Double.IsNaN(targetTypeChild.HeightRequest))
                        {
                            w = Convert.ToInt32(targetTypeChild.DesiredSize.Width);
                            h = Convert.ToInt32(targetTypeChild.DesiredSize.Height);
                        }
                        else
                        {
                            w = Convert.ToInt32(targetTypeChild.WidthRequest + targetTypeChild.Margin.Left + targetTypeChild.Margin.Right);
                            h = Convert.ToInt32(targetTypeChild.HeightRequest + targetTypeChild.Margin.Top + targetTypeChild.Margin.Bottom);
                        }
                    }
                }
                element.BindingContext = Items[playlistIdx];
                if (Double.IsNaN(element.HeightRequest) || Double.IsNaN(element.WidthRequest))
                {
                    if (w == 0)
                    {
                        w = Convert.ToInt32(element.DesiredSize.Width + element.Margin.Left + element.Margin.Right);
                    }
                    if (h == 0)
                    {
                        h = Convert.ToInt32(element.DesiredSize.Height + element.Margin.Top + element.Margin.Bottom);
                    }

                }
                else
                {
                    w = Convert.ToInt32(element.WidthRequest + element.Margin.Left + element.Margin.Right);
                    h = Convert.ToInt32(element.HeightRequest + element.Margin.Top + element.Margin.Bottom);
                }

                if (w == 0 || h == 0)
                {
                    Trace.WriteLine("Tilted Carousel: Item Height and Width (or MaxHeight and MaxWidth) not set.");
                }

                if (_maxItemWidth < element.Width) { _maxItemWidth = w; }
                if (_maxItemHeight < element.Height) { _maxItemHeight = h; }
                PositionElement(element, i, (float)w, (float)h);
                return element;
            }
            return null;
        }

        //private void Element_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    if (e.NewSize.Width > 0 && e.NewSize.Height > 0 && sender is VisualElement element)
        //    {
        //        element.SizeChanged -= Element_SizeChanged;
        //        if (_maxItemHeight < element.ActualHeight) { _maxItemHeight = Convert.ToInt32(element.ActualHeight); }
        //        if (_maxItemWidth < element.ActualWidth) { _maxItemWidth = Convert.ToInt32(element.ActualWidth); }
        //        if (_uIItemsCreated)
        //        {
        //            for (int i = (Density - 1); i > -1; i--)
        //            {
        //                int playlistIdx = (i + this.currentStartIndexBackwards) % Items.Count();
        //                var context = Items[playlistIdx];
        //                VisualElement itemElement = null;
        //                foreach (var child in _itemsLayerGrid.Children)
        //                {
        //                    if (child is VisualElement childElement && childElement.DataContext == context)
        //                    {
        //                        itemElement = childElement;
        //                        break;
        //                    }
        //                }
        //                if (itemElement != null)
        //                {
        //                    PositionElement(itemElement, i, (float)itemElement.ActualWidth, (float)itemElement.ActualHeight);
        //                }
        //            }
        //            OnItemsLoaded();
        //        }
        //    }
        //}

        void PositionElement(VisualElement element, int index, float elementWidth, float elementHeight)
        {
            element.TranslationX = GetOffsetX(index);
            element.TranslationY = GetOffsetY(index);
            
            if (CarouselType == CarouselTypes.Wheel)
            {
                element.Rotation = GetRotation(index);
            }
        }

        void UpdateItemInCarouselSlot(int carouselIdx, int sourceIdx, bool loFi)
        {
            int idx = Modulus(((Density - 1) - carouselIdx), Density);
            if (_itemsLayerGrid != null && _itemsLayerGrid.Children[idx] is VisualElement element)
            {
                element.BindingContext = Items[sourceIdx];

                if (CarouselType != CarouselTypes.Wheel)
                {
                    double translateX = 0;
                    double translateY = 0;
                    IView precedingItemElement = loFi ? _itemsLayerGrid.Children[Modulus(idx - 1, Density)] : _itemsLayerGrid.Children[(idx + 1) % Density];


                    switch (CarouselType)
                    {
                        case CarouselTypes.Row:
                            if (loFi)
                            {
                                translateX = IsCarouselMoving ? precedingItemElement.TranslationX - (_maxItemWidth + ItemGap)
                                    : translateX - (((Density / 2) * (_maxItemWidth + ItemGap)) + _maxItemWidth + ItemGap);

                            }
                            else
                            {
                                translateX = IsCarouselMoving ? precedingItemElement.TranslationX + _maxItemWidth + ItemGap :
                                    (Density / 2) * (_maxItemWidth + ItemGap);
                            }
                            break;
                        case CarouselTypes.Column:
                            if (loFi)
                            {
                                translateY = IsCarouselMoving ? precedingItemElement.TranslationY - (_maxItemHeight + ItemGap) :
                                    translateY - (((Density / 2) * (_maxItemHeight + ItemGap)) + _maxItemHeight + ItemGap);
                            }
                            else
                            {
                                translateY = IsCarouselMoving ? precedingItemElement.TranslationY + _maxItemHeight + ItemGap :
                                    (Density / 2) * (_maxItemHeight + ItemGap);
                            }
                            break;
                    }
                    element.TranslationX = translateX;
                    element.TranslationY = translateY;

                    if (!IsCarouselMoving)
                    {
                        var precedingItemZIndex = precedingItemElement.ZIndex;
                        element.ZIndex = precedingItemZIndex - 1;
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
                    if (_delayedRefreshTimerCTS != null)
                    {
                        StopTimer(ref _delayedZIndexUpdateTimerCTS);
                        _delayedZIndexUpdateTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(NavigationSpeed / 2));
                        UpdateZIndices();
                    }
                    var t = StartDelayedZindexUpdateTimer();
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
            //if (this.SelectedItemElement is VisualElement selectedItemContent)
            //{
            //    Storyboard sb = null;
            //    if (selectedItemContent.Resources.ContainsKey("SelectionAnimation"))
            //    {
            //        sb = selectedItemContent.Resources["SelectionAnimation"] as Storyboard;
            //    }
            //    else if (selectedItemContent.Parent is VisualElement parent && parent.Resources.ContainsKey("SelectionAnimation"))
            //    {
            //        sb = parent.Resources["SelectionAnimation"] as Storyboard;
            //    }
            //    if (sb != null)
            //    {
            //        sb.Completed += SelectionAnimation_Completed;
            //        sb.Begin();
            //    }

            //}
        }

        //private void SelectionAnimation_Completed(object sender, object e)
        //{
        //    if (sender is Storyboard sb)
        //    {
        //        sb.Completed -= SelectionAnimation_Completed;
        //    }
        //    OnSelectionAnimationComplete();
        //}

        private void RotateWheel(bool clockwise)
        {
            float endAngle = (clockwise) ? degrees : -degrees;
            var newVal = _dynamicContainerGrid.Rotation + endAngle;
            _dynamicContainerGrid.Rotation = newVal;
            CarouselRotationAngle = (float)_dynamicContainerGrid.Rotation;
            _currentWheelTick += endAngle;
        }

        void ScrollVerticalColumn(bool scrollUp)
        {
            long endPosition = (scrollUp) ? -(_maxItemHeight + ItemGap) : (_maxItemHeight + ItemGap);
            for (int i = (Density - 1); i > -1; i--)
            {
                int idx = Modulus(((Density - 1) - i), Density);
                if (_itemsLayerGrid != null && _itemsLayerGrid.Children[idx] is VisualElement itemElement)
                {
                    itemElement.TranslationY = endPosition;
                }
            }
        }

        void ScrollHorizontalRow(bool scrollLeft)
        {
            long endPosition = (scrollLeft) ? -(_maxItemWidth + ItemGap) : (_maxItemWidth + ItemGap);
            for (int i = (Density - 1); i > -1; i--)
            {
                int idx = Modulus(((Density - 1) - i), Density);
                if (_itemsLayerGrid != null && _itemsLayerGrid.Children[idx] is VisualElement itemElement)
                {
                    itemElement.TranslationX = endPosition;
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
                    var e = (VisualElement)_itemsLayerGrid.Children[slot];
                    e.ZIndex = 10000 - Math.Abs(i);
                    if (i == 0)
                    {
                        SelectedItemElement = e;
                    }
                }
            }
        }

        #endregion

        #region VALUE CONVERTERS

        protected double GetOffsetY(int i)
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
        protected double GetOffsetX(int i)
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
        /// Raises whenever an item selection storyboard animation completes.
        /// </summary>
        public event EventHandler SelectionAnimationComplete;
        void OnSelectionAnimationComplete()
        {
            EventHandler handler = SelectionAnimationComplete;
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

        private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as Carousel;
            if (oldValue is int oldVal)
            {
                control._previousSelectedIndex = oldVal;
            }
            if (newValue is int newVal)
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

        private static void OnCaptionPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var carousel = bindable as Carousel;
            carousel.Refresh();
        }

        private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as Carousel;
            if (newValue is IEnumerable<object> nv)
            {
                control.Items = nv.ToArray();
            }
            else
            {
                control.Items = null;
            }
            control.Refresh();
        }

        private static void OnTriggerSelectionAnimation(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var control = bindable as Carousel;
                control.AnimateSelection();
            }
        }

        private static void OnNavigationSpeedChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as Carousel;
            if (newValue is int nv && nv > 1)
            {
                control._delayedZIndexUpdateTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(nv / 2));
            }
            control.Refresh();
        }

        private static void OnSelectNextTrigger(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var control = bindable as Carousel;
                control.ChangeSelection(false);
            }
        }

        private static void OnSelectPreviousTrigger(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var control = bindable as Carousel;
                control.ChangeSelection(true);
            }
        }
        private static void OnPanStartedTrigger(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var control = bindable as Carousel;
                control.StartManipulationMode();
            }
        }

        private static void OnPanStoppedTrigger(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var control = bindable as Carousel;
                control.StopManipulationMode();
            }
        }

        private static void OnCarouselRotationAngleChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (newValue != null)
            {
                var control = bindable as Carousel;
                if (control._inputAllowed && control.IsCarouselMoving && newValue is float v)
                {
                    control.UpdateWheelRotation(v);
                }
            }
        }

        private static void OnCarouselPositionYChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as Carousel;
            if (control._inputAllowed && control.IsCarouselMoving && newValue is double v)
            {
                control.UpdateCarouselVerticalScrolling(Convert.ToSingle(v));
            }
        }

        private static void OnCarouselPositionXChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as Carousel;
            if (control._inputAllowed && control.IsCarouselMoving && newValue is double v)
            {
                control.UpdateCarouselHorizontalScrolling(Convert.ToSingle(v));
            }
        }

        #endregion
    }

}
