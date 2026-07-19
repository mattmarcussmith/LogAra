using LogAra.Application.Abstractions;
using LogAra.Contracts.Requests.Analysis;
using LogAra.Contracts.Responses.Analysis;
using Microsoft.AspNetCore.Mvc;

namespace LogAra.Api.Controllers
{
    [ApiController]
    [Route("api/analysis")]
    public sealed class AnalysisController(IAnalysisApiService analysisApiService) : ControllerBase
    {
        [HttpPost("upload")]
        [DisableRequestSizeLimit]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<AnalysisResponseDto>> Upload([FromForm] IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
            {
                return BadRequest("A non-empty log file is required.");
            }

            await using var stream = file.OpenReadStream();
            var result = await analysisApiService.AnalyzeAsync(stream, cancellationToken);
            return Ok(result);
        }

        [HttpPost("explain")]
        public async Task<ActionResult<IssueExplanationDto>> Explain([FromBody] ExplainIssueRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Fingerprint) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Fingerprint and message are required.");
            }

            try
            {
                return Ok(await analysisApiService.ExplainAsync(request, cancellationToken));
            }
            catch (HttpRequestException)
            {
                return Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "AI provider is unavailable.");
            }
        }
    }
}
