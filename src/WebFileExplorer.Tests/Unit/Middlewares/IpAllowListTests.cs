using System.Net;
using WebFileExplorer.Server;

namespace WebFileExplorer.Tests.Unit.Middlewares;

[TestClass]
[TestCategory("Unit")]
public class IpAllowListTests
{
    [TestMethod]
    public void ExactIp_DoesNotOvermatchNeighborIps()
    {
        // Regression: legacy code used string StartsWith so "10.0.0.1" also admitted
        // "10.0.0.10" through "10.0.0.19" and "10.0.0.1xx". With IP-parsing, an entry that
        // parses as a concrete IPAddress must match that exact address only.
        var list = new IpAllowList(new[] { "10.0.0.1" });

        Assert.IsTrue(list.Matches(IPAddress.Parse("10.0.0.1")));
        Assert.IsFalse(list.Matches(IPAddress.Parse("10.0.0.10")));
        Assert.IsFalse(list.Matches(IPAddress.Parse("10.0.0.19")));
        Assert.IsFalse(list.Matches(IPAddress.Parse("10.0.0.100")));
    }

    [TestMethod]
    public void Cidr_MatchesNetworkOnly()
    {
        var list = new IpAllowList(new[] { "10.0.0.0/24" });

        Assert.IsTrue(list.Matches(IPAddress.Parse("10.0.0.1")));
        Assert.IsTrue(list.Matches(IPAddress.Parse("10.0.0.255")));
        Assert.IsFalse(list.Matches(IPAddress.Parse("10.0.1.1")));
        Assert.IsFalse(list.Matches(IPAddress.Parse("10.1.0.0")));
    }

    [TestMethod]
    public void LegacyTrailingDotPrefix_StillSupported()
    {
        // Existing configurations rely on trailing-dot string prefixes — they must keep working.
        var list = new IpAllowList(new[] { "10.0.0." });

        Assert.IsTrue(list.Matches(IPAddress.Parse("10.0.0.5")));
        Assert.IsTrue(list.Matches(IPAddress.Parse("10.0.0.99")));
        Assert.IsFalse(list.Matches(IPAddress.Parse("10.0.1.5")));
    }

    [TestMethod]
    public void IPv4MappedIPv6_IsNormalizedBeforeMatching()
    {
        var list = new IpAllowList(new[] { "10.0.0.0/24" });

        var mapped = IPAddress.Parse("::ffff:10.0.0.5");
        Assert.IsTrue(list.Matches(mapped));
    }

    [TestMethod]
    public void Ipv6Cidr_Matches()
    {
        var list = new IpAllowList(new[] { "fe80::/10" });

        Assert.IsTrue(list.Matches(IPAddress.Parse("fe80::1")));
        Assert.IsFalse(list.Matches(IPAddress.Parse("2001:db8::1")));
    }

    [TestMethod]
    public void Empty_DoesNotMatch()
    {
        var list = new IpAllowList(Array.Empty<string>());

        Assert.IsTrue(list.IsEmpty);
        Assert.IsFalse(list.Matches(IPAddress.Parse("10.0.0.1")));
    }
}
