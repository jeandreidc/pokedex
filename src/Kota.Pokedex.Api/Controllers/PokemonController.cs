using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Queries.Pokemon;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kota.Pokedex.Api.Controllers;

/// <summary>
/// Search and retrieve Pokemon from the Pokedex.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class PokemonController : ControllerBase {
    private readonly IMediator _mediator;

    public PokemonController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Search Pokemon with combinable filters and pagination.
    /// </summary>
    /// <param name="search">Case-insensitive name contains filter.</param>
    /// <param name="type">Filter by type slug, e.g. <c>fire</c>.</param>
    /// <param name="ability">Filter by ability slug, e.g. <c>overgrow</c>.</param>
    /// <param name="generation">Filter by generation id (<c>1</c>) or slug (<c>generation-i</c>).</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 20, max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PokemonSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PagedResult<PokemonSummaryDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] string? type,
        [FromQuery] string? ability,
        [FromQuery] string? generation,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) {
        var result = await _mediator.Send(new SearchPokemonQuery {
            Search = search,
            Type = type,
            Ability = ability,
            Generation = generation,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get a single Pokemon by numeric id or name slug.
    /// </summary>
    /// <param name="idOrName">Pokemon id (e.g. <c>25</c>) or name (e.g. <c>pikachu</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{idOrName}")]
    [ProducesResponseType(typeof(PokemonSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PokemonSummaryDto>> GetById(
        string idOrName,
        CancellationToken cancellationToken = default) {
        var result = await _mediator.Send(new GetPokemonByIdQuery(idOrName), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
