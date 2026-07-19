using LogAra.Client.Models;
using LogAra.Client.Services;
using LogAra.Contracts.Requests.Analysis;
using LogAra.Contracts.Responses.Analysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace LogAra.Client.Pages
{
    public partial class Home
    {
        [Inject]
        private AnalysisApiClient AnalysisApiClient { get; set; } = default!;

        [Inject]
        private BrowserStorageService BrowserStorageService { get; set; } = default!;

        [Inject]
        private HttpClient HttpClient { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private IJSRuntime JsRuntime { get; set; } = default!;

        private IBrowserFile? SelectedFile;
        private string? SelectedFileName;
        private bool IsBusy;
        private bool IsExplaining;
        private bool IsTypingExplanation;
        private string? ErrorMessage;
        private string Theme = "light";
        private bool IsHelpModalOpen;
        private bool IsRawEntryModalOpen;
        private bool IsStateReady;
        private AnalysisResponseDto? CurrentResult;
        private IssueDto? SelectedIssue;
        private IssueExplanationDto? SelectedExplanation;
        private string? DisplayedExplanationText;
        private string? CurrentHistoryId;
        private List<AnalysisHistoryItem> History = [];
        private Dictionary<string, string> ExplanationCache = new(StringComparer.Ordinal);
        private CancellationTokenSource? ExplanationTypingCts;

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var state = await BrowserStorageService.LoadStateAsync();
                if (state is not null)
                {
                    Theme = string.IsNullOrWhiteSpace(state.Theme) ? "light" : state.Theme;
                    History = state.History.OrderByDescending(x => x.AnalyzedAtUtc).ToList();
                    ExplanationCache = new Dictionary<string, string>(state.ExplanationCache, StringComparer.Ordinal);

                    if (History.Count > 0)
                    {
                        CurrentResult = History[0].Result;
                        SelectedIssue = CurrentResult.Issues.FirstOrDefault();
                        SelectedExplanation = BuildExplanationDto(SelectedIssue?.Explanation);
                        DisplayedExplanationText = SelectedExplanation?.RawText;
                        CurrentHistoryId = History[0].Id;
                    }
                }

                await ApplyThemeAsync();
            }
            finally
            {
                IsStateReady = true;
            }
        }

        private void OnFileSelected(InputFileChangeEventArgs args)
        {
            SelectedFile = args.File;
            SelectedFileName = args.File.Name;
            ErrorMessage = null;
        }

        private async Task AnalyzeAsync()
        {
            if (SelectedFile is null)
            {
                ErrorMessage = "Select a log file to analyze.";
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var result = await AnalysisApiClient.AnalyzeAsync(SelectedFile, CancellationToken.None);
                await ApplyAnalysisResultAsync(result, SelectedFile.Name);
            }
            catch (HttpRequestException)
            {
                var apiEndpoint = new Uri(HttpClient.BaseAddress ?? new Uri(NavigationManager.BaseUri), "api/analysis/upload");
                ErrorMessage = $"Analysis failed. Ensure LogAra.Api is running at {apiEndpoint}.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AnalyzeSampleAsync(string samplePath, string fileName)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var sampleUri = new Uri(new Uri(NavigationManager.BaseUri), samplePath);
                await using var sampleStream = await HttpClient.GetStreamAsync(sampleUri, CancellationToken.None);
                var result = await AnalysisApiClient.AnalyzeAsync(sampleStream, fileName, "text/plain", CancellationToken.None);
                SelectedFile = null;
                SelectedFileName = fileName;
                await ApplyAnalysisResultAsync(result, fileName);
            }
            catch (HttpRequestException)
            {
                var apiEndpoint = new Uri(HttpClient.BaseAddress ?? new Uri(NavigationManager.BaseUri), "api/analysis/upload");
                ErrorMessage = $"Sample analysis failed. Ensure LogAra.Api is running at {apiEndpoint}.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ApplyAnalysisResultAsync(AnalysisResponseDto result, string fileName)
        {
            foreach (var issue in result.Issues)
            {
                if (!string.IsNullOrWhiteSpace(issue.Explanation))
                {
                    ExplanationCache[issue.Fingerprint] = issue.Explanation;
                }
                else if (ExplanationCache.TryGetValue(issue.Fingerprint, out var cachedExplanation))
                {
                    issue.Explanation = cachedExplanation;
                }
            }

            CurrentResult = result;
            SelectedIssue = result.Issues.FirstOrDefault();
            SelectedExplanation = BuildExplanationDto(SelectedIssue?.Explanation);
            DisplayedExplanationText = SelectedExplanation?.RawText;

            var historyItem = new AnalysisHistoryItem
            {
                Id = Guid.NewGuid().ToString("n"),
                FileName = fileName,
                AnalyzedAtUtc = DateTimeOffset.UtcNow,
                Result = result
            };

            History.Insert(0, historyItem);
            CurrentHistoryId = historyItem.Id;

            if (History.Count > 30)
            {
                History = History.Take(30).ToList();
            }

            await PersistStateAsync();
        }

        private void SelectIssue(IssueDto issue)
        {
            CancelExplanationTyping();
            SelectedIssue = issue;
            SelectedExplanation = BuildExplanationDto(issue.Explanation);
            DisplayedExplanationText = SelectedExplanation?.RawText;
        }

        private async Task RestoreHistory(AnalysisHistoryItem item)
        {
            CurrentResult = item.Result;
            foreach (var issue in CurrentResult.Issues)
            {
                if (string.IsNullOrWhiteSpace(issue.Explanation) && ExplanationCache.TryGetValue(issue.Fingerprint, out var cachedExplanation))
                {
                    issue.Explanation = cachedExplanation;
                }
            }

            SelectedIssue = CurrentResult.Issues.FirstOrDefault();
            SelectedExplanation = BuildExplanationDto(SelectedIssue?.Explanation);
            DisplayedExplanationText = SelectedExplanation?.RawText;
            CurrentHistoryId = item.Id;
            await InvokeAsync(StateHasChanged);
        }

        private async Task ToggleThemeAsync()
        {
            Theme = Theme == "dark" ? "light" : "dark";
            await ApplyThemeAsync();
            await PersistStateAsync();
        }

        private async Task ClearHistoryAsync()
        {
            History.Clear();
            CurrentResult = null;
            SelectedIssue = null;
            SelectedExplanation = null;
            DisplayedExplanationText = null;
            CurrentHistoryId = null;
            await PersistStateAsync();
        }

        private async Task ExplainSelectedIssueAsync()
        {
            if (SelectedIssue is null || IsExplaining)
            {
                return;
            }

            IsExplaining = true;
            IsTypingExplanation = false;
            DisplayedExplanationText = null;
            ErrorMessage = null;
            CancelExplanationTyping();

            try
            {
                var explanation = await AnalysisApiClient.ExplainIssueAsync(new ExplainIssueRequestDto
                {
                    Fingerprint = SelectedIssue.Fingerprint,
                    Severity = SelectedIssue.Severity,
                    Source = SelectedIssue.RepresentativeEntry.Source,
                    Message = SelectedIssue.RepresentativeEntry.Message,
                    Occurrences = SelectedIssue.Occurrences
                }, CancellationToken.None);

                SelectedIssue.Explanation = explanation.RawText;
                SelectedExplanation = explanation;
                ExplanationCache[SelectedIssue.Fingerprint] = explanation.RawText;
                await PersistStateAsync();

                IsExplaining = false;
                await TypeExplanationAsync(explanation.RawText);
            }
            catch (HttpRequestException)
            {
                ErrorMessage = "AI explanation service is unavailable. Try again shortly.";
            }
            finally
            {
                IsExplaining = false;
            }
        }

        private async Task TypeExplanationAsync(string rawText)
        {
            CancelExplanationTyping();
            ExplanationTypingCts = new CancellationTokenSource();
            var cancellationToken = ExplanationTypingCts.Token;

            IsTypingExplanation = true;
            DisplayedExplanationText = string.Empty;
            await InvokeAsync(StateHasChanged);

            try
            {
                for (var index = 0; index < rawText.Length; index += 6)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var length = Math.Min(6, rawText.Length - index);
                    DisplayedExplanationText += rawText.Substring(index, length);
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(4, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsTypingExplanation = false;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }

        private void CancelExplanationTyping()
        {
            if (ExplanationTypingCts is null)
            {
                return;
            }

            ExplanationTypingCts.Cancel();
            ExplanationTypingCts.Dispose();
            ExplanationTypingCts = null;
            IsTypingExplanation = false;
        }

        private void OpenRawEntryModal()
        {
            if (SelectedIssue is null)
            {
                return;
            }

            IsRawEntryModalOpen = true;
        }

        private void CloseRawEntryModal()
        {
            IsRawEntryModalOpen = false;
        }

        private async Task PersistStateAsync()
        {
            await BrowserStorageService.SaveStateAsync(new ClientStateSnapshot
            {
                Theme = Theme,
                History = History,
                ExplanationCache = ExplanationCache
            });
        }

        private async Task ApplyThemeAsync()
        {
            await JsRuntime.InvokeVoidAsync("logaraTheme.apply", Theme);
        }

        private static string GetSeverityCss(string severity)
        {
            return severity.ToLowerInvariant() switch
            {
                "critical" => "sev-critical",
                "error" => "sev-error",
                "warning" => "sev-warning",
                _ => "sev-info"
            };
        }

        private static string FormatTimestamp(DateTimeOffset? timestamp)
        {
            return timestamp.HasValue ? timestamp.Value.ToLocalTime().ToString("g") : "n/a";
        }

        private MetricDelta? GetErrorDelta()
        {
            var previous = GetPreviousResult();
            if (CurrentResult is null || previous is null)
            {
                return null;
            }

            return BuildDelta(CurrentResult.ErrorCount, previous.ErrorCount);
        }

        private MetricDelta? GetWarningDelta()
        {
            var previous = GetPreviousResult();
            if (CurrentResult is null || previous is null)
            {
                return null;
            }

            return BuildDelta(CurrentResult.WarningCount, previous.WarningCount);
        }

        private AnalysisResponseDto? GetPreviousResult()
        {
            if (History.Count < 2 || string.IsNullOrWhiteSpace(CurrentHistoryId))
            {
                return null;
            }

            var currentIndex = History.FindIndex(x => x.Id == CurrentHistoryId);
            if (currentIndex < 0 || currentIndex + 1 >= History.Count)
            {
                return null;
            }

            return History[currentIndex + 1].Result;
        }

        private static MetricDelta BuildDelta(int current, int previous)
        {
            if (previous <= 0)
            {
                var percentage = current == 0 ? 0 : 100;
                return new MetricDelta(current - previous, percentage);
            }

            var percentChange = (int)Math.Round((current - previous) * 100d / previous, MidpointRounding.AwayFromZero);
            return new MetricDelta(current - previous, percentChange);
        }

        private static string FormatDeltaText(MetricDelta? delta)
        {
            if (!delta.HasValue)
            {
                return "-";
            }

            var value = delta.Value;
            if (value.Difference == 0)
            {
                return "No change vs last log";
            }

            var arrow = value.Difference > 0 ? "↑" : "↓";
            return $"{arrow} {Math.Abs(value.Percentage)}% vs last log";
        }

        private static string GetDeltaClass(MetricDelta? delta)
        {
            if (!delta.HasValue)
            {
                return "delta-neutral";
            }

            var value = delta.Value;
            if (value.Difference == 0)
            {
                return "delta-neutral";
            }

            return value.Difference > 0 ? "delta-up" : "delta-down";
        }

        private static IssueExplanationDto? BuildExplanationDto(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var normalized = raw.Trim();
            var lines = normalized
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var summary = string.Empty;
            var causes = new List<string>();
            var steps = new List<string>();
            var section = string.Empty;

            foreach (var line in lines)
            {
                if (line.StartsWith("Summary:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "summary";
                    var value = line["Summary:".Length..].Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        summary = value;
                    }

                    continue;
                }

                if (line.StartsWith("Possible Causes:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "causes";
                    continue;
                }

                if (line.StartsWith("Recommended Next Steps:", StringComparison.OrdinalIgnoreCase))
                {
                    section = "steps";
                    continue;
                }

                if (section == "summary")
                {
                    summary = string.IsNullOrWhiteSpace(summary) ? line : $"{summary} {line}";
                    continue;
                }

                if (line.StartsWith("-", StringComparison.Ordinal))
                {
                    var item = line[1..].Trim();
                    if (string.IsNullOrWhiteSpace(item))
                    {
                        continue;
                    }

                    if (section == "causes")
                    {
                        causes.Add(item);
                    }
                    else if (section == "steps")
                    {
                        steps.Add(item);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                summary = normalized;
            }

            return new IssueExplanationDto
            {
                Summary = summary,
                PossibleCauses = causes,
                RecommendedNextSteps = steps,
                RawText = normalized
            };
        }

        private void ShowHelpModal()
        {
            IsHelpModalOpen = true;
        }

        private void CloseHelpModal()
        {
            IsHelpModalOpen = false;
        }

        private readonly record struct MetricDelta(int Difference, int Percentage);
    }
}
