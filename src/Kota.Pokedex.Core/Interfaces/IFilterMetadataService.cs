namespace Kota.Pokedex.Core.Interfaces;

public interface IFilterMetadataService {
    Task<IReadOnlyList<FilterOption>> GetTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FilterOption>> GetAbilitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FilterOption>> GetGenerationsAsync(CancellationToken cancellationToken = default);
    Task WarmupAsync(CancellationToken cancellationToken = default);
}

public record FilterOption(int Id, string Name, string DisplayName);
