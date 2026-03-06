using AtsResumeOptimizer.Models;
using AtsResumeOptimizer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AtsResumeOptimizer.Controllers;

public sealed class HomeController : Controller
{
    private readonly IResumeAnalyzer _resumeAnalyzer;

    public HomeController(IResumeAnalyzer resumeAnalyzer)
    {
        _resumeAnalyzer = resumeAnalyzer;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("api/analyze")]
    public IActionResult Analyze([FromBody] AnalysisRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { error = "Invalid request payload." });
        }

        if (string.IsNullOrWhiteSpace(request.JobDescription) || string.IsNullOrWhiteSpace(request.ResumeText))
        {
            return BadRequest(new { error = "Job description and resume text are required." });
        }

        var result = _resumeAnalyzer.Analyze(request);
        return Ok(result);
    }
}
