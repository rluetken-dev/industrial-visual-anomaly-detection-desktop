using System.Windows.Media;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Files;

public interface IImagePreviewLoader
{
    ImageSource Load(string imagePath);
}
