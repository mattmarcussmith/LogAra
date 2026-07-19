using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LogAra.Application.Abstractions;
using LogAra.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LogAra.Infrastructure.Explanations
{
    public sealed class PollinationsExplanationProvider(HttpClient httpClient, IOptions<AiOptions> aiOptions, ILogger<PollinationsExplanationProvider> logger) : IExplanationProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task<string> GetExplanationAsync(string fingerprint, LogEntry representativeEntry, int occurrences, CancellationToken cancellationToken)
        {
            var options = aiOptions.Value;
            if (!options.Enabled)
            {
                throw new InvalidOperationException("AI explanations are disabled.");
            }

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, options.TimeoutSeconds)));

            var requestUri = BuildRequestUri(options);
            var prompt = BuildPrompt(fingerprint, representativeEntry, occurrences);
            var model = string.IsNullOrWhiteSpace(options.Model) ? "openai-fast" : options.Model.Trim();

            var payload = new
            {
                model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a concise software log triage assistant. Return only the requested sections."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.2,
                max_tokens = Math.Clamp(options.MaxOutputTokens, 200, 1024),
                stream = false
            };

            using var response = await httpClient.PostAsJsonAsync(requestUri, payload, JsonOptions, linkedCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(linkedCts.Token);
                logger.LogWarning("Pollinations returned status {StatusCode}. Body: {Body}", (int)response.StatusCode, body);
                throw new HttpRequestException($"Pollinations returned {(int)response.StatusCode} ({response.StatusCode}).", null, response.StatusCode);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: linkedCts.Token);
            var text = ExtractText(document.RootElement);

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new InvalidOperationException("Pollinations returned an empty explanation.");
            }

            return text.Trim();
        }

        private static string BuildRequestUri(AiOptions options)
        {
            var endpoint = string.IsNullOrWhiteSpace(options.Endpoint) ? "https://text.pollinations.ai/openai" : options.Endpoint.TrimEnd('/');
            return endpoint.EndsWith("/openai", StringComparison.OrdinalIgnoreCase) ? endpoint : $"{endpoint}/openai";
        }

        private static string BuildPrompt(string fingerprint, LogEntry representativeEntry, int occurrences)
        {
            var message = representativeEntry.Message.Replace("\n", " ", StringComparison.Ordinal).Trim();
            var source = representativeEntry.Source.Trim();

            var builder = new StringBuilder();
            builder.AppendLine("Analyze this software log issue for a public product demo.");
            builder.AppendLine("Return concise output in plain text using exactly these sections and order:");
            builder.AppendLine("Summary:");
            builder.AppendLine("Possible Causes:");
            builder.AppendLine("- bullet");
            builder.AppendLine("Recommended Next Steps:");
            builder.AppendLine("- bullet");
            builder.AppendLine();
            builder.AppendLine($"Severity: {representativeEntry.Severity}");
            builder.AppendLine($"Occurrences: {occurrences}");
            builder.AppendLine($"Source: {source}");
            builder.AppendLine($"Fingerprint: {fingerprint}");
            builder.AppendLine($"Representative message: {message}");
            builder.AppendLine();
            builder.AppendLine("Keep summary to 2-3 sentences and provide 3-5 bullets for each list.");

            return builder.ToString();
        }

        private static string ExtractText(JsonElement root)
        {
            if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array)
            {
                return string.Empty;
            }

            foreach (var choice in choices.EnumerateArray())
            {
                if (!choice.TryGetProperty("message", out var message))
                {
                    continue;
                }

                if (message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
                {
                    return content.GetString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
    }
}