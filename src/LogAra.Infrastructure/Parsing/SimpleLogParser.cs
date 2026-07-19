using System.Text.RegularExpressions;
using LogAra.Application.Abstractions;
using LogAra.Domain;

namespace LogAra.Infrastructure.Parsing
{
    public sealed class SimpleLogParser : ILogParser
    {
        private static readonly Regex HeaderRegex = new(
            "^(?:\\[(?<timestamp>[^\\]]+)\\]|(?<timestamp>\\d{4}-\\d{2}-\\d{2}[T\\s][^\\s]+))?\\s*(?:\\[(?<severity>[A-Za-z]+)\\]|(?<severity>TRACE|DEBUG|INFO|INFORMATION|WARN|WARNING|ERROR|CRITICAL|FATAL))?\\s*(?<body>.*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public async Task<IReadOnlyList<LogEntry>> ParseAsync(Stream stream, CancellationToken cancellationToken)
        {
            using var reader = new StreamReader(stream);
            var entries = new List<LogEntry>();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                entries.Add(ParseLine(line));
            }

            return entries;
        }

        private static LogEntry ParseLine(string line)
        {
            var match = HeaderRegex.Match(line);
            if (!match.Success)
            {
                return new LogEntry(null, InferSeverity(line), "unknown", line.Trim(), line);
            }

            var timestamp = ParseTimestamp(match.Groups["timestamp"].Value);
            var rawSeverity = match.Groups["severity"].Value;
            var severity = ParseSeverity(rawSeverity, line);
            var (source, parsedMessage) = ParseSourceAndMessage(match.Groups["body"].Value, !string.IsNullOrWhiteSpace(rawSeverity));
            var message = string.IsNullOrWhiteSpace(parsedMessage)
                ? line.Trim()
                : parsedMessage;

            return new LogEntry(timestamp, severity, source, message, line);
        }

        private static DateTimeOffset? ParseTimestamp(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return DateTimeOffset.TryParse(value, out var parsed) ? parsed.ToUniversalTime() : null;
        }

        private static LogSeverity ParseSeverity(string rawSeverity, string line)
        {
            if (!string.IsNullOrWhiteSpace(rawSeverity))
            {
                return rawSeverity.ToUpperInvariant() switch
                {
                    "TRACE" => LogSeverity.Trace,
                    "DEBUG" => LogSeverity.Debug,
                    "INFO" => LogSeverity.Information,
                    "INFORMATION" => LogSeverity.Information,
                    "WARN" => LogSeverity.Warning,
                    "WARNING" => LogSeverity.Warning,
                    "ERROR" => LogSeverity.Error,
                    "CRITICAL" => LogSeverity.Critical,
                    "FATAL" => LogSeverity.Critical,
                    _ => InferSeverity(line)
                };
            }

            return InferSeverity(line);
        }

        private static (string Source, string Message) ParseSourceAndMessage(string body, bool hasStructuredSeverity)
        {
            var trimmed = body.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                return ("unknown", string.Empty);
            }

            if (hasStructuredSeverity)
            {
                var firstSpaceIndex = trimmed.IndexOf(' ', StringComparison.Ordinal);
                if (firstSpaceIndex > 0)
                {
                    return (trimmed[..firstSpaceIndex].Trim().Trim('[', ']'), trimmed[(firstSpaceIndex + 1)..].Trim());
                }
            }

            var colonIndex = trimmed.IndexOf(':', StringComparison.Ordinal);
            if (colonIndex > 0)
            {
                var candidateSource = trimmed[..colonIndex].Trim().Trim('[', ']');
                var message = trimmed[(colonIndex + 1)..].Trim();
                return (string.IsNullOrWhiteSpace(candidateSource) ? "unknown" : candidateSource, message);
            }

            return ("unknown", trimmed);
        }

        private static LogSeverity InferSeverity(string line)
        {
            var lowered = line.ToLowerInvariant();
            if (lowered.Contains("critical") || lowered.Contains("fatal"))
            {
                return LogSeverity.Critical;
            }

            if (lowered.Contains("error") || lowered.Contains("exception") || lowered.Contains("fail"))
            {
                return LogSeverity.Error;
            }

            if (lowered.Contains("warn"))
            {
                return LogSeverity.Warning;
            }

            if (lowered.Contains("debug"))
            {
                return LogSeverity.Debug;
            }

            if (lowered.Contains("trace"))
            {
                return LogSeverity.Trace;
            }

            return LogSeverity.Information;
        }
    }
}
