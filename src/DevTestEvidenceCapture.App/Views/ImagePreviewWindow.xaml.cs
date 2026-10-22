using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace DevTestEvidenceCapture.App.Views;

public partial class ImagePreviewWindow : Window
{
    public ImagePreviewWindow(string imagePath)
    {
        InitializeComponent();
        PreviewImage.Source = LoadFullImage(imagePath);
    }

    private static BitmapImage LoadFullImage(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(path);
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
