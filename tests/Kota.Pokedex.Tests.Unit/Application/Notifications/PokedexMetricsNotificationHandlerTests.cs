using Kota.Pokedex.Application.Interfaces;
using Kota.Pokedex.Application.Notifications;
using Kota.Pokedex.Application.Notifications.Handlers;
using Moq;

namespace Kota.Pokedex.Tests.Unit.Application.Notifications;

public class PokedexMetricsNotificationHandlerTests {
  private readonly Mock<IPokedexMetricsService> _metrics = new();

  [Fact]
  public async Task PokemonMarkedFavoriteMetricsHandler_RecordsMetric() {
    var handler = new PokemonMarkedFavoriteMetricsHandler(_metrics.Object);

    await handler.Handle(new PokemonMarkedFavoriteNotification(25, "pikachu", "I"), CancellationToken.None);

    _metrics.Verify(m => m.RecordPokemonFavorited("pikachu", "I"), Times.Once);
  }

  [Fact]
  public async Task PokemonMarkedCaughtMetricsHandler_RecordsMetric() {
    var handler = new PokemonMarkedCaughtMetricsHandler(_metrics.Object);

    await handler.Handle(new PokemonMarkedCaughtNotification(1, "bulbasaur", "I"), CancellationToken.None);

    _metrics.Verify(m => m.RecordPokemonCaught("bulbasaur", "I"), Times.Once);
  }

  [Fact]
  public async Task UserRegisteredMetricsHandler_RecordsMetric() {
    var handler = new UserRegisteredMetricsHandler(_metrics.Object);

    await handler.Handle(new UserRegisteredNotification("ash"), CancellationToken.None);

    _metrics.Verify(m => m.RecordUserRegistered(), Times.Once);
  }

  [Fact]
  public async Task UserLoggedInMetricsHandler_RecordsMetric() {
    var handler = new UserLoggedInMetricsHandler(_metrics.Object);

    await handler.Handle(new UserLoggedInNotification("ash"), CancellationToken.None);

    _metrics.Verify(m => m.RecordUserLoggedIn(), Times.Once);
  }
}
