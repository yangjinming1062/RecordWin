using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace RecordWin
{
    public class ColorPicker : ToggleButton
    {
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register("Size", typeof(ColorPickerButtonSize), typeof(ColorPicker),
            new PropertyMetadata(default(ColorPickerButtonSize), OnColorPickerSizeChanged));

        public ColorPickerButtonSize Size
        {
            get => (ColorPickerButtonSize)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        private static void OnColorPickerSizeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            var v = (ColorPickerButtonSize)eventArgs.NewValue;
            ColorPicker obj = dependencyObject as ColorPicker;
            if (obj == null) return;
            var w = 0.0;
            switch (v)
            {
                case ColorPickerButtonSize.Small:
                    w = (double)Application.Current.Resources["ColorPickerSmall"];
                    break;
                case ColorPickerButtonSize.Middle:
                    w = (double)Application.Current.Resources["ColorPickerMiddle"];
                    break;
                default:
                    w = (double)Application.Current.Resources["ColorPickerLarge"];
                    break;
            }
            obj.BeginAnimation(WidthProperty, new DoubleAnimation(w, (Duration)Application.Current.Resources["Duration3"]));
        }
    }

    public class CornerRadiusAnimation : AnimationTimeline
    {
        static CornerRadiusAnimation()
        {
            FromProperty = DependencyProperty.Register("From", typeof(CornerRadius), typeof(CornerRadius));
            ToProperty = DependencyProperty.Register("To", typeof(CornerRadius), typeof(CornerRadius));
        }

        private bool _fromSetted;
        private bool _toSetted;
        public static readonly DependencyProperty FromProperty;
        public CornerRadius From
        {
            get => (CornerRadius)GetValue(FromProperty);
            set
            {
                SetValue(FromProperty, value);
                _fromSetted = true;
            }
        }
        public static readonly DependencyProperty ToProperty;
        public CornerRadius To
        {
            get => (CornerRadius)GetValue(ToProperty);
            set
            {
                SetValue(ToProperty, value);
                _toSetted = true;
            }
        }
        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            var fromVal = _fromSetted ? (CornerRadius)GetValue(FromProperty) : (CornerRadius)defaultOriginValue;
            var toVal = _toSetted ? (CornerRadius)GetValue(ToProperty) : (CornerRadius)defaultDestinationValue;
            if (animationClock.CurrentProgress != null)
                return new CornerRadius(
                    animationClock.CurrentProgress.Value * (toVal.TopLeft - fromVal.TopLeft) + fromVal.TopLeft,
                    animationClock.CurrentProgress.Value * (toVal.TopRight - fromVal.TopRight) + fromVal.TopRight,
                    animationClock.CurrentProgress.Value * (toVal.BottomRight - fromVal.BottomRight) + fromVal.BottomRight,
                    animationClock.CurrentProgress.Value * (toVal.BottomLeft - fromVal.BottomLeft) + fromVal.BottomLeft);
            return new CornerRadius();
        }
        protected override Freezable CreateInstanceCore() => new CornerRadiusAnimation();

        public override Type TargetPropertyType => typeof(CornerRadius);
    }
}
