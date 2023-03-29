using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Smdn.Net.AddressResolution;

internal sealed class NullMacAddressResolver : MacAddressResolverBase {
  public override bool HasInvalidated => false;

  internal NullMacAddressResolver()
    : base(logger: null)
  {
  }

#pragma warning disable CA2215
  // prevents disposing instance by not calling Dispose() of the base class
  protected override void Dispose(bool disposing)
  {
  }
#pragma warning restore CA2215

  protected override ValueTask<PhysicalAddress?> ResolveIPAddressToMacAddressAsyncCore(
    IPAddress ipAddress,
    CancellationToken cancellationToken
  )
    =>
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      ValueTask.FromResult<PhysicalAddress?>(null);
#else
      ValueTaskShim.FromResult<PhysicalAddress?>(null);
#endif

  protected override ValueTask<IPAddress?> ResolveMacAddressToIPAddressAsyncCore(
    PhysicalAddress macAddress,
    CancellationToken cancellationToken
  )
    =>
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      ValueTask.FromResult<IPAddress?>(null);
#else
      ValueTaskShim.FromResult<IPAddress?>(null);
#endif

  protected override void InvalidateCore(IPAddress ipAddress)
  {
    // do nothing
  }

  protected override void InvalidateCore(PhysicalAddress macAddress)
  {
    // do nothing
  }
}
