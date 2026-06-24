using Kota.Pokedex.Application.DTOs;
using Kota.Pokedex.Application.Queries.Filters;
using Kota.Pokedex.Application.Queries.Pokemon;
using MediatR;

namespace Kota.Pokedex.Application.Queries.Bootstrap;

public class GetBootstrapQueryHandler : IRequestHandler<GetBootstrapQuery, BootstrapDto> {
    private readonly IMediator _mediator;

    public GetBootstrapQueryHandler(IMediator mediator) => _mediator = mediator;

    public async Task<BootstrapDto> Handle(GetBootstrapQuery request, CancellationToken cancellationToken) {
        var typesTask = _mediator.Send(new GetFilterTypesQuery(), cancellationToken);
        var generationsTask = _mediator.Send(new GetFilterGenerationsQuery(), cancellationToken);
        var abilitiesTask = _mediator.Send(new GetFilterAbilitiesQuery {
            Page = 1,
            PageSize = request.AbilityPageSize
        }, cancellationToken);
        var catalogCountTask = _mediator.Send(new GetPokemonCatalogCountQuery(), cancellationToken);

        await Task.WhenAll(typesTask, generationsTask, abilitiesTask, catalogCountTask);

        return new BootstrapDto {
            Types = await typesTask,
            Generations = await generationsTask,
            Abilities = await abilitiesTask,
            PokemonTotalCount = await catalogCountTask
        };
    }
}
