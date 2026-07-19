using System.Net.Http.Json;
using LogAra.Contracts.Requests.Analysis;
using LogAra.Contracts.Responses.Analysis;
using Microsoft.AspNetCore.Components.Forms;

namespace LogAra.Client.Services
{
    public sealed class AnalysisApiClient(HttpClient httpClient)
    {
        public async Task<AnalysisResponseDto> AnalyzeAsync(IBrowserFile file, CancellationToken cancellationToken)
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024, cancellationToken);
            return await AnalyzeAsync(stream, file.Name, string.IsNullOrWhiteSpace(file.ContentType) ? "text/plain" : file.ContentType, cancellationToken);
        }

        public async Task<AnalysisResponseDto> AnalyzeAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken)
        {
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(string.IsNullOrWhiteSpace(contentType) ? "text/plain" : contentType);

            using var content = new MultipartFormDataContent
            {
                { fileContent, "file", fileName }
            };

            using var response = await httpClient.PostAsync("api/analysis/upload", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AnalysisResponseDto>(cancellationToken: cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException("The analysis API returned an empty response.");
            }

            return result;
        }

        public async Task<IssueExplanationDto> ExplainIssueAsync(ExplainIssueRequestDto request, CancellationToken cancellationToken)
        {
            using var response = await httpClient.PostAsJsonAsync("api/analysis/explain", request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<IssueExplanationDto>(cancellationToken: cancellationToken);
            if (result is null)
            {
                throw new InvalidOperationException("The explanation API returned an empty response.");
            }

            return result;
        }
    }
}
