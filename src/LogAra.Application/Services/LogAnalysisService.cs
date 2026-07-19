using System.Text.RegularExpressions;
using LogAra.Application.Abstractions;
using LogAra.Domain;

namespace LogAra.Application.Services
{
    public sealed class LogAnalysisService(ILogParser parser) : ILogAnalysisService
    {
        private static readonly Regex TraceIdRegex = new("traceid=[^\\s]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex GuidRegex = new("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-5][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12}", RegexOptions.Compiled);
        private static readonly Regex NumberRegex = new("\\b\\d+\\b", RegexOptions.Compiled);
        private static readonly Regex HexRegex = new("0x[0-9a-fA-F]+", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new("\\s+", RegexOptions.Compiled);

        public async Task<AnalysisResult> AnalyzeAsync(Stream rawLogStream, CancellationToken cancellationToken)
        {
            var entries = await parser.ParseAsync(rawLogStream, cancellationToken);

            var grouped = entries
                .GroupBy(entry => BuildFingerprint(entry))
                .Select(group => new
                {
                    Fingerprint = group.Key,
                    Severity = group.Max(x => x.Severity),
                    Entries = group.OrderByDescending(x => x.TimestampUtc ?? DateTimeOffset.MinValue).ToList()
                })
                .OrderByDescending(group => group.Entries.Count)
                .ThenByDescending(group => group.Severity)
                .ToList();

            var issues = new List<IssueInsight>(grouped.Count);
            foreach (var group in grouped)
            {
                var representative = group.Entries[0];

                issues.Add(new IssueInsight(
                    group.Fingerprint,
                    group.Severity,
                    group.Entries.Count,
                    group.Entries.Min(x => x.TimestampUtc),
                    group.Entries.Max(x => x.TimestampUtc),
                    representative,
                        string.Empty));
            }

            return new AnalysisResult(
                DateTimeOffset.UtcNow,
                entries.Count,
                entries.Count(x => x.Severity >= LogSeverity.Error),
                entries.Count(x => x.Severity == LogSeverity.Warning),
                entries.Count(x => x.Severity == LogSeverity.Information),
                issues);
        }

        private static string BuildFingerprint(LogEntry entry)
        {
            var normalizedMessage = entry.Message.Trim().ToLowerInvariant();
            normalizedMessage = TraceIdRegex.Replace(normalizedMessage, "traceid={id}");
            normalizedMessage = GuidRegex.Replace(normalizedMessage, "{guid}");
            normalizedMessage = HexRegex.Replace(normalizedMessage, "{hex}");
            normalizedMessage = NumberRegex.Replace(normalizedMessage, "{n}");
            normalizedMessage = WhitespaceRegex.Replace(normalizedMessage, " ");

            return $"{entry.Severity}|{entry.Source.ToLowerInvariant()}|{normalizedMessage}";
        }
    }
}
