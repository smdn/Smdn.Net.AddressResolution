// Smdn.Net.AddressResolution.dll (Smdn.Net.AddressResolution-1.0.0-preview6)
//   Name: Smdn.Net.AddressResolution
//   AssemblyVersion: 1.0.0.0
//   InformationalVersion: 1.0.0-preview6+867630e3d8768ccca99d991e9c0c1cca62c64c76
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Vanara.Core, Version=3.4.13.0, Culture=neutral, PublicKeyToken=c37e4080322237fa
//     Vanara.PInvoke.IpHlpApi, Version=3.4.13.0, Culture=neutral, PublicKeyToken=c37e4080322237fa
//     Vanara.PInvoke.Shared, Version=3.4.13.0, Culture=neutral, PublicKeyToken=c37e4080322237fa
//     Vanara.PInvoke.Ws2_32, Version=3.4.13.0, Culture=neutral, PublicKeyToken=c37e4080322237fa
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smdn.Net;
using Smdn.Net.AddressResolution;
using Smdn.Net.NeighborDiscovery;

namespace Smdn.Net {
  public abstract class IPNetworkProfile {
    public static IPNetworkProfile Create() {}
    public static IPNetworkProfile Create(Func<IEnumerable<IPAddress>?> addressRangeGenerator, NetworkInterface? networkInterface = null) {}
    public static IPNetworkProfile Create(IPAddress baseAddress, IPAddress subnetMask, NetworkInterface? networkInterface = null) {}
    public static IPNetworkProfile Create(IPAddress baseAddress, int prefixLength, NetworkInterface? networkInterface = null) {}
    public static IPNetworkProfile Create(NetworkInterface networkInterface) {}
    public static IPNetworkProfile Create(Predicate<NetworkInterface> predicate) {}

    protected IPNetworkProfile(NetworkInterface? networkInterface) {}

    public NetworkInterface? NetworkInterface { get; }

    public abstract IEnumerable<IPAddress>? GetAddressRange();
  }

  public static class PhysicalAddressExtensions {
    public static string ToMacAddressString(this PhysicalAddress hardwareAddress, char delimiter = ':') {}
  }
}

namespace Smdn.Net.AddressResolution {
  public interface IAddressResolver<TAddress, TResolvedAddress> {
    void Invalidate(TAddress address);
    ValueTask<TResolvedAddress> ResolveAsync(TAddress address, CancellationToken cancellationToken);
  }

  public class MacAddressResolver : MacAddressResolverBase {
    protected MacAddressResolver(INeighborTable neighborTable, INeighborDiscoverer neighborDiscoverer, TimeSpan neighborDiscoveryInterval, ILogger? logger) {}
    public MacAddressResolver(INeighborTable? neighborTable = null, INeighborDiscoverer? neighborDiscoverer = null, int neighborDiscoveryIntervalMilliseconds = -1, IServiceProvider? serviceProvider = null) {}
    public MacAddressResolver(IPNetworkProfile? networkProfile, IServiceProvider? serviceProvider = null) {}
    public MacAddressResolver(IPNetworkProfile? networkProfile, TimeSpan neighborDiscoveryInterval, IServiceProvider? serviceProvider = null) {}
    public MacAddressResolver(TimeSpan neighborDiscoveryInterval, INeighborTable? neighborTable = null, INeighborDiscoverer? neighborDiscoverer = null, IServiceProvider? serviceProvider = null) {}

    public override bool HasInvalidated { get; }

    protected override void Dispose(bool disposing) {}
    protected override void InvalidateCore(IPAddress ipAddress) {}
    protected override void InvalidateCore(PhysicalAddress macAddress) {}
    protected override ValueTask RefreshCacheAsyncCore(CancellationToken cancellationToken = default) {}
    protected override ValueTask RefreshInvalidatedCacheAsyncCore(CancellationToken cancellationToken = default) {}
    protected override async ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(IPAddress ipAddress, CancellationToken cancellationToken) {}
    protected override async ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(PhysicalAddress macAddress, CancellationToken cancellationToken) {}
  }

  public abstract class MacAddressResolverBase :
    IAddressResolver<IPAddress, PhysicalAddress>,
    IAddressResolver<PhysicalAddress, IPAddress>,
    IDisposable
  {
    protected static PhysicalAddress AllZeroMacAddress { get; }
    public static MacAddressResolverBase Null { get; }

    protected MacAddressResolverBase(ILogger? logger = null) {}

    public abstract bool HasInvalidated { get; }
    protected ILogger? Logger { get; }

    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public void Invalidate(IPAddress ipAddress) {}
    public void Invalidate(PhysicalAddress macAddress) {}
    protected abstract void InvalidateCore(IPAddress ipAddress);
    protected abstract void InvalidateCore(PhysicalAddress macAddress);
    public ValueTask RefreshCacheAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask RefreshCacheAsyncCore(CancellationToken cancellationToken) {}
    public ValueTask RefreshInvalidatedCacheAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask RefreshInvalidatedCacheAsyncCore(CancellationToken cancellationToken) {}
    public ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsync(IPAddress ipAddress, CancellationToken cancellationToken = default) {}
    protected abstract ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(IPAddress ipAddress, CancellationToken cancellationToken);
    public ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsync(PhysicalAddress macAddress, CancellationToken cancellationToken = default) {}
    protected abstract ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(PhysicalAddress macAddress, CancellationToken cancellationToken);
    void IAddressResolver<IPAddress, PhysicalAddress>.Invalidate(IPAddress address) {}
    ValueTask<PhysicalAddress?> IAddressResolver<IPAddress, PhysicalAddress>.ResolveAsync(IPAddress address, CancellationToken cancellationToken) {}
    void IAddressResolver<PhysicalAddress, IPAddress>.Invalidate(PhysicalAddress address) {}
    ValueTask<IPAddress?> IAddressResolver<PhysicalAddress, IPAddress>.ResolveAsync(PhysicalAddress address, CancellationToken cancellationToken) {}
    protected void ThrowIfDisposed() {}
  }
}

namespace Smdn.Net.NeighborDiscovery {
  public interface INeighborDiscoverer {
    ValueTask DiscoverAsync(CancellationToken cancellationToken);
    ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken);
  }

  public interface INeighborTable {
    IAsyncEnumerable<NeighborTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken);
  }

  public enum NeighborTableEntryState : int {
    Delay = 4,
    Incomplete = 1,
    None = 0,
    Probe = 5,
    Reachable = 2,
    Stale = 3,
  }

  public sealed class ArpScanCommandNeighborDiscoverer : RunCommandNeighborDiscovererBase {
    public static bool IsSupported { get; }

    public ArpScanCommandNeighborDiscoverer(IPNetworkProfile? networkProfile, IServiceProvider? serviceProvider) {}

    protected override bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToDiscover, out string executable, out string arguments) {}
    protected override bool GetCommandLineArguments(out string executable, out string arguments) {}
  }

  public sealed class IpHlpApiNeighborDiscoverer : INeighborDiscoverer {
    public IpHlpApiNeighborDiscoverer(IPNetworkProfile networkProfile, ILogger? logger = null) {}

    public ValueTask DiscoverAsync(CancellationToken cancellationToken = default) {}
    public async ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken = default) {}
  }

  public sealed class IpHlpApiNeighborTable : INeighborTable {
    public static bool IsSupported { get; }

    public IpHlpApiNeighborTable(IServiceProvider? serviceProvider = null) {}

    [AsyncIteratorStateMachine(typeof(IpHlpApiNeighborTable.<EnumerateEntriesAsync>d__5))]
    public IAsyncEnumerable<NeighborTableEntry> EnumerateEntriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default) {}
  }

  public sealed class NmapCommandNeighborDiscoverer : RunCommandNeighborDiscovererBase {
    public static bool IsSupported { get; }

    public NmapCommandNeighborDiscoverer(IPNetworkProfile networkProfile, IServiceProvider? serviceProvider) {}

    protected override bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToDiscover, out string executable, out string arguments) {}
    protected override bool GetCommandLineArguments(out string executable, out string arguments) {}
  }

  public sealed class NullNeighborDiscoverer : INeighborDiscoverer {
    public static readonly NullNeighborDiscoverer Instance; // = "Smdn.Net.NeighborDiscovery.NullNeighborDiscoverer"

    public ValueTask DiscoverAsync(CancellationToken cancellationToken) {}
    public ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken) {}
  }

  public sealed class ProcfsArpNeighborTable : INeighborTable {
    public static bool IsSupported { get; }

    public ProcfsArpNeighborTable(IServiceProvider? serviceProvider = null) {}

    [AsyncIteratorStateMachine(typeof(ProcfsArpNeighborTable.<EnumerateEntriesAsync>d__6))]
    public IAsyncEnumerable<NeighborTableEntry> EnumerateEntriesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default) {}
  }

  public abstract class RunCommandNeighborDiscovererBase : INeighborDiscoverer {
    protected static string FindPathToCommand(string command, IEnumerable<string> paths) {}

    protected RunCommandNeighborDiscovererBase(ILogger? logger) {}

    public virtual ValueTask DiscoverAsync(CancellationToken cancellationToken) {}
    public virtual ValueTask DiscoverAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken) {}
    protected abstract bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToDiscover, out string executable, out string arguments);
    protected abstract bool GetCommandLineArguments(out string executable, out string arguments);
  }

  public readonly struct NeighborTableEntry :
    IEquatable<IPAddress>,
    IEquatable<PhysicalAddress>
  {
    public NeighborTableEntry(IPAddress ipAddress, PhysicalAddress? physicalAddress, bool isPermanent, NeighborTableEntryState state, int? interfaceIndex = null, string? interfaceName = null) {}

    public IPAddress IPAddress { get; }
    public int? InterfaceIndex { get; }
    public string? InterfaceName { get; }
    public bool IsPermanent { get; }
    public PhysicalAddress? PhysicalAddress { get; }
    public NeighborTableEntryState State { get; }

    public bool Equals(IPAddress? other) {}
    public bool Equals(PhysicalAddress? other) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.2.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.2.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
