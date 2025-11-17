using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokeSync.Infrastructure.Interfaces;

[ApiController]
[Route("api/status")]
public sealed class StatusController : ControllerBase
{
    private readonly IStatusService _status;
    public StatusController(IStatusService status) => _status = status;

    [HttpGet]
    [AllowAnonymous] // public
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var dto = await _status.GetAsync(ct);
        return Ok(new StatusResponse
        {
            Initializing = dto.Initializing,
            LastSyncUtc = dto.LastSyncUtc
        });
    }
}

public sealed class StatusResponse
{
    public bool Initializing { get; init; }
    public DateTimeOffset? LastSyncUtc { get; init; } // rendu en ISO 8601 UTC
}
