using Kota.Pokedex.Application.Commands.Collection;
using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Queries.Collection;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kota.Pokedex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class CollectionController : ControllerBase {
    private readonly IMediator _mediator;

    public CollectionController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CollectionEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CollectionEntryDto>>> GetCollection(
        [FromQuery] bool? favoritesOnly,
        [FromQuery] bool? caughtOnly,
        CancellationToken cancellationToken) {
        var result = await _mediator.Send(new GetCollectionQuery(favoritesOnly, caughtOnly), cancellationToken);
        return Ok(result);
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(CollectionStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CollectionStatsDto>> GetStats(CancellationToken cancellationToken) {
        var result = await _mediator.Send(new GetCollectionStatsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{pokemonId:int}")]
    [ProducesResponseType(typeof(CollectionEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionEntryDto>> GetEntry(
        int pokemonId,
        CancellationToken cancellationToken) {
        var result = await _mediator.Send(new GetCollectionEntryQuery(pokemonId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{pokemonId:int}")]
    [ProducesResponseType(typeof(CollectionEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionEntryDto>> UpdateEntry(
        int pokemonId,
        [FromBody] UpdateCollectionEntryRequest request,
        CancellationToken cancellationToken) {
        try {
            var result = await _mediator.Send(new UpdateCollectionEntryCommand(
                pokemonId,
                request.IsCaught,
                request.IsFavorite), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("{pokemonId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveEntry(int pokemonId, CancellationToken cancellationToken) {
        await _mediator.Send(new RemoveCollectionEntryCommand(pokemonId), cancellationToken);
        return NoContent();
    }
}
