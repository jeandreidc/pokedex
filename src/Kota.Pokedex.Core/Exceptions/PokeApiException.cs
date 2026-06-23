namespace Kota.Pokedex.Core.Exceptions;

public class PokeApiException : Exception {
    public int? StatusCode { get; }

    public PokeApiException(string message, int? statusCode = null, Exception? innerException = null)
        : base(message, innerException) {
        StatusCode = statusCode;
    }
}
