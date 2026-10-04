using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Zamfara.Web.Infrastructure;

public static class ProxyHeaders
{
    public static ForwardedHeadersOptions CreateOptions() => new()
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedProto,
        KnownIPNetworks =
        {
            new System.Net.IPNetwork(IPAddress.Loopback, 8),
            new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12)
        }
    };
}
