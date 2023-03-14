// Smdn.Net.AddressResolution.dll (Smdn.Net.AddressResolution-1.0.0-preview4)
//   Name: Smdn.Net.AddressResolution
//   AssemblyVersion: 1.0.0.0
//   InformationalVersion: 1.0.0-preview4+f17248a683dc95a5f8a1d3f3ec79fb49b8b2852f
//   TargetFramework: .NETStandard,Version=v2.1
//   Configuration: Release
//   Referenced assemblies:
//     Microsoft.Extensions.DependencyInjection.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     Microsoft.Extensions.Logging.Abstractions, Version=6.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60
//     netstandard, Version=2.1.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
#nullable enable annotations

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smdn.Net.AddressResolution;

namespace Smdn.Net {
  public static class PhysicalAddressExtensions {
    public static string ToMacAddressString(this PhysicalAddress hardwareAddress, char delimiter = ':') {}
  }
}

namespace Smdn.Net.AddressResolution {
  public interface IAddressResolver<TAddress, TResolvedAddress> {
    void Invalidate(TResolvedAddress resolvedAddress);
    ValueTask<TResolvedAddress> ResolveAsync(TAddress address, CancellationToken cancellationToken);
  }

  public abstract class MacAddressResolver :
    IAddressResolver<IPAddress, PhysicalAddress>,
    IAddressResolver<PhysicalAddress, IPAddress>,
    IDisposable
  {
    protected static readonly PhysicalAddress AllZeroMacAddress; // = "000000000000"

    public static MacAddressResolver Null { get; }

    public static MacAddressResolver Create(MacAddressResolverOptions? options = null, IServiceProvider? serviceProvider = null) {}

    protected MacAddressResolver(ILogger? logger = null) {}

    public abstract bool HasInvalidated { get; }
    protected ILogger? Logger { get; }

    protected virtual void Dispose(bool disposing) {}
    public void Dispose() {}
    public void Invalidate(IPAddress resolvedIPAddress) {}
    public void Invalidate(PhysicalAddress resolvedMacAddress) {}
    protected abstract void InvalidateCore(IPAddress resolvedIPAddress);
    protected abstract void InvalidateCore(PhysicalAddress resolvedMacAddress);
    public ValueTask RefreshCacheAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask RefreshCacheAsyncCore(CancellationToken cancellationToken) {}
    public ValueTask RefreshInvalidatedCacheAsync(CancellationToken cancellationToken = default) {}
    protected virtual ValueTask RefreshInvalidatedCacheAsyncCore(CancellationToken cancellationToken) {}
    public ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsync(IPAddress ipAddress, CancellationToken cancellationToken = default) {}
    protected abstract ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(IPAddress ipAddress, CancellationToken cancellationToken);
    public ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsync(PhysicalAddress macAddress, CancellationToken cancellationToken = default) {}
    protected abstract ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(PhysicalAddress macAddress, CancellationToken cancellationToken);
    void IAddressResolver<IPAddress, PhysicalAddress>.Invalidate(PhysicalAddress resolvedAddress) {}
    ValueTask<PhysicalAddress?> IAddressResolver<IPAddress, PhysicalAddress>.ResolveAsync(IPAddress address, CancellationToken cancellationToken) {}
    void IAddressResolver<PhysicalAddress, IPAddress>.Invalidate(IPAddress resolvedAddress) {}
    ValueTask<IPAddress?> IAddressResolver<PhysicalAddress, IPAddress>.ResolveAsync(PhysicalAddress address, CancellationToken cancellationToken) {}
    protected void ThrowIfDisposed() {}
  }

  public sealed class MacAddressResolverOptions {
    public static readonly MacAddressResolverOptions Default; // = "Smdn.Net.AddressResolution.MacAddressResolverOptions"

    public MacAddressResolverOptions() {}

    public string? NmapTargetSpecification { get; init; }
    public TimeSpan ProcfsArpFullScanInterval { get; init; }
  }
}
// API list generated by Smdn.Reflection.ReverseGenerating.ListApi.MSBuild.Tasks v1.2.1.0.
// Smdn.Reflection.ReverseGenerating.ListApi.Core v1.2.0.0 (https://github.com/smdn/Smdn.Reflection.ReverseGenerating)
