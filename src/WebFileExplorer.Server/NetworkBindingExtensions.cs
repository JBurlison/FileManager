using System.Net;
using System.Net.NetworkInformation;

namespace WebFileExplorer.Server;

public static class NetworkBindingExtensions
{
    private const string AllowedPrefixKey = "NetworkBinding:AllowedPrefix";
    private const string PortKey = "NetworkBinding:Port";
    private const string DefaultPrefix = "10.0.0.";
    private const int DefaultPort = 5000;

    public static IWebHostBuilder ConfigureNetworkBindings(this IWebHostBuilder webHostBuilder, IConfiguration configuration, bool isDevelopment)
    {
        var allowedIPPrefix = configuration[AllowedPrefixKey] ?? DefaultPrefix;
        var bindPort = configuration.GetValue<int>(PortKey, DefaultPort);

        if (!isDevelopment && !allowedIPPrefix.StartsWith(DefaultPrefix))
        {
            throw new InvalidOperationException($"Strict hosting to {DefaultPrefix}x interfaces is required in production environments.");
        }

        var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
            .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
            .SelectMany(ni => ni.GetIPProperties().UnicastAddresses)
            .Where(ua => ua.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            .Select(ua => ua.Address)
            .ToList();

        var allowedAddresses = networkInterfaces.Where(ip => ip.ToString().StartsWith(allowedIPPrefix)).ToList();

        if (isDevelopment)
        {
            if (!allowedAddresses.Contains(IPAddress.Loopback))
                allowedAddresses.Add(IPAddress.Loopback);
                
            if (!allowedAddresses.Contains(IPAddress.IPv6Loopback))
                allowedAddresses.Add(IPAddress.IPv6Loopback);
        }

        if (!allowedAddresses.Any())
        {
            throw new InvalidOperationException($"FATAL ERROR: No network interfaces matching '{allowedIPPrefix}' found. The application strictly requires a local {allowedIPPrefix}x network. Shutting down.");
        }

        webHostBuilder.ConfigureKestrel(options =>
        {
            var logger = options.ApplicationServices.GetService<ILogger<Program>>();

            foreach (var ip in allowedAddresses)
            {
                options.Listen(ip, bindPort);
                if (logger != null)
                {
                    logger.LogInformation("Binding to allowed local address: {IP}:{Port}", ip, bindPort);
                }
                else
                {
                    Console.WriteLine($"Binding to allowed local address: {ip}:{bindPort}");
                }
            }
        });

        return webHostBuilder;
    }
}
