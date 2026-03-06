# ATSLens (.NET + C#)

A modern ASP.NET Core MVC app that helps job seekers optimize resume visibility for ATS-style recruiter search.

## What it checks
- Exact title match with the job posting
- Resume parse-readiness signal (selectable PDF text confirmation)
- Hard-skill section coverage
- Keyword/phrase matching from job description to resume text
- Binary visibility outcome: `Likely Visible` vs `At Risk`

## Run locally
1. Install .NET 8 SDK: https://dotnet.microsoft.com/download
2. Open terminal in this folder:
   ```powershell
   cd C:\Users\farhan\Documents\Playground\AtsResumeOptimizer
   ```
3. Run:
   ```powershell
   dotnet restore
   dotnet run
   ```
4. Open the localhost URL shown in terminal.

## Notes
- This app intentionally avoids fake ATS percentage scoring.
- It focuses on practical ATS mechanics: exact match and discoverability.
