using System.Windows.Media;
using System.Windows.Media.Imaging;
using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;
using IndustrialVisualAnomalyDetection.Desktop.Services.Images;

namespace IndustrialVisualAnomalyDetection.Desktop.Tests.Unit.Services.Images;

public sealed class HeatmapImageLoaderTests
{
    private const string TransparentPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    [Fact]
    public void NullHeatmapIsRejected()
    {
        HeatmapImageLoader loader = new();

        Assert.Throws<ArgumentNullException>(() => loader.Load(null!));
    }

    [Fact]
    public void PngHeatmapIsLoadedAsFrozenBitmap()
    {
        AnalysisHeatmap heatmap = new(
            "image/png",
            1,
            1,
            TransparentPngBase64);

        HeatmapImageLoader loader = new();

        ImageSource imageSource = loader.Load(heatmap);

        BitmapImage bitmap = Assert.IsType<BitmapImage>(imageSource);
        Assert.Equal(1, bitmap.PixelWidth);
        Assert.Equal(1, bitmap.PixelHeight);
        Assert.True(bitmap.IsFrozen);
    }
}
