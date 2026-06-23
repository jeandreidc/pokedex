using Kota.Pokedex.Application.Commands.Auth;
using Kota.Pokedex.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Kota.Pokedex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("api")]
[Produces("application/json")]
public class AuthController : ControllerBase {
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponseDto>> Register(
        [FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken) {
        try {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(Register), result);
        }
        catch (InvalidOperationException ex) {
            return Conflict(new { error = ex.Message });
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginUserCommand command,
        CancellationToken cancellationToken) {
        try {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) {
            return Unauthorized(new { error = ex.Message });
        }
    }
}
