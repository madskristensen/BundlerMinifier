using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace BundlerMinifierVsix
{
    class GeneratedAdornment
    {
        private IAdornmentLayer _adornmentLayer;
        private TextBlock _adornment;
        private double _currentOpacity = 0.4;
        private double _initOpacity;

        public GeneratedAdornment(IWpfTextView view,  bool isVisible, double initOpacity)
        {
            _adornmentLayer = view.GetAdornmentLayer(AdornmentLayer.LayerName);
            _currentOpacity = isVisible ? initOpacity : 0;
            _initOpacity = initOpacity;

            CreateImage();

            view.ViewportHeightChanged += SetAdornmentLocation;
            view.ViewportWidthChanged += SetAdornmentLocation;
            LogoAdornment.VisibilityChanged += ToggleVisibility;

            if (_adornmentLayer.IsEmpty)
                _adornmentLayer.AddAdornment(AdornmentPositioningBehavior.ViewportRelative, null, null, _adornment, null);
        }

        private void ToggleVisibility(object sender, bool isVisible)
        {
            _adornment.Opacity = isVisible ? _initOpacity : 0;
            _currentOpacity = _adornment.Opacity;
        }

        private void CreateImage()
        {
            _adornment = new TextBlock();
            _adornment.Text = Resources.Text.AdornmentGenerated;
            _adornment.FontSize = 75;
            _adornment.FontWeight = FontWeights.Bold;
            _adornment.Foreground = Brushes.Gray;
            _adornment.ToolTip = Resources.Text.AdornmentTooltip;
            _adornment.Opacity = _currentOpacity;
            _adornment.SetValue(TextOptions.TextRenderingModeProperty, TextRenderingMode.Aliased);
            _adornment.SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Ideal);

            _adornment.MouseEnter += (s, e) => { _adornment.Opacity = 1; };
            _adornment.MouseLeave += (s, e) => { _adornment.Opacity = _currentOpacity; };
            _adornment.MouseLeftButtonUp += (s, e) => { LogoAdornment.OnVisibilityChanged(_currentOpacity == 0); };
        }

        private void SetAdornmentLocation(object sender, EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;
            Canvas.SetLeft(_adornment, view.ViewportRight - 380);
            Canvas.SetTop(_adornment, view.ViewportBottom - 100);
        }
    }
}
