using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace WebFileExplorer.Server.Configuration;

public static class NetworkBindingExtensions
{
    private const string NetworkBindingSection = "NetworkBinding";
    private const string AllowedPrefixKey = NetworkBindingSection + ":AllowedPrefix";
    private const string AllowedPrefixesKey = NetworkBindingSection + ":AllowedPrefixes";
    private const string PortKey = NetworkBindingSection + ":Port";

    public static ConfigureWebHostBuilder ConfigureNetworkBindings(
        this ConfigureWebHostBuilder builder,
        IConfiguration configuration,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var allowedPrefixes = GetAllowedPrefixes(configuration);
        var allowList = new IpAllowList(allowedPrefixes);
        var port = configuration.GetValue<int?>(PortKey) ?? 5000;

        if (TryFindBindingAddress(allowList, out var bindingAddress))
        {
            builder.UseUrls($"http://{bindingAddress}:{port}");
            return builder;
        }

        if (isDevelopment)
        {
            builder.UseUrls($"http://127.0.0.1:{port}");
            return builder;
        }

        var configuredPrefixes = allowedPrefixes.Length == 0 ? "<none>" : string.Join(", ", allowedPrefixes);
        throw new InvalidOperationException($"FATAL ERROR: No network interfaces matching '{configuredPrefixes}' found. The application strictly requires a configured local network. Shutting down.");
    }

    private static bool TryFindBindingAddress(IpAllowList allowList, out IPAddress bindingAddress)
    {
        bindingAddress = IPAddress.None;

        if (allowList.IsEmpty)
        {
            return false;
        }

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up)
            {
                continue;
            }

            foreach (var unicastAddress in networkInterface.GetIPProperties().UnicastAddresses)
            {
                var address = unicastAddress.Address;
                if (address.AddressFamily != AddressFamily.InterNetwork &&
                    address.AddressFamily != AddressFamily.InterNetworkV6)
                {
                    continue;
                }

                if (allowList.Matches(address))
                {
                    bindingAddress = address;
                    return true;
                }
            }
        }

        return false;
    }

    private static string[] GetAllowedPrefixes(IConfiguration configuration)
    {
        var prefix = configuration[AllowedPrefixKey];
        if (!string.IsNullOrWhiteSpace(prefix))
        {
            return [prefix];
        }

        var prefixes = configuration.GetSection(AllowedPrefixesKey).Get<string[]>();
        if (prefixes is { Length: > 0 })
        {
            return prefixes.Where(prefix => !string.IsNullOrWhiteSpace(prefix)).ToArray();
        }

        return string.IsNullOrWhiteSpace(prefix) ? [] : [prefix];
    }
}
