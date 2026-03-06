namespace AtsResumeOptimizer.Models;

public sealed class AnalysisResult
{
    public string VisibilityStatus { get; set; } = "At Risk";
    public bool TitleExactMatch { get; set; }
    public bool ResumeLikelyReadable { get; set; }
    public int HardSkillCountInResume { get; set; }
    public List<string> MatchedKeywords { get; set; } = new();
    public List<string> MissingKeywords { get; set; } = new();
    public List<string> PriorityFixes { get; set; } = new();
    public List<string> SuggestedSkillsSection { get; set; } = new();
    public string RecommendedHeadline { get; set; } = string.Empty;
    public string RecommendedSummary { get; set; } = string.Empty;
}
