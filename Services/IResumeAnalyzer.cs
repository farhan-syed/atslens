using AtsResumeOptimizer.Models;

namespace AtsResumeOptimizer.Services;

public interface IResumeAnalyzer
{
    AnalysisResult Analyze(AnalysisRequest request);
}
