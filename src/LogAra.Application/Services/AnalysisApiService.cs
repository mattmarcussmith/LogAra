using LogAra.Application.Abstractions;
using LogAra.Contracts.Requests.Analysis;
using LogAra.Contracts.Responses.Analysis;
using LogAra.Domain;

namespace LogAra.Application.Services
{
    public sealed class AnalysisApiService(ILogAnalysisService logAnalysisService, IExplanationProvider explanationProvider) : IAnalysisApiService
    {
        public async Task<AnalysisResponseDto> AnalyzeAsync(Stream rawLogStream, CancellationToken cancellationToken)
        {
            var result = await logAnalysisService.AnalyzeAsync(rawLogStream, cancellationToken);
            return MapToDto(result);
        }

        public async Task<IssueExplanationDto> ExplainAsync(ExplainIssueRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Fingerprint) || string.IsNullOrWhiteSpace(request.Message))
            {
                throw new ArgumentException("Fingerprint and message are required.");
            }

            var severity = ParseSeverity(request.Severity);
            var source = string.IsNullOrWhiteSpace(request.Source) ? "unknown" : request.Source.Trim();
            var occurrences = Math.Max(1, request.Occurrences);
            var representative = new LogEntry(null, severity, source, request.Message.Trim(), request.Message.Trim());
            var rawExplanation = await explanationProvider.GetExplanationAsync(request.Fingerprint.Trim(), representative, occurrences, cancellationToken);

            return MapToExplanationDto(rawExplanation);
        }

        private static AnalysisResponseDto MapToDto(AnalysisResult result)
        {
            return new AnalysisResponseDto
            {
                GeneratedAtUtc = result.GeneratedAtUtc,
                TotalEntries = result.TotalEntries,
                ErrorCount = result.ErrorCount,
                WarningCount = result.WarningCount,
                InformationCount = result.InformationCount,
                Issues = result.Issues.Select(issue => new IssueDto
                {
                    Fingerprint = issue.Fingerprint,
                    Severity = issue.Severity.ToString(),
                    Occurrences = issue.Occurrences,
                    FirstSeenUtc = issue.FirstSeenUtc,
                    LastSeenUtc = issue.LastSeenUtc,
                    Explanation = issue.Explanation,
                    RepresentativeEntry = new RepresentativeEntryDto
                    {
                        TimestampUtc = issue.RepresentativeEntry.TimestampUtc,
                        Severity = issue.RepresentativeEntry.Severity.ToString(),
                        Source = issue.RepresentativeEntry.Source,
                        Message = issue.RepresentativeEntry.Message,
                        RawLine = issue.RepresentativeEntry.RawLine
                    }
                }).ToList()
            };
        }

        private static LogSeverity ParseSeverity(string severity)
        {
            if (Enum.TryParse<LogSeverity>(severity, ignoreCase: true, out var parsed))
            {
                return parsed;
            }

            return LogSeverity.Error;
        }

        private static IssueExplanationDto MapToExplanationDto(string rawExplanation)
        {
            var normalized = (rawExplanation ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalized))
            {
                normalized = "No explanation was generated.";
            }

            var lines = normalized
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var summary = string.Empty;
            var possibleCauses = new List<string>();
            var recommendedSteps = new List<string>();
            var section = string.Empty;

            foreach (var line in lines)
            {
                var cleaned = line.Trim();
                if (cleaned.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "summary";
                    var value = cleaned["Summary:".Length..].Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        summary = value;
                    }

                    continue;
                }

                if (cleaned.StartsWith("Possible Causes:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "causes";
                    continue;
                }

                if (cleaned.StartsWith("Recommended Next Steps:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "steps";
                    continue;
                }

                if (section == "summary")
                {
                    summary = string.IsNullOrWhiteSpace(summary)
                        ? cleaned
                        : $"{summary} {cleaned}";
                    continue;
                }

                if (cleaned.StartsWith("-", StringComparison.Ordinal))
                {
                    var item = cleaned[1..].Trim();
                    if (string.IsNullOrWhiteSpace(item))
                    {
                        continue;
                    }

                    if (section == "causes")
                    {
                        possibleCauses.Add(item);
                    }
                    else if (section == "steps")
                    {
                        recommendedSteps.Add(item);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                summary = normalized;
            }

            if (possibleCauses.Count == 0)
            {
                possibleCauses.Add("Inspect surrounding logs for correlated failures and dependency errors.");
            }

            if (recommendedSteps.Count == 0)
            {
                recommendedSteps.Add("Prioritize this issue by severity and frequency, then validate a fix in staging.");
            }

            return new IssueExplanationDto
            {
                Summary = summary,
                PossibleCauses = possibleCauses,
                RecommendedNextSteps = recommendedSteps,
                RawText = normalized
            };
        }
    }
}
