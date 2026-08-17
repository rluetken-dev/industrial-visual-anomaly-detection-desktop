using Microsoft.Win32;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Files;

public sealed class ImageFilePicker : IImageFilePicker
{
    public string? SelectImage()
    {
        OpenFileDialog dialog = new()
        {
            Title = "Select an industrial image",
            Filter = "Supported images (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|PNG images (*.png)|*.png|JPEG images (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
