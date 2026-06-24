using Kota.Pokedex.Application.DTOs;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Bootstrap;

public record GetBootstrapQuery : IRequest<BootstrapDto> {
    public int PageSize { get; init; } = 12;
    public int AbilityPageSize { get; init; } = 50;
}
