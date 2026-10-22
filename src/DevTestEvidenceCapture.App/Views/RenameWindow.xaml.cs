using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DevTestEvidenceCapture.App.ViewModels;

namespace DevTestEvidenceCapture.App.Views;

public partial class RenameWindow : Window
{
    public RenameWindow(RenameViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Thumbnail_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ScreenshotRenameRow { Thumbnail: not null } row })
        {
            new ImagePreviewWindow(row.FullFilePath) { Owner = this }.ShowDialog();
        }
    }

    private void MeaningfulNameTextBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox
            && DataContext is RenameViewModel viewModel
            && textBox.DataContext is ScreenshotRenameRow row
            && ReferenceEquals(row, viewModel.Rows.FirstOrDefault()))
        {
            textBox.Focus();
        }
    }
}
