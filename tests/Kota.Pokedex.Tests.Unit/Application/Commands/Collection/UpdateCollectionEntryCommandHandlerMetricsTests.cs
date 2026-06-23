using Kota.Pokedex.Application.Commands.Collection;
using Kota.Pokedex.Application.Interfaces;
using Kota.Pokedex.Application.Notifications;
using Kota.Pokedex.Core.Entities;
using Kota.Pokedex.Core.Interfaces;
using Kota.Pokedex.Core.Models;
using MediatR;
using Moq;

namespace Kota.Pokedex.Tests.Unit.Application.Commands.Collection;

public class UpdateCollectionEntryCommandHandlerMetricsTests {
  private readonly Mock<ICollectionRepository> _collectionRepository = new();
  private readonly Mock<IPokemonIndexService> _indexService = new();
  private readonly Mock<ICurrentUserService> _currentUser = new();
  private readonly Mock<IPublisher> _publisher = new();
  private readonly UpdateCollectionEntryCommandHandler _sut;

  public UpdateCollectionEntryCommandHandlerMetricsTests() {
    _currentUser.Setup(c => c.UserId).Returns(Guid.NewGuid());
    _indexService.Setup(s => s.GetEntryAsync(25, It.IsAny<CancellationToken>()))
      .ReturnsAsync(new PokemonIndexEntry { Id = 25, Name = "pikachu", SpriteUrl = "sprite.png" });
    _indexService.Setup(s => s.GetPokemonCardDetailsAsync(25, It.IsAny<CancellationToken>()))
      .ReturnsAsync(new PokemonCardDetails { Generation = "I" });

    _sut = new UpdateCollectionEntryCommandHandler(
      _collectionRepository.Object,
      _indexService.Object,
      _currentUser.Object,
      _publisher.Object);
  }

  [Fact]
  public async Task Handle_WhenMarkedFavorite_PublishesFavoriteNotification() {
    _collectionRepository.Setup(r => r.GetEntryAsync(It.IsAny<Guid>(), 25, It.IsAny<CancellationToken>()))
      .ReturnsAsync(new UserPokemonEntry { PokemonId = 25, IsFavorite = false, IsCaught = false });

    await _sut.Handle(new UpdateCollectionEntryCommand(25, false, true), CancellationToken.None);

    _publisher.Verify(
      p => p.Publish(
        It.Is<PokemonMarkedFavoriteNotification>(n =>
          n.PokemonId == 25 && n.PokemonName == "pikachu" && n.Generation == "I"),
        It.IsAny<CancellationToken>()),
      Times.Once);
    _publisher.Verify(
      p => p.Publish(It.IsAny<PokemonMarkedCaughtNotification>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }

  [Fact]
  public async Task Handle_WhenMarkedCaught_PublishesCaughtNotification() {
    _collectionRepository.Setup(r => r.GetEntryAsync(It.IsAny<Guid>(), 25, It.IsAny<CancellationToken>()))
      .ReturnsAsync(new UserPokemonEntry { PokemonId = 25, IsFavorite = false, IsCaught = false });

    await _sut.Handle(new UpdateCollectionEntryCommand(25, true, false), CancellationToken.None);

    _publisher.Verify(
      p => p.Publish(
        It.Is<PokemonMarkedCaughtNotification>(n =>
          n.PokemonId == 25 && n.PokemonName == "pikachu" && n.Generation == "I"),
        It.IsAny<CancellationToken>()),
      Times.Once);
  }

  [Fact]
  public async Task Handle_WhenAlreadyFavorite_DoesNotPublishAgain() {
    _collectionRepository.Setup(r => r.GetEntryAsync(It.IsAny<Guid>(), 25, It.IsAny<CancellationToken>()))
      .ReturnsAsync(new UserPokemonEntry { PokemonId = 25, IsFavorite = true, IsCaught = false });

    await _sut.Handle(new UpdateCollectionEntryCommand(25, false, true), CancellationToken.None);

    _publisher.Verify(
      p => p.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()),
      Times.Never);
  }
}
