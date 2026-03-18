using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using EasyMeeting.UI.Controls;
using EasyMeeting.ViewModels;

namespace EasyMeeting.UI.Views;

public partial class PreviewWindow : TransparentWindow
{
    private readonly MainViewModel _viewModel;
    
    public PreviewWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
        Opacity = 0.8;
        OpacitySlider.Value = 80;
    }
    
    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();
        }
    }
    
    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PreviewText != null)
        {
            PreviewText.FontSize = FontSizeSlider.Value;
        }
    }
    
    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (this.IsLoaded)
        {
            Opacity = OpacitySlider.Value / 100.0;
        }
    }
}
