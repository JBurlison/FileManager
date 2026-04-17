using System.Net;
using System.Net.Sockets;

namespace WebFileExplorer.Server;

/// <summary>
/// Parses configured allow-list entries and matches <see cref="IPAddress"/> values against them.
/// Supported entry formats (evaluated in order):
///   * CIDR notation, e.g. "10.0.0.0/24" or "fe80::/10"
///   * Exact IP literal, e.g. "127.0.0.1"
///   * Legacy string prefix, e.g. "10.0.0." (retained for backward compatibility)
/// IPv4-mapped IPv6 addresses are normalized to IPv4 before matching.
/// </summary>
internal sealed class IpAllowList
{
    private readonly IReadOnlyList<Entry> _entries;

    public IpAllowList(IEnumerable<string> rawEntries)
    {
        var parsed = new List<Entry>();
        foreach (var raw in rawEntries)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (TryParseCidr(raw, out var cidr))
            {
                parsed.Add(cidr);
                continue;
            }

            if (IPAddress.TryParse(raw, out var exact))
            {
                parsed.Add(Entry.ForExact(exact));
                continue;
            }

            parsed.Add(Entry.ForLegacyPrefix(raw));
        }

        _entries = parsed;
    }

    public bool IsEmpty => _entries.Count == 0;

    public bool Matches(IPAddress address)
    {
        if (address is null)
        {
            return false;
        }

        var candidate = Normalize(address);

        foreach (var entry in _entries)
        {
            if (entry.Matches(candidate))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns true if any entry could plausibly be satisfied by an IPv4 address assigned to a local interface.
    /// Used by <see cref="Configuration.NetworkBindingExtensions"/> while scanning interfaces for a binding address.
    /// </summary>
    public bool MatchesAny(IPAddress address) => Matches(address);

    private static IPAddress Normalize(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6)
        {
            return address.MapToIPv4();
        }
        return address;
    }

    private static bool TryParseCidr(string raw, out Entry entry)
    {
        entry = default!;
        var slashIndex = raw.IndexOf('/');
        if (slashIndex <= 0)
        {
            return false;
        }

        var ipPart = raw.AsSpan(0, slashIndex).ToString();
        var maskPart = raw.AsSpan(slashIndex + 1).ToString();

        if (!IPAddress.TryParse(ipPart, out var network))
        {
            return false;
        }

        if (!int.TryParse(maskPart, out var maskBits) || maskBits < 0)
        {
            return false;
        }

        var totalBits = network.AddressFamily == AddressFamily.InterNetwork ? 32 : 128;
        if (maskBits > totalBits)
        {
            return false;
        }

        entry = Entry.ForCidr(network, maskBits);
        return true;
    }

    private readonly struct Entry
    {
        private readonly EntryKind _kind;
        private readonly byte[]? _networkBytes;
        private readonly int _maskBits;
        private readonly AddressFamily _family;
        private readonly string? _legacyPrefix;

        private Entry(EntryKind kind, byte[]? networkBytes, int maskBits, AddressFamily family, string? legacyPrefix)
        {
            _kind = kind;
            _networkBytes = networkBytes;
            _maskBits = maskBits;
            _family = family;
            _legacyPrefix = legacyPrefix;
        }

        public static Entry ForCidr(IPAddress network, int maskBits)
        {
            var bytes = network.GetAddressBytes();
            ApplyMask(bytes, maskBits);
            return new Entry(EntryKind.Cidr, bytes, maskBits, network.AddressFamily, null);
        }

        public static Entry ForExact(IPAddress address)
        {
            var bytes = address.GetAddressBytes();
            return new Entry(EntryKind.Exact, bytes, bytes.Length * 8, address.AddressFamily, null);
        }

        public static Entry ForLegacyPrefix(string prefix)
            => new(EntryKind.LegacyPrefix, null, 0, AddressFamily.Unspecified, prefix);

        public bool Matches(IPAddress candidate)
        {
            switch (_kind)
            {
                case EntryKind.Exact:
                    if (candidate.AddressFamily != _family) return false;
                    var exactBytes = candidate.GetAddressBytes();
                    return exactBytes.AsSpan().SequenceEqual(_networkBytes);
                case EntryKind.Cidr:
                    if (candidate.AddressFamily != _family) return false;
                    var candidateBytes = candidate.GetAddressBytes();
                    return PrefixEquals(candidateBytes, _networkBytes!, _maskBits);
                case EntryKind.LegacyPrefix:
                    return candidate.ToString().StartsWith(_legacyPrefix!, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        private static void ApplyMask(byte[] bytes, int maskBits)
        {
            var fullBytes = maskBits / 8;
            var remainingBits = maskBits % 8;
            for (var i = fullBytes; i < bytes.Length; i++)
            {
                if (i == fullBytes && remainingBits > 0)
                {
                    var mask = (byte)(0xFF << (8 - remainingBits));
                    bytes[i] &= mask;
                }
                else
                {
                    bytes[i] = 0;
                }
            }
        }

        private static bool PrefixEquals(byte[] a, byte[] b, int maskBits)
        {
            if (a.Length != b.Length) return false;

            var fullBytes = maskBits / 8;
            for (var i = 0; i < fullBytes; i++)
            {
                if (a[i] != b[i]) return false;
            }

            var remainingBits = maskBits % 8;
            if (remainingBits == 0) return true;

            var mask = (byte)(0xFF << (8 - remainingBits));
            return (a[fullBytes] & mask) == (b[fullBytes] & mask);
        }

        private enum EntryKind
        {
            Exact,
            Cidr,
            LegacyPrefix
        }
    }
}
