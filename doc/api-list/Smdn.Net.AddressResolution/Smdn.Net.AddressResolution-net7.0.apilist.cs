// Smdn.Net.AddressResolution.dll (Smdn.Net.AddressResolution-1.0.0-rc2)
//   Name: Smdn.Net.AddressResolution
//   AssemblyVersion: 1.0.0.0
//   InformationalVersion: 1.0.0-rc2+3ee5c5d35420506d56f36dcd2bf148727d37aa11
//   TargetFramework: .NETCoreApp,Version=v7.0
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Memory, Version=7.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
//     System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Net.Ping, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
//     Vanara.PInvoke.IpHlpApi, Version=3.4.13.0, Culture=neutral, PublicKeyToken=c37e4080322237fa
//     Vanara.PInvoke.Shared, Version=3.4.13.0, Culture=neutral, PublicKeyToken=c37e4080322237fa
//     Vanara.PInvoke.Ws2_32, Version=3.4.13.0, Culture=neutral, PublicKeyToken=c37e4080322237fa
#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smdn.Net;
using Smdn.Net.AddressResolution;
using Smdn.Net.AddressTables;
using Smdn.Net.NetworkScanning;

namespace Smdn.Net {
  public abstract class IPNetworkProfile {
    public static IPNetworkProfile Create() {}
    public static IPNetworkProfile Create(Func<IEnumerable<IPAddress>?> addressRangeGenerator, NetworkInterface? networkInterface = null) {}
    public static IPNetworkProfile Create(IPAddress baseAddress, IPAddress subnetMask, NetworkInterface? networkInterface = null) {}
    public static IPNetworkProfile Create(IPAddress baseAddress, int prefixLength, NetworkInterface? networkInterface = null) {}
    public static IPNetworkProfile Create(NetworkInterface networkInterface) {}
    public static IPNetworkProfile Create(Predicate<NetworkInterface> predicateForNetworkInterface) {}

    protected IPNetworkProfile(NetworkInterface? networkInterface) {}

    public NetworkInterface? NetworkInterface { get; }

    public abstract IEnumerable<IPAddress>? GetAddressRange();
  }

  public static class PhysicalAddressExtensions {
    public static string ToMacAddressString(this PhysicalAddress hardwareAddress, char delimiter = ':') {}
  }
}

namespace Smdn.Net.AddressResolution {
  public interface IAddressResolver<TAddress, TResolvedAddress> where TAddress : notnull where TResolvedAddress : notnull {
    void Invalidate(TAddress address);
    ValueTask<TResolvedAddress?> ResolveAsync(TAddress address, CancellationToken cancellationToken);
  }

  public class MacAddressResolver : MacAddressResolverBase {
    protected MacAddressResolver(IAddressTable addressTable, bool shouldDisposeAddressTable, INetworkScanner? networkScanner, bool shouldDisposeNetworkScanner, NetworkInterface? networkInterface, int maxParallelCountForRefreshInvalidatedAddresses, ILogger? logger) {}
    public MacAddressResolver() {}
    public MacAddressResolver(IAddressTable? addressTable, INetworkScanner? networkScanner, bool shouldDisposeAddressTable = false, bool shouldDisposeNetworkScanner = false, NetworkInterface? networkInterface = null, int maxParallelCountForRefreshInvalidatedAddresses = 3, IServiceProvider? serviceProvider = null) {}
    public MacAddressResolver(IPNetworkProfile? networkProfile, IServiceProvider? serviceProvider = null) {}

    public bool CanPerformNetworkScan { get; }
    public override bool HasInvalidated { get; }
    public TimeSpan NetworkScanInterval { get; set; }
    public TimeSpan NetworkScanMinInterval { get; set; }

    protected override void Dispose(bool disposing) {}
    public IAsyncEnumerable<AddressTableEntry> EnumerateAddressTableEntriesAsync(CancellationToken cancellationToken = default) {}
    public IAsyncEnumerable<AddressTableEntry> EnumerateAddressTableEntriesAsync(Predicate<AddressTableEntry> predicate, CancellationToken cancellationToken = default) {}
    protected override void InvalidateCore(IPAddress ipAddress) {}
    protected override void InvalidateCore(PhysicalAddress macAddress) {}
    protected override ValueTask RefreshAddressTableAsyncCore(CancellationToken cancellationToken = default) {}
    protected override ValueTask RefreshInvalidatedAddressesAsyncCore(CancellationToken cancellationToken = default) {}
    protected override async ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(IPAddress ipAddress, CancellationToken cancellationToken) {}
    protected override async ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(PhysicalAddress macAddress, CancellationToken cancellationToken) {}
    protected virtual async ValueTask<AddressTableEntry> SelectAddressTableEntryAsync(Predicate<AddressTableEntry> predicate, CancellationToken cancellationToken) {}
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
    public ValueTask RefreshAddressTableAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask RefreshAddressTableAsyncCore(CancellationToken cancellationToken) {}
    public ValueTask RefreshInvalidatedAddressesAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask RefreshInvalidatedAddressesAsyncCore(CancellationToken cancellationToken) {}
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

namespace Smdn.Net.AddressTables {
  public interface IAddressTable : IDisposable {
    IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken);
  }

  public enum AddressTableEntryState : int {
    Delay = 4,
    Incomplete = 1,
    None = 0,
    Probe = 5,
    Reachable = 2,
    Stale = 3,
  }

  public abstract class AddressTable : IAddressTable {
    public static IAddressTable Null { get; }

    public static IAddressTable Create(IServiceProvider? serviceProvider = null) {}

    protected AddressTable(ILogger? logger = null) {}

    protected ILogger? Logger { get; }

    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsync(CancellationToken cancellationToken = default) {}
    protected abstract IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsyncCore(CancellationToken cancellationToken);
    protected void ThrowIfDisposed() {}
  }

  public sealed class IpHlpApiAddressTable : AddressTable {
    public static bool IsSupported { get; }

    public IpHlpApiAddressTable(IServiceProvider? serviceProvider = null) {}

    [AsyncIteratorStateMachine(typeof(IpHlpApiAddressTable.<EnumerateEntriesAsyncCore>d__4))]
    protected override IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsyncCore([EnumeratorCancellation] CancellationToken cancellationToken) {}
  }

  public sealed class ProcfsArpAddressTable : AddressTable {
    public static bool IsSupported { get; }

    public ProcfsArpAddressTable(IServiceProvider? serviceProvider = null) {}

    [AsyncIteratorStateMachine(typeof(ProcfsArpAddressTable.<EnumerateEntriesAsyncCore>d__5))]
    protected override IAsyncEnumerable<AddressTableEntry> EnumerateEntriesAsyncCore([EnumeratorCancellation] CancellationToken cancellationToken) {}
  }

  public readonly struct AddressTableEntry :
    IEquatable<AddressTableEntry>,
    IEquatable<IPAddress>,
    IEquatable<PhysicalAddress>
  {
    public static readonly AddressTableEntry Empty; // = "{IP=, MAC=(null), IsPermanent=False, State=None, Iface=}"

    public static IEqualityComparer<AddressTableEntry> DefaultEqualityComparer { get; }
    public static IEqualityComparer<AddressTableEntry> ExceptStateEqualityComparer { get; }

    public AddressTableEntry(IPAddress ipAddress, PhysicalAddress? physicalAddress, bool isPermanent, AddressTableEntryState state, string? interfaceId) {}

    public IPAddress? IPAddress { get; }
    public string? InterfaceId { get; }
    [MemberNotNullWhen(false, "IPAddress")]
    public bool IsEmpty { [MemberNotNullWhen(false, "IPAddress")] get; }
    public bool IsPermanent { get; }
    public PhysicalAddress? PhysicalAddress { get; }
    public AddressTableEntryState State { get; }

    public bool Equals(AddressTableEntry other) {}
    public bool Equals(IPAddress? other) {}
    public bool Equals(PhysicalAddress? other) {}
    public override bool Equals(object? obj) {}
    public override int GetHashCode() {}
    public override string ToString() {}
  }
}

namespace Smdn.Net.NetworkScanning {
  public interface INetworkScanner : IDisposable {
    ValueTask ScanAsync(CancellationToken cancellationToken);
    ValueTask ScanAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken);
  }

  public sealed class ArpScanCommandNetworkScanner : CommandNetworkScanner {
    public static bool IsSupported { get; }

    public ArpScanCommandNetworkScanner(IPNetworkProfile? networkProfile, IServiceProvider? serviceProvider = null) {}

    protected override bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToScan, out string executable, out string arguments) {}
    protected override bool GetCommandLineArguments(out string executable, out string arguments) {}
  }

  public abstract class CommandNetworkScanner : INetworkScanner {
    public interface IProcessFactory {
      Process CreateProcess(ProcessStartInfo processStartInfo);
    }

    protected readonly struct Command {
      public Command(string name, string? executablePath) {}

      public bool IsAvailable { get; }
      public string Name { get; }

      public string GetExecutablePathOrThrow() {}
    }

    protected static IReadOnlyCollection<string> DefaultCommandPaths { get; }

    protected static CommandNetworkScanner.Command FindCommand(string command, IEnumerable<string> paths) {}

    protected CommandNetworkScanner(ILogger? logger, IServiceProvider? serviceProvider) {}

    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    protected abstract bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToScan, out string executable, out string? arguments);
    protected abstract bool GetCommandLineArguments(out string executable, out string? arguments);
    public virtual ValueTask ScanAsync(CancellationToken cancellationToken = default) {}
    public virtual ValueTask ScanAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken = default) {}
    protected void ThrowIfDisposed() {}
  }

  public sealed class IpHlpApiNetworkScanner : NetworkScanner {
    public static bool IsSupported { get; }

    public IpHlpApiNetworkScanner(IPNetworkProfile networkProfile, IServiceProvider? serviceProvider = null) {}

    protected override async ValueTask ScanAsyncCore(IPAddress address, CancellationToken cancellationToken = default) {}
  }

  public abstract class NetworkScanner : INetworkScanner {
    public static INetworkScanner Null { get; }

    public static INetworkScanner Create(IPNetworkProfile? networkProfile, IServiceProvider? serviceProvider = null) {}

    protected NetworkScanner(IPNetworkProfile networkProfile, ILogger? logger = null) {}

    protected ILogger? Logger { get; }
    protected IPNetworkProfile NetworkProfile { get; }

    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public virtual ValueTask ScanAsync(CancellationToken cancellationToken = default) {}
    public virtual ValueTask ScanAsync(IEnumerable<IPAddress> addresses, CancellationToken cancellationToken = default) {}
    protected virtual ValueTask ScanAsyncCore(IPAddress address, CancellationToken cancellationToken) {}
    protected void ThrowIfDisposed() {}
  }

  public sealed class NmapCommandNetworkScanner : CommandNetworkScanner {
    public static bool IsSupported { get; }

    public NmapCommandNetworkScanner(IPNetworkProfile networkProfile, IServiceProvider? serviceProvider = null) {}

    protected override bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToScan, out string executable, out string arguments) {}
    protected override bool GetCommandLineArguments(out string executable, out string arguments) {}
  }

  public sealed class PingNetworkScanner : NetworkScanner {
    public static bool IsSupported { get; }

    public PingNetworkScanner(IPNetworkProfile networkProfile, IServiceProvider? serviceProvider = null) {}

    protected override void Dispose(bool disposing) {}
    protected override async ValueTask ScanAsyncCore(IPAddress address, CancellationToken cancellationToken = default) {}
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.2.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.2.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
