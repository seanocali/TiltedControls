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
using static Tilted.Common;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace Tilted
{
    public sealed partial class Carousel : Grid
    {
        #region CONSTRUCTOR

        public Carousel()
        {
            this.DataContextChanged += Carousel_DataContextChanged;
            _compositor = Window.Current.Compositor;
            _delayedRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _delayedRefreshTimer.Tick += _delayedRefreshTimer_Tick;
            _restartExpressionsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _restartExpressionsTimer.Tick += _restartExpressionsTimer_Tick;
            this.Background = new SolidColorBrush(Colors.Transparent);

        }


        #endregion

        #region FIELDS
        DispatcherTimer _restartExpressionsTimer;
        DispatcherTimer _delayedRefreshTimer;
        CancellationTokenSource _cancelTokenSource;
        int _itemWidth;
        int _itemHeight;
        int _previousSelectedIndex;
        Compositor _compositor;
        float _scrollValue = 0;
        float _scrollSnapshot = 0;
        int _carouselInsertPosition;
        Visual _dynamicGridVisual;
        Visual _outerGridVisual;
        Grid _dynamicContainerGrid;
        ContentControl _dynamicWheelUnderlayGrid;
        Grid _itemsLayerGrid;
        Canvas _gestureHitbox;
        volatile float _currentWheelTick;
        long _currentColumnYPosTick;
        long _currentRowXPosTick;

        #endregion

        #region PRIVATE PROPERTIES

        bool isXaxisNavigation
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

        bool useFliptych
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
        int startIndex
        {
            get
            {
                return Items != null ? Modulus((SelectedIndex - (Density / 2)), Items.Count()) : 0;
            }
        }

        #endregion

        #region PROPERTIES

        public FrameworkElement SelectedItemContent { get; set; }

        public IList<object> Items { get; set; }

        bool _isCarouselMoving;
        public bool IsCarouselMoving
        {
            get
            {
                return _isCarouselMoving;
            }
            set
            {
                if (_isCarouselMoving != value)
                {
                    if (value)
                    {
                        RemoveImplicitWheelRotationAnimation(_dynamicGridVisual);
                    }
                    else
                    {
                        StopCarouselMoving();
                    }
                    _isCarouselMoving = value;
                    OnCarouselManipulationStateChanged(new CarouselManipulationStateChangedArgs { IsCarouselMoving = value });
                }
            }
        }

        float _carouselRotationAngle;
        public float CarouselRotationAngle
        {
            get { return _carouselRotationAngle; }
            set
            {
                _carouselRotationAngle = value;
                if (_dynamicGridVisual != null)
                {
                    UpdateWheelSpinning(value);
                }
            }
        }

        double _carouselPositionY;
        public double CarouselPositionY
        {
            get { return _carouselPositionY; }
            set
            {
                _carouselPositionY = value;
                if (_dynamicGridVisual != null)
                {
                    UpdateCarouselVerticalScrolling(Convert.ToSingle(value));
                }
            }
        }

        double _carouselPositionX;
        public double CarouselPositionX
        {
            get { return _carouselPositionX; }
            set
            {
                _carouselPositionX = value;
                if (_dynamicGridVisual != null)
                {
                    UpdateCarouselHorizontalScrolling(Convert.ToSingle(value));
                }
            }
        }

        public int WheelSize
        {
            get
            {
                var maxDimension = (Height > Width) ? Height : Width;
                return Convert.ToInt32(WheelScale * maxDimension);
            }
        }

        #endregion

        #region DEPENDENCY PROPERTIES

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

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(nameof(SelectedIndex), typeof(object), typeof(Carousel),
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

        public DataTemplate ItemTemplate
        {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty = DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(Carousel), new PropertyMetadata(null));


        volatile bool _selectedIndexSetInternally;
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
            new PropertyMetadata(0, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
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
                    else
                    {
                        control._selectedIndexSetInternally = false;
                    }
                    if (control.Items != null)
                    {
                        control.SelectedItem = control.Items[newVal];
                    }
                }
            })));

        public object SelectedItem
        {
            get
            {
                return (object)base.GetValue(SelectedItemProperty);
            }
            set
            {
                base.SetValue(SelectedItemProperty, value);
            }
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(Carousel),
            new PropertyMetadata(null));


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
            new PropertyMetadata(null, new PropertyChangedCallback(async (s, e) =>
            {
                if (e.NewValue != null)
                {
                    var control = s as Carousel;
                    await control.AnimateSelection();
                }
            })));

        public bool EnableGestures
        {
            get
            {
                return (bool)base.GetValue(EnableGesturesProperty);
            }
            set
            {
                if (value != EnableGestures)
                {
                    base.SetValue(EnableGesturesProperty, value);
                    SetSizeAndGestureEvents();
                }
                else
                {
                    base.SetValue(EnableGesturesProperty, value);
                }
            }
        }

        public static readonly DependencyProperty EnableGesturesProperty = DependencyProperty.Register(nameof(EnableGestures), typeof(bool), typeof(Carousel),
            new PropertyMetadata(true, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                control.Refresh();
            })));


        public bool EnableMouseWheel
        {
            get
            {
                return (bool)base.GetValue(EnableMouseWheelProperty);
            }
            set
            {
                if (value != EnableMouseWheel)
                {
                    base.SetValue(EnableMouseWheelProperty, value);
                    SetSizeAndGestureEvents();
                }
                else
                {
                    base.SetValue(EnableMouseWheelProperty, value);
                }
            }
        }

        public static readonly DependencyProperty EnableMouseWheelProperty = DependencyProperty.Register(nameof(EnableMouseWheel), typeof(bool), typeof(Carousel),
            new PropertyMetadata(true, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                control.Refresh();
            })));


        public bool EnableCursorChange
        {
            get
            {
                return (bool)base.GetValue(EnableCursorChangeProperty);
            }
            set
            {
                if (value != EnableCursorChange)
                {
                    base.SetValue(EnableCursorChangeProperty, value);
                    SetSizeAndGestureEvents();
                }
                else
                {
                    base.SetValue(EnableCursorChangeProperty, value);
                }
            }
        }

        public static readonly DependencyProperty EnableCursorChangeProperty = DependencyProperty.Register(nameof(EnableCursorChange), typeof(bool), typeof(Carousel),
            new PropertyMetadata(true, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                control.Refresh();
            })));

        public new double Width
        {
            get
            {
                return (double)GetValue(WidthProperty);
            }
            set
            {
                SetValue(WidthProperty, value);
            }
        }

        private static new readonly DependencyProperty WidthProperty = DependencyProperty.Register(nameof(Width), typeof(double), typeof(Carousel),
        new PropertyMetadata(100.0, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));

        public new double Height
        {
            get
            {
                return (double)GetValue(HeightProperty);
            }
            set
            {
                SetValue(HeightProperty, value);
            }
        }

        private static new readonly DependencyProperty HeightProperty = DependencyProperty.Register(nameof(Height), typeof(double), typeof(Carousel),
        new PropertyMetadata(100.0, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));

        public int NavigationSpeed
        {
            get
            {
                return (int)base.GetValue(NavigationSpeedProperty);
            }
            set
            {
                if (value != NavigationSpeed)
                {
                    base.SetValue(NavigationSpeedProperty, value);
                }
                else
                {
                    base.SetValue(NavigationSpeedProperty, value);
                }
            }
        }

        public static readonly DependencyProperty NavigationSpeedProperty = DependencyProperty.Register(nameof(NavigationSpeed), typeof(int), typeof(Carousel),
        new PropertyMetadata(500, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));


        bool _selectedItemScaleInitialized;
        public float SelectedItemScale
        {
            get
            {
                return (float)base.GetValue(SelectedItemScaleProperty);
            }
            set
            {
                if (_selectedItemScaleInitialized && value != SelectedItemScale)
                {
                    base.SetValue(SelectedItemScaleProperty, value);
                    _restartExpressionsTimer.Start();
                    SetSizeAndGestureEvents();
                }
                else
                {
                    base.SetValue(SelectedItemScaleProperty, value);
                    _selectedItemScaleInitialized = true;
                }
            }
        }

        public static readonly DependencyProperty SelectedItemScaleProperty = DependencyProperty.Register(nameof(SelectedItemScale), typeof(float), typeof(Carousel),
        new PropertyMetadata(1.5f, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));

        int SelectedItemFalloff
        {
            get
            {
                return (AdditionalItemsToScale + 2) / 2;
            }
        }

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

        public static readonly DependencyProperty AdditionalItemsToScaleProperty = DependencyProperty.Register(nameof(AdditionalItemsToScale), typeof(int), typeof(Carousel),
            new PropertyMetadata(0, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                control.Refresh();
            })));

        bool _additionalItemsToWarpInitialized;
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
            new PropertyMetadata(0, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                control.Refresh();
            })));

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
        new PropertyMetadata(CarouselTypes.Row, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));

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
            new PropertyMetadata(WheelAlignments.Right, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                control.Refresh();
            })));

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
        new PropertyMetadata(36, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));

        public float FliptychDegrees
        {
            get
            {
                return (float)base.GetValue(FliptychDegreesProperty);
            }
            set
            {
                base.SetValue(FliptychDegreesProperty, value);
            }
        }

        public static readonly DependencyProperty FliptychDegreesProperty = DependencyProperty.Register(nameof(FliptychDegrees), typeof(float), typeof(Carousel),
        new PropertyMetadata(0f, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));


        bool _wheelScaleInitialized;
        public double WheelScale
        {
            get
            {
                return (double)base.GetValue(WheelScaleProperty);
            }
            set
            {
                if (_wheelScaleInitialized && WheelScale != value)
                {
                    base.SetValue(WheelScaleProperty, value);
                }
                else
                {
                    base.SetValue(WheelScaleProperty, value);
                    _wheelScaleInitialized = true;
                }
            }
        }
        public static readonly DependencyProperty WheelScaleProperty = DependencyProperty.Register(nameof(WheelScale), typeof(double), typeof(Carousel),
        new PropertyMetadata(1.0, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));

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
        new PropertyMetadata(0, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));

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
        new PropertyMetadata(.002, new PropertyChangedCallback((s, e) =>
        {
            var control = s as Carousel;
            control.Refresh();
        })));

        bool _itemGapInitialized;
        public int ItemGap
        {
            get
            {
                return (int)base.GetValue(ItemGapProperty);
            }
            set
            {
                if (_itemGapInitialized && value != ItemGap)
                {
                    base.SetValue(ItemGapProperty, value);
                    _restartExpressionsTimer.Start();
                }
                else
                {
                    base.SetValue(ItemGapProperty, value);
                    _itemGapInitialized = true;
                }
            }
        }

        public static readonly DependencyProperty ItemGapProperty = DependencyProperty.Register(nameof(ItemGap), typeof(int), typeof(Carousel),
        new PropertyMetadata(0, new PropertyChangedCallback((s, e) =>
            {
                var control = s as Carousel;
                control.Refresh();
            })));

        #endregion

        #region EVENT METHODS

        private void Carousel_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            LoadNewCarousel();
        }

        private void Carousel_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(this);
            switch (point.Properties.MouseWheelDelta)
            {
                case 120:
                    SelectPrevious(true);
                    break;
                case -120:
                    SelectNext(true);
                    break;
            }
        }

        private void GestureHitbox_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.PointerCursor != null)
            {
                // TODO: Implement cursor change
            }
        }

        private void GestureHitbox_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (Window.Current.CoreWindow.PointerCursor != null)
            {
                // TODO: Implement cursor change
            }
        }

        private void _gestureHitbox_ManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            if (IsCarouselMoving)
            {
                IsCarouselMoving = false;
            }
        }

        private void _gestureHitbox_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (ItemsSource != null && EnableGestures)
            {
                if (!IsCarouselMoving)
                {
                    IsCarouselMoving = true;
                }

                switch (CarouselType)
                {
                    case CarouselTypes.Wheel:
                        switch (this.WheelAlignment)
                        {
                            case WheelAlignments.Right:
                                CarouselRotationAngle -= Convert.ToSingle(e.Delta.Translation.Y / 4);
                                break;
                            case WheelAlignments.Left:
                                CarouselRotationAngle += Convert.ToSingle(e.Delta.Translation.Y / 4);
                                break;
                            case WheelAlignments.Top:
                                CarouselRotationAngle -= Convert.ToSingle(e.Delta.Translation.X / 4);
                                break;
                            case WheelAlignments.Bottom:
                                CarouselRotationAngle += Convert.ToSingle(e.Delta.Translation.X / 4);
                                break;
                        }
                        OnCarouselRotationAngleChanged(new CarouselChangedArgs { Rotation = CarouselRotationAngle });
                        break;
                    case CarouselTypes.Column:
                        CarouselPositionY = Convert.ToSingle(e.Cumulative.Translation.Y * 2);
                        OnCarouselPositionYChanged(new CarouselChangedArgs { PositionY = CarouselPositionY });
                        break;
                    case CarouselTypes.Row:
                        CarouselPositionX = Convert.ToSingle(e.Cumulative.Translation.X * 2);
                        OnCarouselPositionXChanged(new CarouselChangedArgs { PositionX = CarouselPositionX });
                        break;
                }
            }
        }
        #endregion

        #region SMOOTH SCROLL METHODS

        void UpdateCarouselVerticalScrolling(float newValue)
        {
            if (Items != null && _itemsLayerGrid.Children.Count == Density)
            {
                _scrollValue = newValue - _scrollSnapshot;
                var selectedIdx = Modulus(((Density - 1) - (displaySelectedIndex)), Density);

                _scrollSnapshot = newValue;
                while (newValue > _currentColumnYPosTick + _itemHeight + ItemGap)
                {
                    SelectPrevious(true);
                    Interlocked.Add(ref _currentColumnYPosTick, (_itemHeight + ItemGap));
                }
                while (newValue < _currentColumnYPosTick - (_itemHeight + ItemGap))
                {
                    SelectNext(true);
                    Interlocked.Add(ref _currentColumnYPosTick, -(_itemHeight + ItemGap));
                }

                for (int i = (Density - 1); i > -1; i--)
                {
                    int idx = Modulus(((Density - 1) - i), Density);
                    if (_itemsLayerGrid.Children[idx] is Grid itemGrid)
                    {
                        var itemGridVisual = ElementCompositionPreview.GetElementVisual(itemGrid);
                        itemGridVisual.ImplicitAnimations.Remove("Offset");
                        //itemGridVisual.ImplicitAnimations.Clear();
                        var currentX = itemGridVisual.Offset.X;
                        var currentY = itemGridVisual.Offset.Y;
                        var currentZ = itemGridVisual.Offset.Z;
                        itemGridVisual.Offset = new Vector3(currentX, currentY + _scrollValue, currentZ);
                    }
                }
            }
        }

        void UpdateCarouselHorizontalScrolling(float newValue)
        {
            if (Items != null && _itemsLayerGrid.Children.Count == Density)
            {
                _scrollValue = newValue - _scrollSnapshot;
                var selectedIdx = Modulus(((Density - 1) - (displaySelectedIndex)), Density);

                _scrollSnapshot = newValue;
                while (newValue > _currentRowXPosTick + _itemWidth + ItemGap)
                {
                    SelectPrevious(true);
                    Interlocked.Add(ref _currentRowXPosTick, (_itemWidth + ItemGap));
                }
                while (newValue < _currentRowXPosTick - (_itemWidth + ItemGap))
                {
                    SelectNext(true);
                    Interlocked.Add(ref _currentRowXPosTick, -(_itemWidth + ItemGap));
                }

                for (int i = (Density - 1); i > -1; i--)
                {
                    int idx = Modulus(((Density - 1) - i), Density);
                    if (_itemsLayerGrid.Children[idx] is FrameworkElement itemElement)
                    {
                        var itemElementVisual = ElementCompositionPreview.GetElementVisual(itemElement);
                        if (itemElementVisual.ImplicitAnimations != null)
                        {
                            itemElementVisual.ImplicitAnimations.Remove("Offset");
                        }
                        var currentX = itemElementVisual.Offset.X;
                        var currentY = itemElementVisual.Offset.Y;
                        var currentZ = itemElementVisual.Offset.Z;
                        itemElementVisual.Offset = new System.Numerics.Vector3(currentX + _scrollValue, currentY, currentZ);
                    }
                }
            }
        }

        void UpdateWheelSpinning(float newValue)
        {
            if (Items != null && _itemsLayerGrid.Children.Count == Density)
            {
                _dynamicGridVisual.RotationAngleInDegrees = newValue;
                bool tick = false;
                while (newValue > _currentWheelTick + degrees)
                {
                    _currentWheelTick += degrees;
                    switch (WheelAlignment)
                    {
                        case WheelAlignments.Right:
                        case WheelAlignments.Top:
                            SelectNext(true);
                            break;
                        case WheelAlignments.Left:
                        case WheelAlignments.Bottom:
                            SelectPrevious(true);
                            break;
                    }
                    tick = true;
                }
                while (newValue < _currentWheelTick - degrees)
                {
                    _currentWheelTick -= degrees;
                    switch (WheelAlignment)
                    {
                        case WheelAlignments.Right:
                        case WheelAlignments.Top:
                            SelectPrevious(true);
                            break;
                        case WheelAlignments.Left:
                        case WheelAlignments.Bottom:
                            SelectNext(true);
                            break;
                    }
                    tick = true;
                }

                for (int i = (Density - 1); i > -1; i--)
                {
                    int idx = Modulus(((Density - 1) - i), Density);
                    if (_itemsLayerGrid.Children[idx] is FrameworkElement itemElement)
                    {
                        var itemGridVisual = ElementCompositionPreview.GetElementVisual(itemElement);
                        if (itemGridVisual.ImplicitAnimations != null)
                        {
                            itemGridVisual.ImplicitAnimations.Clear();
                        }
                        var currentX = itemGridVisual.Offset.X;
                        var currentY = itemGridVisual.Offset.Y;
                        var currentZ = itemGridVisual.Offset.Z;
                        if (tick)
                        {
                            itemGridVisual.Offset = new System.Numerics.Vector3(currentX, currentY, currentZ);
                        }
                    }
                }
                tick = false;
            }

        }


        public async void StopCarouselMoving()

        {
            var selectedIdx = Modulus(((Density - 1) - (displaySelectedIndex)), Density);
            if (CarouselType == CarouselTypes.Wheel)
            {
                AddImplicitWheelSnapToAnimation(_dynamicGridVisual);
                _dynamicGridVisual.RotationAngleInDegrees = (float)_currentWheelTick;
                var animation = (ScalarKeyFrameAnimation)_dynamicGridVisual.ImplicitAnimations["RotationAngleInDegrees"];
                await Task.Delay(animation.Duration);
                _dynamicGridVisual.ImplicitAnimations.Clear();
                _currentWheelTick = _currentWheelTick % 360;
                _dynamicGridVisual.RotationAngleInDegrees = (float)_currentWheelTick;
                _carouselRotationAngle = _currentWheelTick;
                OnCarouselRotationAngleChanged(new CarouselChangedArgs { Rotation = _currentWheelTick });
            }

            var offsetVertical = _itemHeight + ItemGap;
            var offsetHorizontal = _itemWidth + ItemGap;

            for (int i = -((Density / 2) - 1); i <= (Density / 2); i++)
            {
                int j = Modulus((selectedIdx + i), Density);
                if (_itemsLayerGrid != null && _itemsLayerGrid.Children[j] is Grid itemGrid)
                {
                    var itemGridVisual = ElementCompositionPreview.GetElementVisual(itemGrid);
                    AddStandardImplicitItemAnimation(itemGridVisual);
                    if (CarouselType == CarouselTypes.Column)
                    {
                        var currentX = itemGridVisual.Offset.X;
                        itemGridVisual.Offset = new System.Numerics.Vector3(currentX, offsetVertical * -i, (Density - Math.Abs(i)));
                    }
                    else if (CarouselType == CarouselTypes.Row)
                    {
                        var currentY = itemGridVisual.Offset.Y;
                        itemGridVisual.Offset = new System.Numerics.Vector3(offsetHorizontal * -i, currentY, (Density - Math.Abs(i)));
                    }
                }
            }
            AddImplicitWheelRotationAnimation(_dynamicGridVisual);
            _carouselPositionY = 0;
            OnCarouselPositionYChanged(new CarouselChangedArgs { PositionY = 0 });
            _currentColumnYPosTick = 0;
            _carouselPositionX = 0;
            OnCarouselPositionXChanged(new CarouselChangedArgs { PositionX = 0 });
            _currentRowXPosTick = 0;
            _scrollValue = 0;
            _scrollSnapshot = 0;
            OnSelectionChanged(new CarouselSelectionChangedArgs { SelectedIndex = this.SelectedIndex });
            UpdateZIndices();
        }

        #endregion

        #region INITIALIZATION

        void Refresh()
        {
            if (this.IsLoaded)
            {
                _delayedRefreshTimer.Start();
            }
        }

        void LoadNewCarousel()
        {
            _cancelTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancelTokenSource.Token;
            CreateContainers();
            if (Items != null && Items.Count() > 0)
            {
                try
                {
                    for (int i = (Density - 1); i > -1; i--)
                    {
                        if (cancellationToken.IsCancellationRequested) { break; }
                        int playlistIdx = (i + this.startIndex) % Items.Count();
                        var itemElement = CreateItemInCarouselSlot(i, playlistIdx);
                        _itemsLayerGrid.Children.Add(itemElement);
                    }
                    if (cancellationToken.IsCancellationRequested) { return; }
                    int slot = Density / 2;
                    for (int i = 0; i < Density; i++)
                    {
                        if (slot < 0)
                        {
                            slot *= -1;
                        }
                        else if (slot > 0)
                        {
                            slot++;
                            slot *= -1;
                        }
                        else
                        {
                            slot++;
                        }
                    }

                    if (cancellationToken.IsCancellationRequested) { return; }

                    foreach (var element in _itemsLayerGrid.Children)
                    {
                        StartExpressionItemAnimations(element as FrameworkElement);
                    }
                    UpdateZIndices();

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

            }
            if (cancellationToken.IsCancellationRequested) { return; }
            SetSizeAndGestureEvents();
        }


        void CreateContainers()
        {
            this.Children.Clear();
            _currentRowXPosTick = 0;
            _currentWheelTick = 0;
            _carouselInsertPosition = 0;

            _dynamicContainerGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            _dynamicGridVisual = ElementCompositionPreview.GetElementVisual(_dynamicContainerGrid);
            _dynamicGridVisual.CenterPoint = new System.Numerics.Vector3(_itemWidth / 2, _itemHeight / 2, 0);
            ElementCompositionPreview.SetIsTranslationEnabled(_dynamicContainerGrid, true);
            AddImplicitWheelRotationAnimation(_dynamicGridVisual);

            _dynamicWheelUnderlayGrid = new ContentControl();

            _itemsLayerGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

            if (CarouselType == CarouselTypes.Wheel)
            {
                _dynamicContainerGrid.Children.Add(_dynamicWheelUnderlayGrid);
            }

            _dynamicContainerGrid.Children.Add(_itemsLayerGrid);
            this.Children.Add(_dynamicContainerGrid);


            _outerGridVisual = ElementCompositionPreview.GetElementVisual(this);
            ElementCompositionPreview.SetIsTranslationEnabled(this, true);

            _gestureHitbox = new Canvas
            {
                Name = "Hitbox",
                Background = new SolidColorBrush(Colors.Transparent),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            this.Children.Add(_gestureHitbox);

        }

        public void _restartExpressionsTimer_Tick(object sender, object e)
        {
            _restartExpressionsTimer.Stop();
            if (this.IsLoaded)
            {
                StopExpressionAnimations(true);
            }
        }
        private void _delayedRefreshTimer_Tick(object sender, object e)
        {
            _delayedRefreshTimer.Stop();
            LoadNewCarousel();
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

        void StopExpressionAnimations(bool restart)
        {
            if (_itemsLayerGrid != null)
            {
                foreach (var child in _itemsLayerGrid.Children)
                {
                    if (child is FrameworkElement element)
                    {
                        StopExpressionAnimations(element, restart);
                    }
                }
            }
        }

        void StopExpressionAnimations(FrameworkElement element, bool restart)
        {
            var visual = ElementCompositionPreview.GetElementVisual(element);
            visual.StopAnimation("Translation.X");
            visual.StopAnimation("Translation.Y");
            visual.StopAnimation("Scale.X");
            visual.StopAnimation("Scale.Y");
            visual.Scale = new Vector3(1, 1, 1);

            var childVisual = ElementCompositionPreview.GetElementChildVisual(element);
            if (childVisual is SpriteVisual spriteVisual)
            {
                spriteVisual.StopAnimation("RotationAngleInDegrees");
            }

            if (restart)
            {
                StartExpressionItemAnimations(element);
            }
        }

        void StartExpressionItemAnimations(FrameworkElement element)
        {
            // Scaling Expression Animation
            Visual visual = ElementCompositionPreview.GetElementVisual(element);
            var compositor = Window.Current.Compositor;
            var scaleRange = SelectedItemScale - 1;
            ScalarNode distanceAsPercentOfScaleThreshold = null;
            ScalarNode scaleThresholdDistanceRaw = null;
            BooleanNode distanceIsNegativeValue = null;
            BooleanNode isWithinScaleThreshold = null;
            float scaleItemsthreshold = 0;
            if (CarouselType == CarouselTypes.Wheel)
            {
                scaleItemsthreshold = AdditionalItemsToScale == 0 ? degrees : degrees * AdditionalItemsToScale;

                var slotDegrees = ((Int32.Parse(element.Name) + (Density / 2)) % Density) * degrees;

                float wheelDegreesWhenItemIsSelected = Convert.ToSingle(slotDegrees);

                ScalarNode wheelAngle = _dynamicGridVisual.GetReference().RotationAngleInDegrees;

                float wheelAngleForDebug = _dynamicGridVisual.RotationAngleInDegrees;

                float distanceRawForDebug = 0;
                switch (WheelAlignment)
                {
                    case WheelAlignments.Right:
                        scaleThresholdDistanceRaw = ExpressionFunctions.Mod((wheelAngle - wheelDegreesWhenItemIsSelected), 360);
                        distanceRawForDebug = wheelAngleForDebug - wheelDegreesWhenItemIsSelected;
                        distanceRawForDebug = distanceRawForDebug % 360;
                        break;
                    case WheelAlignments.Top:
                        scaleThresholdDistanceRaw = ExpressionFunctions.Mod((wheelAngle - wheelDegreesWhenItemIsSelected), 360);
                        distanceRawForDebug = wheelAngleForDebug - wheelDegreesWhenItemIsSelected;
                        distanceRawForDebug = distanceRawForDebug % 360;
                        break;
                    case WheelAlignments.Left:
                        scaleThresholdDistanceRaw = ExpressionFunctions.Mod((wheelAngle + wheelDegreesWhenItemIsSelected), 360);
                        distanceRawForDebug = wheelAngleForDebug + wheelDegreesWhenItemIsSelected;
                        distanceRawForDebug = distanceRawForDebug % 360;
                        break;
                    case WheelAlignments.Bottom:
                        scaleThresholdDistanceRaw = ExpressionFunctions.Mod((wheelAngle + wheelDegreesWhenItemIsSelected), 360);
                        distanceRawForDebug = wheelAngleForDebug + wheelDegreesWhenItemIsSelected;
                        distanceRawForDebug = distanceRawForDebug % 360;
                        break;
                }



                ScalarNode distanceToZero = ExpressionFunctions.Abs(scaleThresholdDistanceRaw);
                ScalarNode distanceTo360 = 360 - distanceToZero;
                BooleanNode isClosestToZero = distanceToZero <= distanceTo360;
                ScalarNode distanceInDegrees = ExpressionFunctions.Conditional(isClosestToZero, distanceToZero, distanceTo360);
                distanceAsPercentOfScaleThreshold = distanceInDegrees / scaleItemsthreshold;

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

                ScalarNode scalePercent = scaleRange * (1 - distanceAsPercentOfScaleThreshold) + 1;
                isWithinScaleThreshold = distanceInDegrees < scaleItemsthreshold;
                ScalarNode finalScaleValue = ExpressionFunctions.Conditional(isWithinScaleThreshold, scalePercent, 1);

                // Two animations required, a single Vector3 animation on Scale results in a string-too-long exception.
                if (SelectedItemScale > 1)
                {
                    visual.StartAnimation("Scale.X", finalScaleValue);
                    visual.StartAnimation("Scale.Y", finalScaleValue);
                }
            }
            else if (CarouselType == CarouselTypes.Row || CarouselType == CarouselTypes.Column)
            {
                scaleItemsthreshold = isXaxisNavigation ? AdditionalItemsToScale * (_itemWidth + ItemGap) : AdditionalItemsToScale * (_itemHeight + ItemGap);
                if (scaleItemsthreshold == 0)
                {
                    scaleItemsthreshold = isXaxisNavigation ? _itemWidth + ItemGap : _itemHeight + ItemGap;
                }

                Vector3Node offset = visual.GetReference().Offset;
                scaleThresholdDistanceRaw = this.isXaxisNavigation ? offset.X / scaleItemsthreshold : offset.Y / scaleItemsthreshold;

                distanceAsPercentOfScaleThreshold = ExpressionFunctions.Abs(scaleThresholdDistanceRaw);

                distanceIsNegativeValue = scaleThresholdDistanceRaw < 0;
                isWithinScaleThreshold = isXaxisNavigation ? offset.X > -scaleItemsthreshold & offset.X < scaleItemsthreshold : offset.Y > -scaleItemsthreshold & offset.Y < scaleItemsthreshold;


                ScalarNode scalePercent = scaleRange * (1 - distanceAsPercentOfScaleThreshold) + 1;
                ScalarNode finalScaleValue = ExpressionFunctions.Conditional(isWithinScaleThreshold, scalePercent, 1);

                // Two animations required, a single Vector3 animation on Scale results in a string-too-long exception.
                if (SelectedItemScale > 1)
                {
                    visual.StartAnimation("Scale.X", finalScaleValue);
                    visual.StartAnimation("Scale.Y", finalScaleValue);
                }

                if (WarpIntensity != 0)
                {
                    var warpItemsthreshold = isXaxisNavigation ? AdditionalItemsToWarp * (_itemWidth + ItemGap) : AdditionalItemsToWarp * (_itemHeight + ItemGap);
                    if (warpItemsthreshold == 0)
                    {
                        warpItemsthreshold = isXaxisNavigation ? _itemWidth + ItemGap : _itemHeight + ItemGap;
                    }
                    var warpThresholdDistanceRaw = this.isXaxisNavigation ? offset.X / warpItemsthreshold : offset.Y / warpItemsthreshold;
                    var distanceAsPercentOfWarpThreshold = ExpressionFunctions.Abs(warpThresholdDistanceRaw);
                    var isWithinWarpThreshold = isXaxisNavigation ? offset.X > -warpItemsthreshold & offset.X < warpItemsthreshold : offset.Y > -warpItemsthreshold & offset.Y < warpItemsthreshold;
                    ScalarNode y = WarpIntensity - (distanceAsPercentOfWarpThreshold * WarpIntensity);
                    ScalarNode WarpOffset = Convert.ToSingle(-WarpCurve) * warpThresholdDistanceRaw * warpThresholdDistanceRaw + WarpIntensity;
                    ScalarNode finalWarpValue = ExpressionFunctions.Conditional(isWithinWarpThreshold, y * ExpressionFunctions.Abs(y) * (float)WarpCurve, 0);
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

            // Fliptych
            if (useFliptych && CarouselType != CarouselTypes.Wheel) // TODO: Implement Fliptych on Wheel
            {
                var fliptychDegrees = isXaxisNavigation ? FliptychDegrees : -FliptychDegrees;
                if (CarouselType == CarouselTypes.Wheel) { fliptychDegrees *= -1; }
                visual.RotationAxis = isXaxisNavigation ? new Vector3(0, 1, 0) : new Vector3(1, 0, 0);
                ScalarNode rotatedValue = ExpressionFunctions.Conditional(distanceIsNegativeValue, fliptychDegrees, -fliptychDegrees);

                ScalarNode finalValue = ExpressionFunctions.Conditional(isWithinScaleThreshold, distanceAsPercentOfScaleThreshold * rotatedValue, rotatedValue);
                visual.StartAnimation("RotationAngleInDegrees", finalValue);
            }
        }


        private void AddStandardImplicitItemAnimation(Visual visual)
        {
            AddStandardImplicitItemAnimation(visual, NavigationSpeed, false);
        }

        private void AddStandardImplicitItemAnimation(Visual visual, int durationMilliseconds, bool rotation)
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

        FrameworkElement CreateItemInCarouselSlot(int i, int playlistIdx)
        {
            if (ItemTemplate != null)
            {
                FrameworkElement element = ItemTemplate.LoadContent() as FrameworkElement;
                element.DataContext = Items[playlistIdx];
                element.Name = i.ToString();
                _itemHeight = (int)element.Height;
                _itemWidth = (int)element.Width;
                ElementCompositionPreview.SetIsTranslationEnabled(element, true);
                var elementVisual = ElementCompositionPreview.GetElementVisual(element);
                elementVisual.CenterPoint = new System.Numerics.Vector3((float)(_itemWidth / 2), (float)(_itemHeight / 2), 0);
                var translateX = GetTranslateX(i);
                var translateY = GetTranslateY(i);
                elementVisual.Offset = new System.Numerics.Vector3((float)translateX, (float)translateY, 0);
                if (CarouselType == CarouselTypes.Wheel)
                {
                    elementVisual.RotationAngleInDegrees = GetRotation(i);
                }
                AddStandardImplicitItemAnimation(elementVisual);
                return element;

            }
            return null;

        }

        void UpdateItemInCarouselSlot(int carouselIdx, int sourceIdx, bool loFi, bool fastScroll)
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
                    var precedingItemGridVisual = ElementCompositionPreview.GetElementVisual(precedingItemElement);

                    if (elementVisual.ImplicitAnimations != null) { elementVisual.ImplicitAnimations.Clear(); }

                    switch (CarouselType)
                    {
                        case CarouselTypes.Row:
                            if (loFi)
                            {
                                translateX = IsCarouselMoving ? precedingItemGridVisual.Offset.X - (_itemWidth + ItemGap)
                                    : translateX - (((Density / 2) * (_itemWidth + ItemGap)) + _itemWidth + ItemGap);

                            }
                            else
                            {
                                translateX = IsCarouselMoving ? precedingItemGridVisual.Offset.X + _itemWidth + ItemGap :
                                    (Density / 2) * (_itemWidth + ItemGap);
                            }
                            break;
                        case CarouselTypes.Column:
                            if (loFi)
                            {
                                translateY = IsCarouselMoving ? precedingItemGridVisual.Offset.Y - (_itemHeight + ItemGap) :
                                    translateY - (((Density / 2) * (_itemHeight + ItemGap)) + _itemHeight + ItemGap);
                            }
                            else
                            {
                                translateY = IsCarouselMoving ? precedingItemGridVisual.Offset.Y + _itemHeight + ItemGap :
                                    (Density / 2) * (_itemHeight + ItemGap);
                            }
                            break;
                    }
                    elementVisual.Offset = new System.Numerics.Vector3((float)translateX, (float)translateY, 0);
                    AddStandardImplicitItemAnimation(elementVisual);

                    if (fastScroll)
                    {
                        var precedingItemZIndex = Canvas.GetZIndex(precedingItemElement);
                        Canvas.SetZIndex(element, precedingItemZIndex - 1);
                    }

                }
            }
        }


        public void SelectNext()
        {
            SelectNext(false);
        }
        void SelectNext(bool calledInternally, int? startIdx = null)
        {
            var animate = !IsCarouselMoving;
            if (Items != null && Items.Count() > 0)
            {

                if (calledInternally)
                {
                    _selectedIndexSetInternally = true;
                    SelectedIndex = (SelectedIndex + 1) % Items.Count();
                }

                _carouselInsertPosition = (_carouselInsertPosition + 1) % Density;

                int pIdx;
                if (startIdx == null)
                {
                    pIdx = ((startIndex + (Density - 1)) % Items.Count());
                }
                else
                {
                    pIdx = (((int)startIdx + (Density - 1)) % Items.Count());
                }

                if (animate)
                {
                    switch (CarouselType)
                    {
                        default:
                            switch (WheelAlignment)
                            {
                                default:
                                    RotateWheel(true);
                                    break;
                                case WheelAlignments.Left:
                                case WheelAlignments.Bottom:
                                    RotateWheel(false);
                                    break;
                            }
                            UpdateItemInCarouselSlot(Modulus((_carouselInsertPosition - 1), Density), pIdx, false, animate);
                            break;
                        case CarouselTypes.Column:
                            UpdateItemInCarouselSlot(Modulus((_carouselInsertPosition - 1), Density), pIdx, false, animate);
                            ScrollVerticalColumn(true);
                            break;
                        case CarouselTypes.Row:
                            UpdateItemInCarouselSlot(Modulus((_carouselInsertPosition - 1), Density), pIdx, false, animate);
                            ScrollHorizontalRow(true);
                            break;
                    }
                    OnSelectionChanged(new CarouselSelectionChangedArgs { SelectedIndex = this.SelectedIndex });
                }
                else
                {
                    UpdateItemInCarouselSlot(Modulus((_carouselInsertPosition - 1), Density), pIdx, false, animate);
                }

                UpdateZIndices();
            }
        }
        public void SelectPrevious()
        {
            SelectPrevious(false);
        }
        void SelectPrevious(bool calledInternally, int? playlistStartIdx = null)
        {
            var animate = !IsCarouselMoving;
            if (Items != null && Items.Count() > 0)
            {

                if (calledInternally)
                {
                    _selectedIndexSetInternally = true;
                    SelectedIndex = Modulus(SelectedIndex - 1, Items.Count());
                }

                _carouselInsertPosition = Modulus((_carouselInsertPosition - 1), Density);

                int pIdx;
                if (playlistStartIdx == null)
                {
                    pIdx = startIndex;
                }
                else
                {
                    pIdx = (int)playlistStartIdx;
                }

                if (animate)
                {
                    switch (CarouselType)
                    {
                        default:
                            switch (WheelAlignment)
                            {
                                default:
                                    RotateWheel(false);
                                    break;
                                case WheelAlignments.Left:
                                case WheelAlignments.Bottom:
                                    RotateWheel(true);
                                    break;
                            }
                            UpdateItemInCarouselSlot(_carouselInsertPosition, pIdx, true, animate);
                            break;
                        case CarouselTypes.Column:
                            UpdateItemInCarouselSlot(_carouselInsertPosition, pIdx, true, animate);
                            ScrollVerticalColumn(false);
                            break;
                        case CarouselTypes.Row:
                            UpdateItemInCarouselSlot(_carouselInsertPosition, pIdx, true, animate);
                            ScrollHorizontalRow(false);
                            break;
                    }
                    OnSelectionChanged(new CarouselSelectionChangedArgs { SelectedIndex = this.SelectedIndex });
                }
                else
                {
                    UpdateItemInCarouselSlot(_carouselInsertPosition, pIdx, true, animate);
                }

                UpdateZIndices(IsCarouselMoving);
            }
        }

        public void AnimateToSelectedIndex()
        {
            // Determine closest animation direction
            var count = Items != null && Items.Count >= 0 ? Items.Count() : 0;
            var oldIdx = _previousSelectedIndex;
            var newIdx = SelectedIndex;
            var distance = ModularDistance(oldIdx, newIdx, count);
            bool goForward = false;
            if (Tilted.Common.Mod(oldIdx + distance, count) == newIdx)
            {
                goForward = true;
            }

            var steps = distance > Density ? Density : distance;

            if (goForward)
            {
                var startIdx = Modulus((newIdx + 1 - steps - (Density / 2)), count);
                for (int i = 0; i < steps; i++)
                {
                    SelectNext(false, startIdx + i);
                }
            }
            else
            {
                var startIdx = Modulus(newIdx - 1 + steps - (Density / 2), count);
                for (int i = 0; i < steps; i++)
                {
                    SelectPrevious(false, Tilted.Common.Mod(startIdx - i, count));
                }
            }
        }

        public async Task AnimateSelection()
        {
            if (this.SelectedItemContent is FrameworkElement selectedItemContent)
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
                    if (sb.Duration.TimeSpan != null)
                    {
                        await Task.Delay((int)sb.Duration.TimeSpan.TotalMilliseconds);
                    }
                }
            }
        }

        private void RotateWheel(bool clockwise)
        {
            float endAngle = (clockwise) ? degrees : -degrees;
            var newVal = Convert.ToInt32(_dynamicGridVisual.RotationAngleInDegrees + endAngle);
            _dynamicGridVisual.RotationAngleInDegrees = newVal;
            _carouselRotationAngle = _dynamicGridVisual.RotationAngleInDegrees;
            OnCarouselRotationAngleChanged(new CarouselChangedArgs { Rotation = _carouselRotationAngle });
            _currentWheelTick += endAngle;
        }

        public void ScrollVerticalColumn(bool scrollUp)
        {
            long endPosition = (scrollUp) ? -(_itemHeight + ItemGap) : (_itemHeight + ItemGap);
            for (int i = (Density - 1); i > -1; i--)
            {
                int idx = Modulus(((Density - 1) - i), Density);
                if (_itemsLayerGrid != null && _itemsLayerGrid.Children[idx] is Grid itemGrid)
                {
                    var itemGridVisual = ElementCompositionPreview.GetElementVisual(itemGrid);
                    var currentX = itemGridVisual.Offset.X;
                    var currentY = itemGridVisual.Offset.Y;
                    itemGridVisual.Offset = new Vector3(currentX, currentY + endPosition, 0);
                }
            }
        }

        public void ScrollHorizontalRow(bool scrollLeft)
        {
            long endPosition = (scrollLeft) ? -(_itemWidth + ItemGap) : (_itemWidth + ItemGap);
            for (int i = (Density - 1); i > -1; i--)
            {
                int idx = Modulus(((Density - 1) - i), Density);
                if (_itemsLayerGrid != null && _itemsLayerGrid.Children[idx] is Grid itemGrid)
                {
                    var itemGridVisual = ElementCompositionPreview.GetElementVisual(itemGrid);
                    var currentX = itemGridVisual.Offset.X;
                    var currentY = itemGridVisual.Offset.Y;
                    itemGridVisual.Offset = new System.Numerics.Vector3(currentX + endPosition, currentY, 0);
                }
            }
        }

        #endregion

        #region UPDATE UI METHODS

        public void SetSizeAndGestureEvents()
        {
            _gestureHitbox.ManipulationDelta -= _gestureHitbox_ManipulationDelta;
            _gestureHitbox.ManipulationCompleted -= _gestureHitbox_ManipulationCompleted;
            this.PointerWheelChanged -= Carousel_PointerWheelChanged;
            _gestureHitbox.ManipulationMode = this.EnableGestures ? ManipulationModes.All : ManipulationModes.None;

            if (Items != null && Items.Count() > 0)
            {
                if (this.EnableGestures)
                {
                    _gestureHitbox.ManipulationDelta += _gestureHitbox_ManipulationDelta;
                    _gestureHitbox.ManipulationCompleted += _gestureHitbox_ManipulationCompleted;
                }

                if (EnableMouseWheel)
                {
                    this.PointerWheelChanged += Carousel_PointerWheelChanged;
                }

                if (EnableCursorChange)
                {
                    _gestureHitbox.PointerEntered += GestureHitbox_PointerEntered;
                    _gestureHitbox.PointerExited += GestureHitbox_PointerExited;
                }


                switch (CarouselType)
                {
                    default:
                        _gestureHitbox.Width = Width;
                        _gestureHitbox.Height = Height;
                        break;
                    case CarouselTypes.Wheel:
                        float ws = 0;
                        switch (WheelAlignment)
                        {
                            case WheelAlignments.Bottom:
                            case WheelAlignments.Top:
                                ws = WheelSize + (_itemHeight * SelectedItemScale);
                                break;
                            case WheelAlignments.Left:
                            case WheelAlignments.Right:
                                ws = WheelSize + (_itemWidth * SelectedItemScale);
                                break;
                        }
                        _gestureHitbox.Width = ws;
                        _gestureHitbox.Height = ws;
                        break;
                    case CarouselTypes.Column:
                        _gestureHitbox.Width = _itemWidth * SelectedItemScale;
                        _gestureHitbox.Height = Height;
                        break;
                    case CarouselTypes.Row:
                        _gestureHitbox.Width = Width;
                        _gestureHitbox.Height = _itemHeight * SelectedItemScale;
                        break;
                }
            }

        }


        void UpdateZIndices(bool movingReverse = false)
        {
            if (Items != null && _itemsLayerGrid != null)
            {
                for (int i = -(Density / 2); i < (Density / 2); i++)
                {
                    var j = this.IsCarouselMoving ? i + 1 : i;
                    var slot = Modulus(((Density - 1) - (displaySelectedIndex + j)), Density);
                    var k = movingReverse ? j + 1 : i;
                    Canvas.SetZIndex((UIElement)_itemsLayerGrid.Children[slot], 10000 - Math.Abs(k));
                    var itemGrid = (Grid)_itemsLayerGrid.Children[slot];

                    if (j == 0)
                    {
                        itemGrid.Tag = true;
                        SelectedItemContent = itemGrid.Children.FirstOrDefault() as FrameworkElement;
                    }
                    else
                    {
                        itemGrid.Tag = false;
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
                    return ((_itemHeight + ItemGap) * (i - (Density / 2)));
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
                    return ((_itemWidth + ItemGap) * (i - (Density / 2)));
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
        public class CarouselSelectionChangedArgs : EventArgs
        {
            public FrameworkElement SelectedItem { get; set; }
            public int SelectedIndex { get; set; }
        }

        public event EventHandler SelectionChanged;

        void OnSelectionChanged(CarouselSelectionChangedArgs e)
        {
            EventHandler handler = SelectionChanged;
            if (handler != null)
                handler(this, e);
        }

        public class CarouselManipulationStateChangedArgs : EventArgs
        {
            public bool IsCarouselMoving { get; set; }
        }

        public event EventHandler CarouselManipulationStateChanged;

        public void OnCarouselManipulationStateChanged(CarouselManipulationStateChangedArgs e)
        {
            EventHandler handler = CarouselManipulationStateChanged;
            if (handler != null)
                handler(this, e);
        }

        public void OnCarouselRotationAngleChanged(CarouselChangedArgs e)
        {
            EventHandler handler = CarouselRotationAngleChanged;
            if (handler != null)
                handler(this, e);
        }
        public event EventHandler CarouselRotationAngleChanged;

        public void OnCarouselPositionXChanged(CarouselChangedArgs e)
        {
            EventHandler handler = CarouselPositionXChanged;
            if (handler != null)
                handler(this, e);
        }
        public event EventHandler CarouselPositionXChanged;

        public void OnCarouselPositionYChanged(CarouselChangedArgs e)
        {
            EventHandler handler = CarouselPositionYChanged;
            if (handler != null)
                handler(this, e);
        }
        public event EventHandler CarouselPositionYChanged;

        public class CarouselChangedArgs : EventArgs
        {
            public float Rotation { get; set; }
            public double PositionX { get; set; }
            public double PositionY { get; set; }
        }
        #endregion
    }

}
