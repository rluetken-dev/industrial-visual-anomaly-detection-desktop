using IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;

namespace IndustrialVisualAnomalyDetection.Desktop.Tests.Unit.Models.Analysis;

public sealed class AnalysisHeatmapTests
{
    private const string ValidBase64 = "AQID";

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingContentTypeIsRejected(string contentType)
    {
        Assert.Throws<ArgumentException>(() =>
            new AnalysisHeatmap(contentType, 320, 320, ValidBase64));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidWidthIsRejected(int width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnalysisHeatmap("image/png", width, 320, ValidBase64));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidHeightIsRejected(int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AnalysisHeatmap("image/png", 320, height, ValidBase64));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingDataIsRejected(string dataBase64)
    {
        Assert.Throws<ArgumentException>(() =>
            new AnalysisHeatmap("image/png", 320, 320, dataBase64));
    }

    [Fact]
    public void InvalidBase64IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new AnalysisHeatmap("image/png", 320, 320, "not-base64"));
    }

    [Fact]
    public void ValidHeatmapPreservesValues()
    {
        AnalysisHeatmap heatmap = new("image/png", 320, 320, ValidBase64);

        Assert.Equal("image/png", heatmap.ContentType);
        Assert.Equal(320, heatmap.Width);
        Assert.Equal(320, heatmap.Height);
        Assert.Equal(ValidBase64, heatmap.DataBase64);
    }

    [Fact]
    public void UnsupportedContentTypeIsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new AnalysisHeatmap("image/jpeg", 320, 320, ValidBase64));
    }
}
