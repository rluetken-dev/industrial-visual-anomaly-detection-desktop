namespace IndustrialVisualAnomalyDetection.Desktop.Configuration;

public sealed class BackendOptions
{
    public const string SectionName = "Backend";
    public string BaseAddress { get; set; } = "https://localhost:7056";
    public int TimeoutSeconds { get; set; } = 30;
}
