namespace IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;

public sealed record AnalysisHeatmap
{
    public AnalysisHeatmap(string contentType, int width, int height, string dataBase64)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("The heatmap content type is required.", nameof(contentType));
        }

        if (!string.Equals(contentType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The heatmap content type must be image/png.", nameof(contentType));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "The heatmap width must be greater than zero.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "The heatmap height must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(dataBase64))
        {
            throw new ArgumentException("The heatmap data is required.", nameof(dataBase64));
        }

        if (!Convert.TryFromBase64String(dataBase64, new byte[dataBase64.Length], out _))
        {
            throw new ArgumentException("The heatmap data must be valid Base64.", nameof(dataBase64));
        }

        ContentType = contentType;
        Width = width;
        Height = height;
        DataBase64 = dataBase64;
    }

    public string ContentType { get; }

    public int Width { get; }

    public int Height { get; }

    public string DataBase64 { get; }
}
