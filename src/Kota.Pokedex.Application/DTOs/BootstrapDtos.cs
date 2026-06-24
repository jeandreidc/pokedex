using Kota.Pokedex.Application.Common;

namespace Kota.Pokedex.Application.DTOs;

public class BootstrapDto {
    public IReadOnlyList<FilterOptionDto> Types { get; init; } = [];
    public IReadOnlyList<FilterOptionDto> Generations { get; init; } = [];
    public PagedResult<FilterOptionDto> Abilities { get; init; } = new();
    public PagedResult<PokemonSummaryDto> Pokemon { get; init; } = new();
}
