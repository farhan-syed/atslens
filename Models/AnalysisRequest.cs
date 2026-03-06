namespace AtsResumeOptimizer.Models;

public sealed class AnalysisRequest
{
    public string TargetTitle { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public string ResumeText { get; set; } = string.Empty;
    public bool IsPdfSelectableText { get; set; }
}
