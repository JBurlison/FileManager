using System.Net;

namespace WebFileExplorer.Server;

public class AllowedIPMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AllowedIPMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _allowedPrefix;

    public AllowedIPMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AllowedIPMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
        _environment = environment;
        _allowedPrefix = _configuration["NetworkBinding:AllowedPrefix"] ?? "10.0.0.";

        if (!_environment.IsDevelopment() && !_allowedPrefix.StartsWith("10.0.0."))
        {
            throw new InvalidOperationException("Strict hosting to 10.0.0.x interfaces is required in production environments.");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp == null)
        {
            _logger.LogWarning("Request rejected: Remote IP address is null.");
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var ipString = remoteIp.ToString();
        var isLocalOrIPv6Local = IPAddress.IsLoopback(remoteIp) || remoteIp.Equals(IPAddress.IPv6Loopback) || remoteIp.ToString() == "::1" || remoteIp.ToString() == "127.0.0.1";

        bool allowed = ipString.StartsWith(_allowedPrefix);
        
        if (!allowed && _environment.IsDevelopment() && isLocalOrIPv6Local)
        {
            allowed = true;
        }

        if (!allowed)
        {
            _logger.LogWarning("Request from {RemoteIp} was rejected because it does not match the allowed prefix {Prefix}.", ipString, _allowedPrefix);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }
}
