namespace IndustrialVisualAnomalyDetection.Desktop.Models.Analysis;

public sealed class ImageAnalysisResult
{
    public ImageAnalysisResult(
        string modelId,
        string category,
        double score,
        double threshold,
        AnalysisDecision decision,
        long processingTimeMs,
        string traceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);

        if (!double.IsFinite(score) || score < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(score));
        }

        if (!double.IsFinite(threshold) || threshold < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold));
        }

        if (processingTimeMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processingTimeMs));
        }

        ModelId = modelId;
        Category = category;
        Score = score;
        Threshold = threshold;
        Decision = decision;
        ProcessingTimeMs = processingTimeMs;
        TraceId = traceId;
    }

    public string ModelId { get; }

    public string Category { get; }

    public double Score { get; }

    public double Threshold { get; }

    public AnalysisDecision Decision { get; }

    public long ProcessingTimeMs { get; }

    public string TraceId { get; }
}
