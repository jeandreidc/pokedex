using FluentValidation;

namespace Kota.Pokedex.Application.Commands.Collection;

public class UpdateCollectionEntryCommandValidator : AbstractValidator<UpdateCollectionEntryCommand> {
    public UpdateCollectionEntryCommandValidator() {
        RuleFor(x => x.PokemonId).GreaterThan(0);
        RuleFor(x => x)
            .Must(x => x.IsCaught.HasValue || x.IsFavorite.HasValue)
            .WithMessage("At least one of IsCaught or IsFavorite must be provided.");
    }
}

public class RemoveCollectionEntryCommandValidator : AbstractValidator<RemoveCollectionEntryCommand> {
    public RemoveCollectionEntryCommandValidator() {
        RuleFor(x => x.PokemonId).GreaterThan(0);
    }
}
