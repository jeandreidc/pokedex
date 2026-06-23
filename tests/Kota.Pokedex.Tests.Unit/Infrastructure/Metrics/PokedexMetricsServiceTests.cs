using System.Diagnostics.Metrics;
using Kota.Pokedex.Infrastructure.Metrics;

namespace Kota.Pokedex.Tests.Unit.Infrastructure.Metrics;

public class PokedexMetricsServiceTests {
  [Fact]
  public void RecordPokemonFavorited_IncrementsCounterWithTags() {
    var measurements = CaptureMeasurements(
      metrics => metrics.RecordPokemonFavorited("pikachu", "I"),
      "pokedex.pokemon.favorited");

    measurements.Should().ContainSingle();
    measurements[0].Value.Should().Be(1);
    measurements[0].Tags.Should().Contain(new KeyValuePair<string, object?>("pokemon_name", "pikachu"));
    measurements[0].Tags.Should().Contain(new KeyValuePair<string, object?>("generation", "I"));
  }

  [Fact]
  public void RecordPokemonCaught_IncrementsCounterWithTags() {
    var measurements = CaptureMeasurements(
      metrics => metrics.RecordPokemonCaught("bulbasaur", "I"),
      "pokedex.pokemon.caught");

    measurements.Should().ContainSingle();
    measurements[0].Value.Should().Be(1);
    measurements[0].Tags.Should().Contain(new KeyValuePair<string, object?>("pokemon_name", "bulbasaur"));
    measurements[0].Tags.Should().Contain(new KeyValuePair<string, object?>("generation", "I"));
  }

  [Fact]
  public void RecordUserRegistered_IncrementsCounter() {
    var measurements = CaptureMeasurements(
      metrics => metrics.RecordUserRegistered(),
      "pokedex.users.registered");

    measurements.Should().ContainSingle();
    measurements[0].Value.Should().Be(1);
  }

  [Fact]
  public void RecordUserLoggedIn_IncrementsCounter() {
    var measurements = CaptureMeasurements(
      metrics => metrics.RecordUserLoggedIn(),
      "pokedex.users.logged_in");

    measurements.Should().ContainSingle();
    measurements[0].Value.Should().Be(1);
  }

  private static List<MetricMeasurement<long>> CaptureMeasurements(
    Action<PokedexMetricsService> record,
    string instrumentName) {
    var measurements = new List<MetricMeasurement<long>>();
    using var listener = new MeterListener();
    listener.InstrumentPublished = (instrument, meterListener) => {
      if (instrument.Meter.Name == PokedexMetricsService.MeterName
          && instrument.Name == instrumentName) {
        meterListener.EnableMeasurementEvents(instrument);
      }
    };
    listener.SetMeasurementEventCallback<long>((_, measurement, tags, _) => {
      measurements.Add(new MetricMeasurement<long>(measurement, tags.ToArray()));
    });
    listener.Start();

    var metrics = new PokedexMetricsService();
    record(metrics);

    return measurements;
  }

  private sealed record MetricMeasurement<T>(T Value, KeyValuePair<string, object?>[] Tags);
}
