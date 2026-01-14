using System.Threading;
using System.Threading.Tasks;
using Aprs.Application.Packets.Queries.GetPackets;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aprs.Api.Controllers;

/// <summary>
/// API endpoints for APRS packet operations.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/packets")]
[EnableRateLimiting("sliding")]
[Produces("application/json")]
public class PacketsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PacketsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Retrieves packets with optional filtering and pagination.
    /// </summary>
    /// <param name="query">Query parameters for filtering and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of APRS packets.</returns>
    /// <response code="200">Returns the list of packets.</response>
    /// <response code="400">Invalid query parameters.</response>
    /// <response code="429">Rate limit exceeded.</response>
    [HttpGet]
    [ProducesResponseType(typeof(GetPacketsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<GetPacketsResponse>> Get(
        [FromQuery] GetPacketsQuery query, 
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
