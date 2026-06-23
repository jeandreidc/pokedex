using Kota.Pokedex.Application.Common;
using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Queries.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kota.Pokedex.Api.Controllers;

/// <summary>
/// Filter metadata for populating search dropdowns in the frontend.
/// </summary>
[ApiController]
[Route("api/filters")]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class FiltersController : ControllerBase {
    private readonly IMediator _mediator;

    public FiltersController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// List all Pokemon types for filter dropdowns.
    /// </summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IReadOnlyList<FilterOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<IReadOnlyList<FilterOptionDto>>> GetTypes(CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetFilterTypesQuery(), cancellationToken));

    /// <summary>
    /// List abilities for the filter dropdown. Returns the first page immediately on open;
    /// use the optional <c>search</c> parameter to narrow results as the user types.
    /// </summary>
    /// <param name="search">Optional typeahead filter (narrows the dropdown list).</param>
    /// <param name="page">Page number (default 1).</param>
    /// <param name="pageSize">Items per page (default 50, max 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("abilities")]
    [ProducesResponseType(typeof(PagedResult<FilterOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PagedResult<FilterOptionDto>>> GetAbilities(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetFilterAbilitiesQuery {
            Search = search,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    /// <summary>
    /// List all generations for filter dropdowns.
    /// </summary>
    [HttpGet("generations")]
    [ProducesResponseType(typeof(IReadOnlyList<FilterOptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<IReadOnlyList<FilterOptionDto>>> GetGenerations(CancellationToken cancellationToken = default) =>
        Ok(await _mediator.Send(new GetFilterGenerationsQuery(), cancellationToken));
}
