using System.Text.RegularExpressions;
using AtsResumeOptimizer.Models;

namespace AtsResumeOptimizer.Services;

public sealed class ResumeAnalyzer : IResumeAnalyzer
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "being", "by", "for", "from", "has", "he", "in", "is", "it", "its", "of", "on", "that", "the", "to", "was", "were", "will", "with", "you", "your", "or", "our", "we", "this", "these", "those", "their", "they", "them", "into", "about", "over", "under", "across", "ability", "experience", "years", "year", "don", "doesn", "didn", "can", "could", "should", "would", "must", "expect"
    };

    private static readonly HashSet<string> NoiseKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "work", "team", "teams", "role", "responsible", "responsibilities", "company", "candidate", "position", "apply", "application", "job", "code", "coding", "strong", "excellent", "good", "great", "ability", "detail", "preferred", "required", "requirements", "qualification", "qualifications", "communication", "collaboration", "hands-on", "must-have", "musthave", "data-first", "data-oriented", "trade-offs", "tradeoff", "tradeoffs", "real-time"
    };

    private static readonly HashSet<string> AcronymKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ai", "llm", "ml", "nlp", "sql", "api", "etl", "kpi", "okr", "aws", "gcp", "ui", "ux", "qa", "sso", "oauth", "jwt", "ci", "cd"
    };

    private static readonly Dictionary<string, string> CanonicalAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["apis"] = "api",
        ["node"] = "node.js",
        ["nodejs"] = "node.js",
        ["node js"] = "node.js",
        ["ai-powered"] = "ai",
        ["ai powered"] = "ai",
        ["full stack"] = "full-stack",
        ["a/b test"] = "a/b testing",
        ["a/b tests"] = "a/b testing"
    };

    private static readonly HashSet<string> SkillLikeTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "sql", "python", "tableau", "power bi", "excel", "java", "c#", ".net", "asp.net", "entity framework", "linq", "aws", "azure", "gcp", "etl", "pipeline", "pipelines", "salesforce", "hubspot", "jira", "agile", "scrum", "figma", "postgresql", "mysql", "nosql", "mongodb", "snowflake", "bigquery", "looker", "tensorflow", "pytorch", "spss", "sas", "data storytelling", "stakeholder communication", "cross-functional collaboration", "customer lifecycle", "project management", "product management", "financial modeling", "forecasting", "a/b testing", "seo", "sem", "html", "css", "javascript", "typescript", "react", "angular", "vue", "node", "node.js", "api", "apis", "rest", "graphql", "microservices", "kubernetes", "docker", "ci/cd", "github", "git", "devops", "powerpoint", "excel modeling", "full-stack", "end-to-end", "ai", "llm", "ai/llm"
    };

    public AnalysisResult Analyze(AnalysisRequest request)
    {
        var normalizedTargetTitle = Normalize(request.TargetTitle);
        var normalizedResume = Normalize(request.ResumeText);

        var extractedKeywords = ExtractKeywords(request.JobDescription);

        var matchedKeywords = extractedKeywords
            .Where(keyword => IsKeywordPresent(normalizedResume, keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .Take(30)
            .ToList();

        var missingKeywords = extractedKeywords
            .Where(keyword => !IsKeywordPresent(normalizedResume, keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .Take(20)
            .ToList();

        var titleExactMatch = !string.IsNullOrWhiteSpace(normalizedTargetTitle) && ContainsPhrase(normalizedResume, normalizedTargetTitle);
        var hardSkillCount = EstimateHardSkillCount(request.ResumeText);

        var priorityFixes = BuildPriorityFixes(titleExactMatch, request.IsPdfSelectableText, hardSkillCount, missingKeywords.Count);
        var status = DetermineStatus(titleExactMatch, request.IsPdfSelectableText, hardSkillCount, missingKeywords.Count);

        return new AnalysisResult
        {
            VisibilityStatus = status,
            TitleExactMatch = titleExactMatch,
            ResumeLikelyReadable = request.IsPdfSelectableText,
            HardSkillCountInResume = hardSkillCount,
            MatchedKeywords = matchedKeywords,
            MissingKeywords = missingKeywords,
            PriorityFixes = priorityFixes,
            SuggestedSkillsSection = BuildSuggestedSkills(matchedKeywords, missingKeywords),
            RecommendedHeadline = string.IsNullOrWhiteSpace(request.TargetTitle)
                ? "[Paste exact target title from the job posting]"
                : request.TargetTitle.Trim(),
            RecommendedSummary = BuildSummary(request.TargetTitle, matchedKeywords, missingKeywords)
        };
    }

    private static string DetermineStatus(bool titleExactMatch, bool pdfReadable, int hardSkillCount, int missingCount)
    {
        if (!pdfReadable || !titleExactMatch || hardSkillCount < 10 || missingCount > 10)
        {
            return "At Risk";
        }

        return "Likely Visible";
    }

    private static List<string> BuildPriorityFixes(bool titleExactMatch, bool pdfReadable, int hardSkillCount, int missingCount)
    {
        var fixes = new List<string>();

        if (!pdfReadable)
        {
            fixes.Add("Export your resume as selectable-text PDF. If text cannot be highlighted, many ATS systems cannot parse it.");
        }

        if (!titleExactMatch)
        {
            fixes.Add("Use the exact target title from the job posting at the top of your resume.");
        }

        if (hardSkillCount < 10)
        {
            fixes.Add("Expand your skills section to at least 10 role-specific hard skills.");
        }

        if (missingCount > 0)
        {
            fixes.Add("Mirror exact language from the job post in your summary, skills section, and experience bullets.");
        }

        if (fixes.Count == 0)
        {
            fixes.Add("Your core ATS fundamentals look strong. Keep applying quickly and consistently.");
        }

        return fixes;
    }

    private static string BuildSummary(string targetTitle, IReadOnlyCollection<string> matched, IReadOnlyCollection<string> missing)
    {
        var topSkills = matched.Take(4).ToList();

        if (topSkills.Count < 4)
        {
            topSkills.AddRange(missing.Take(4 - topSkills.Count));
        }

        var title = string.IsNullOrWhiteSpace(targetTitle) ? "Target Role" : targetTitle.Trim();
        var skillSegment = topSkills.Count == 0 ? "Role-relevant hard skills" : string.Join(" | ", topSkills);

        return $"{title} - {skillSegment} - Results-focused execution with exact keyword alignment to the job posting.";
    }

    private static List<string> BuildSuggestedSkills(List<string> matched, List<string> missing)
    {
        var skills = matched
            .Concat(missing)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();

        if (skills.Count < 10)
        {
            skills.AddRange(new[]
            {
                "Project management", "Stakeholder communication", "Cross-functional collaboration", "Data analysis", "Process improvement"
            }.Where(x => skills.Count < 20 && !skills.Contains(x, StringComparer.OrdinalIgnoreCase)));
        }

        return skills;
    }

    private static List<string> ExtractKeywords(string jobDescription)
    {
        var knownTerms = SkillLikeTerms
            .Where(term => Regex.IsMatch(jobDescription, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase))
            .Select(Canonicalize)
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .ToList();

        var tokenCandidates = Regex.Matches(jobDescription, "[A-Za-z][A-Za-z0-9+#./-]*")
            .Select(m => m.Value)
            .Select(Canonicalize)
            .Where(IsMeaningfulToken)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return knownTerms
            .Concat(tokenCandidates)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(40)
            .ToList();
    }

    private static string Canonicalize(string token)
    {
        var clean = token.Trim().Trim('.', ',', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\'');
        clean = Regex.Replace(clean, "\\s+", " ");

        if (CanonicalAliases.TryGetValue(clean, out var alias))
        {
            return alias;
        }

        return clean;
    }

    private static bool IsMeaningfulToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length < 2)
        {
            return false;
        }

        if (StopWords.Contains(token) || NoiseKeywords.Contains(token))
        {
            return false;
        }

        if (SkillLikeTerms.Contains(token))
        {
            return true;
        }

        if (token.Any(char.IsDigit))
        {
            return true;
        }

        var hasTechnicalSeparator = token.IndexOfAny(new[] { '+', '#', '.', '/', '-' }) >= 0;
        if (hasTechnicalSeparator)
        {
            var parts = token
                .Split(new[] { '+', '#', '.', '/', '-' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(part => part.Length >= 2)
                .ToList();

            return parts.Any(part => SkillLikeTerms.Contains(part) || AcronymKeywords.Contains(part));
        }

        return AcronymKeywords.Contains(token);
    }

    private static int EstimateHardSkillCount(string resumeText)
    {
        var explicitSkillsMatch = Regex.Match(
            resumeText,
            @"(?is)(skills?|technical skills?)\s*[:\-]\s*(?<body>.+?)(\n\s*\n|experience|education|projects|certifications|$)");

        if (explicitSkillsMatch.Success)
        {
            var body = explicitSkillsMatch.Groups["body"].Value;
            var parsed = body.Split(new[] { ',', '|', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var cleanCount = parsed.Count(item => item.Length >= 2 && item.Length <= 40);
            return cleanCount;
        }

        var inferredMatches = SkillLikeTerms.Count(term => Regex.IsMatch(resumeText, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase));
        return inferredMatches;
    }

    private static bool IsKeywordPresent(string normalizedResume, string keyword)
    {
        var normalizedKeyword = Normalize(keyword);

        if (normalizedKeyword == "api")
        {
            return Regex.IsMatch(normalizedResume, @"\bapi(s)?\b", RegexOptions.IgnoreCase);
        }

        if (normalizedKeyword == "node.js")
        {
            return Regex.IsMatch(normalizedResume, @"\bnode(\.js|\sjs)?\b", RegexOptions.IgnoreCase);
        }

        if (normalizedKeyword == "ai/llm")
        {
            return Regex.IsMatch(normalizedResume, @"\b(ai|llm)\b", RegexOptions.IgnoreCase);
        }

        return ContainsPhrase(normalizedResume, normalizedKeyword);
    }

    private static bool ContainsPhrase(string haystack, string phrase)
    {
        if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(phrase))
        {
            return false;
        }

        return haystack.Contains(phrase, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string input)
    {
        return Regex.Replace(input ?? string.Empty, "\\s+", " ").Trim().ToLowerInvariant();
    }
}
