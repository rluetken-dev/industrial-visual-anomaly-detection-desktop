using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Files;

public sealed class ImagePreviewLoader : IImagePreviewLoader
{
    public ImageSource Load(string imagePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagePath);

        BitmapImage bitmap = new();

        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }
}
