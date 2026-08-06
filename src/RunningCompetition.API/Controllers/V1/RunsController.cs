using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RunningCompetition.Application.DTOs.Runs;
using RunningCompetition.Application.Services;
using RunningCompetition.Shared.Common;

namespace RunningCompetition.API.Controllers.V1;

/// <summary>Manages running sessions: start, pause, resume, finish, GPS, and history.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/runs")]
[Authorize]
[Produces("application/json")]
public sealed class RunsController : ControllerBase
{
    private readonly RunService _runService;

    public RunsController(RunService runService) => _runService = runService;

    /// <summary>Starts a new run session.</summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(ApiResponse<RunSessionDto>), 201)]
    public async Task<IActionResult> Start([FromBody] StartRunRequest request, CancellationToken cancellationToken)
    {
        var session = await _runService.StartRunAsync(request, cancellationToken);
        return StatusCode(201, ApiResponse<RunSessionDto>.Created(session, "Run started."));
    }

    /// <summary>Pauses the active run session.</summary>
    [HttpPost("pause")]
    [ProducesResponseType(typeof(ApiResponse<RunSessionDto>), 200)]
    public async Task<IActionResult> Pause(CancellationToken cancellationToken)
        => Ok(ApiResponse<RunSessionDto>.Ok(await _runService.PauseRunAsync(cancellationToken), "Run paused."));

    /// <summary>Resumes the paused run session.</summary>
    [HttpPost("resume")]
    [ProducesResponseType(typeof(ApiResponse<RunSessionDto>), 200)]
    public async Task<IActionResult> Resume(CancellationToken cancellationToken)
        => Ok(ApiResponse<RunSessionDto>.Ok(await _runService.ResumeRunAsync(cancellationToken), "Run resumed."));

    /// <summary>Finishes the active run session and processes achievements.</summary>
    [HttpPost("finish")]
    [ProducesResponseType(typeof(ApiResponse<RunSessionDto>), 200)]
    public async Task<IActionResult> Finish([FromBody] FinishRunRequest request, CancellationToken cancellationToken)
        => Ok(ApiResponse<RunSessionDto>.Ok(await _runService.FinishRunAsync(request, cancellationToken), "Run completed!"));

    /// <summary>Batch-uploads GPS waypoints for an active session (max 100 per request).</summary>
    [HttpPost("{sessionId:guid}/gps")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<IActionResult> UploadGps(Guid sessionId, [FromBody] GpsBatchRequest request, CancellationToken cancellationToken)
    {
        await _runService.UploadGpsAsync(sessionId, request, cancellationToken);
        return Ok(ApiResponse.Ok($"{request.Locations.Count} GPS points recorded."));
    }

    /// <summary>Gets paginated run history for the authenticated user.</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<PagedList<RunSessionDto>>), 200)]
    public async Task<IActionResult> GetMyRuns([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
        => Ok(ApiResponse<PagedList<RunSessionDto>>.Ok(await _runService.GetMyRunsAsync(page, pageSize, cancellationToken)));

    /// <summary>Gets a detailed run session with GPS waypoints and lap splits.</summary>
    [HttpGet("{sessionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RunSessionDetailDto>), 200)]
    public async Task<IActionResult> GetDetail(Guid sessionId, CancellationToken cancellationToken)
        => Ok(ApiResponse<RunSessionDetailDto>.Ok(await _runService.GetRunDetailAsync(sessionId, cancellationToken)));

    /// <summary>Gets run statistics for the authenticated user over a date range.</summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<RunStatsDto>), 200)]
    public async Task<IActionResult> GetStats([FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken cancellationToken)
        => Ok(ApiResponse<RunStatsDto>.Ok(await _runService.GetStatsAsync(from, to, cancellationToken)));
}
