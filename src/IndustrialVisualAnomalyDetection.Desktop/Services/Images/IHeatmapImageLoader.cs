using System.Windows.Media;
using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;

namespace IndustrialVisualAnomalyDetection.Desktop.Services.Images;

public interface IHeatmapImageLoader
{
    ImageSource Load(AnalysisHeatmap heatmap);
}
