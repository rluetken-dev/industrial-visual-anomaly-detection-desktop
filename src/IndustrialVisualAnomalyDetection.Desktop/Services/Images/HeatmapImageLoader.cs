using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Images;

public sealed class HeatmapImageLoader : IHeatmapImageLoader
{
    public ImageSource Load(AnalysisHeatmap heatmap)
    {
        ArgumentNullException.ThrowIfNull(heatmap);

        byte[] imageBytes = Convert.FromBase64String(heatmap.DataBase64);

        using MemoryStream imageStream = new(imageBytes, writable: false);

        BitmapImage bitmap = new();

        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = imageStream;
        bitmap.EndInit();
        bitmap.Freeze();

        return bitmap;
    }
}
