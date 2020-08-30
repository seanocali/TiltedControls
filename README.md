# Tilted Carousel

#### A true (looping) carousel control that makes use of UWP's Composition API. It has unique features not found in other carousels such as wheel mode, warping, adjustible lazy loading, and CompositionBrush animation support.

![WheelAnimation](https://raw.githubusercontent.com/seanocali/TiltedCarousel/master/wheel.webp)


## Properties

#### SelectedItem

 The original object of the selected item in Items. 



---
#### SelectedItemElement

 Returns the FrameworkElement of the SelectedItem. 



---
#### Items

 Returns a collection of items generated from the ItemsSource. 



---
#### WheelSize

 Returns an integer value of the wheel diameter in pixels. 



---
#### Hitbox

 Use this element's Manipulation event handlers for gesture controls. 



---
#### ItemsSource

 Assign or bind the data source you wish to present to this property. 



---
#### ItemTemplate

 Assign a DataTempate here that will be used to present each item in your data collection. 



---
#### SelectedIndex

 The index of the currently selected item in Items. 



---
#### SelectedItemForegroundBrush

 This can be used to animate a CompositionBrush property of a custom control. The control must have a container parent with a name that starts with "CarouselItemMedia." The child element's property to animate must be of type CompositionColorBrush or CompositionLinearGradientBrush. These types can be found in the namespace Windows.UI.Composition for legacy UWP, or Microsoft.UI.Composition for WinUI. Set your control's brush property to what you want for the deselected state. For the selected state, set this this property. An expression animation will be added which will create a smooth animated transition between the two brushes as items animate in and out of the selected item position. Win2D may be required to create a custom control that allows text with a CompositionBrush. 



---
#### TriggerSelectionAnimation

For MVVM. Bind this to a viewmodel property and you can call the 'SelectionAnimation()' method whenever you change its value to anything other than null. 

##### Example:  Bind a property like this (One-Way) and use an INotifyPropertyChanged handler to trigger it. 

######  code

```
    private bool _animationTrigger;
    public bool AnimationTrigger
    {
        get { return !_animationTrigger;}
    }
```





---
#### NavigationSpeed

 The time it takes, in milliseconds, for a selection change to animate. 



---
#### ZIndexUpdateWaitsForAnimation

 For carousel configurations with overlapping items. When this is disabled, the ZIndex of items update immediately upon interaction. Enable this so that it updates only after the animation is halfway complete (NavigationSpeed / 2). 



---
#### SelectedItemScale

 Sets the scale of the selected item to make it more prominent. The Framework's UIElement scaling does not do vector scaling of text or SVG images, unfortunately, so keep that in mind when using this. 



---
#### AdditionalItemsToScale

 Set the number of additional items surrounding the selected item to also scale, creating a 'falloff' effect. This value also applies to Fliptych. 



---
#### AdditionalItemsToWarp

 Set the number of additional items surrounding the selected item to also warp, creating a 'falloff' effect. 



---
#### CarouselType

 Sets the type of carousel. Chose a Column, a Row, or a Wheel. 



---
#### WheelAlignment

 When CarouselType is set to Wheel, use this property to set the conainer edge the wheel should be aligned to. 



---
#### Density

 Then number of presented items to be in the UI at once. Increasing this value may help hide visual lag associated with the lazy loading while scrolling. Decreasing it may improve performance. When CarouselType is set to wheel, use this property to adjust the space between items. 



---
#### FliptychDegrees

 Amount of 3-D rotation to apply to deselected items, creating a fliptych or "Coverflow" type of effect. 



---
#### WarpIntensity

 For Row and Column mode only. Use this in combination with WarpCurve and AdditionalItemsToWarp to create an effect where the selected item juts out from the rest. 



---
#### WarpCurve

 For Row and Column mode only. Use this in combination with WarpIntensity and AdditionalItemsToWarp to create an effect where the selected item juts out from the rest. 



---
#### ItemGap

 For Row and Column mode only. Increase or decrease (overlap) space between items. 



---
#### SelectNextTrigger

 For MVVM. Bind this to a viewmodel property and you can call 'ChangeSelection(false)' whenever you change its value to anything other than null. 

##### Example:  Bind to a property like this (One-Way) and use an INotifyPropertyChanged handler to trigger it. 

######  code

```
    private bool _selectNextTrigger;
    public bool SelectNextTrigger
    {
        get { return !_selectNextTrigger;}
    }
```





---
#### SelectPreviousTrigger

 For MVVM. Bind this to a viewmodel property and you can call 'ChangeSelection(true)' whenever you change its value to anything other than null. 

##### Example:  Bind to a property like this (One-Way) and use an INotifyPropertyChanged handler to trigger it. 

######  code

```
    private bool _selectPreviousTrigger;
    public bool SelectPreviousTrigger
    {
        get { return !_selectPreviousTrigger;}
    }
```





---
#### ManipulationStartedTrigger

 For MVVM. Bind this to a viewmodel property and you can call the 'ManipulationStarted()' method whenever you change its value to anything other than null. 

##### Example:  Bind to a property like this (One-Way) and call OnPropertyChanged to trigger it. 

######  code

```
    private bool _manipulationStartedTrigger;
    public bool ManipulationStartedTrigger
    {
        get { return !_manipulationStartedTrigger;}
    }
```





---
#### ManipulationCompletedTrigger

 For MVVM. Bind this to a viewmodel property and you can call the 'ManipulationCompleted()' method whenever you change its value to anything other than null. 

##### Example:  Bind to a property like this (One-Way) and call OnPropertyChanged to trigger it. 

######  code

```
    private bool _manipulationCompletedTrigger;
    public bool ManipulationCompletedTrigger
    {
        get { return !_manipulationCompletedTrigger;}
    }
```





---
#### CarouselRotationAngle

 Use a ManipulationDelta event to update this value to control the carousel with a dragging gesture or analog stick of a gamepade. It is important to call 'StartManipulationMode()' and 'StopManipulationMode()' before and after (respectively) updating this with a ManipulationDelta. Use ManipulationStarted and ManipulationCompleted events accordingly. 



---
#### CarouselPositionY

 Use a ManipulationDelta event to update this value to control the carousel with a dragging gesture or analog stick of a gamepade. It is important to call 'StartManipulationMode()' and 'StopManipulationMode()' before and after (respectively) updating this with a ManipulationDelta. Use ManipulationStarted and ManipulationCompleted events accordingly. 



---
#### CarouselPositionX

 Use a ManipulationDelta event to update this value to control the carousel with a dragging gesture or analog stick of a gamepade. It is important to call 'StartManipulationMode()' and 'StopManipulationMode()' before and after (respectively) updating this with a ManipulationDelta. Use ManipulationStarted and ManipulationCompleted events accordingly. 



## Methods

---
#### StartManipulationMode

 This must be called prior updating the carousel with ManipulationDelta events. MMVM implementations can use the trigger property to call it. 



---
#### StopManipulationMode

 This must be called after updating the carousel with a sequence of ManipulationDelta events from a single gesture. MMVM implementations can use the trigger property to call it. 



---
#### AnimateSelection

 This is used to trigger a storyboard animation for the selected item. Add a storyboard to resources of the root element of your ItemTemplate and assign the key "SelectionAnimation". 



---
## Events


#### SelectionChanged

 Raises whenever the selection changes. 



---


