using Kota.Pokedex.Core.Interfaces;

namespace Kota.Pokedex.Infrastructure.Services;

public sealed class WarmupState : IWarmupState {
    public bool IsComplete { get; private set; }

    public void MarkComplete() => IsComplete = true;
}
