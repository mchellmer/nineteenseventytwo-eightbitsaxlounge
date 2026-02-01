using Microsoft.AspNetCore.Http;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public interface IEndpointHandler<in TRequest, TResponse>
{
    Task<TResponse> HandleAsync(TRequest request);
}

public interface IMidiEndpointHandler<TResponse>
{
    Task<TResponse> HandleAsync();
}
