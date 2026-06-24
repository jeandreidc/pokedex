using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Queries.Bootstrap;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kota.Pokedex.Api.Controllers;

/// <summary>
/// Initial page payload: filter metadata (first abilities page) and first Pokémon page with total counts.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class BootstrapController : ControllerBase {
    private readonly IMediator _mediator;

    public BootstrapController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(BootstrapDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<BootstrapDto>> Get(
        [FromQuery] int pageSize = 12,
        [FromQuery] int abilityPageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetBootstrapQuery {
            PageSize = pageSize,
            AbilityPageSize = abilityPageSize
        }, cancellationToken));
}
