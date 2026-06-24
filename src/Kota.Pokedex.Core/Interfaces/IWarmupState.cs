namespace Kota.Pokedex.Core.Interfaces;

public interface IWarmupState {
    bool IsComplete { get; }
    void MarkComplete();
}
