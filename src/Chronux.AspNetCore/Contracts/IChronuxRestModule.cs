using Microsoft.AspNetCore.Routing;

namespace Chronux.AspNetCore.Contracts;

public interface IChronuxRestModule
{
    void MapEndpoints(IEndpointRouteBuilder endpoints);
}