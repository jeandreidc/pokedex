using Kota.Pokedex.Application.DTOs;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Filters;

public record GetFilterGenerationsQuery : IRequest<IReadOnlyList<FilterOptionDto>>;
