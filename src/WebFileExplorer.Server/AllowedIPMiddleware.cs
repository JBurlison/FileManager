using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebFileExplorer.Server;

public sealed class AllowedIPMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AllowedIPMiddleware> _logger;
    private readonly string[] _allowedPrefixes;
    private readonly bool _allowLoopback;

    public AllowedIPMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<AllowedIPMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _allowedPrefixes = GetAllowedPrefixes(configuration);
        _allowLoopback = environment.IsDevelopment();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;

        if (remoteIp is null)
        {
            _logger.LogWarning("Blocking request with no remote IP address.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        if (_allowLoopback && IPAddress.IsLoopback(remoteIp))
        {
            await _next(context);
            return;
        }

        var candidateIp = remoteIp.AddressFamily == AddressFamily.InterNetworkV6 && remoteIp.IsIPv4MappedToIPv6
            ? remoteIp.MapToIPv4()
            : remoteIp;

        if (candidateIp.AddressFamily == AddressFamily.InterNetwork)
        {
            var ipString = candidateIp.ToString();
            if (_allowedPrefixes.Any(prefix => ipString.StartsWith(prefix, StringComparison.Ordinal)))
            {
                await _next(context);
                return;
            }
        }

        _logger.LogWarning("Blocking request from remote IP address {RemoteIpAddress}.", remoteIp);
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
    }

    private static string[] GetAllowedPrefixes(IConfiguration configuration)
    {
        var prefix = configuration["NetworkBinding:AllowedPrefix"];
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            return [prefix];
        }

        var prefixes = configuration.GetSection("NetworkBinding:AllowedPrefixes").Get<string[]>();
        if (prefixes is { Length: > 0 })
        {
            return prefixes.Where(prefix => !string.IsNullOrWhiteSpace(prefix)).ToArray();
        }

        return string.IsNullOrWhiteSpace(prefix) ? [] : [prefix];
    }
}
