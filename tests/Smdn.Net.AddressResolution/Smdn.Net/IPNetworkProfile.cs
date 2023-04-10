// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if NET8_0_OR_GREATER
// #define SYSTEM_NET_IPNETWORK
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using NUnit.Framework;

namespace Smdn.Net;

[TestFixture]
public partial class IPNetworkProfileTests {
#if SYSTEM_NET_IPNETWORK
  [Test]
  public void Create_FromIPNetwork_IPv4()
  {
    Assert.Fail("test not implemented");
  }

  [Test]
  public void Create_FromIPNetwork_IPv6()
  {
    Assert.Fail("test not implemented");
  }
#endif

  private static System.Collections.IEnumerable YieldTestCases_Create_FromBaseAddressAndPrefixLength_IPv4()
  {
    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      24,
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.AreEqual(255, addresses.Count);
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.1"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.255"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.0.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.1.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("127.0.0.1"));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      30,
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.AreEqual(3, addresses.Count);
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.1"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.3"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.0.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.0.4"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("127.0.0.1"));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      20,
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.AreEqual(4095, addresses.Count);
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.1"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.255"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.15.0"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.15.255"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.0.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.16.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("127.0.0.1"));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      32,
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.AreEqual(1, addresses.Count);

        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.1"));
      })
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Create_FromBaseAddressAndPrefixLength_IPv4))]
  public void Create_FromBaseAddressAndPrefixLength_IPv4(
    IPAddress baseAddress,
    int prefixLength,
    Action<IReadOnlyList<IPAddress>> assertAddresses
  )
  {
    var profile = IPNetworkProfile.Create(
      baseAddress: baseAddress,
      prefixLength: prefixLength
    );

    Assert.IsNull(profile.NetworkInterface, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange()?.ToList();

    Assert.IsNotNull(addresses, nameof(addresses));

    assertAddresses(addresses!);
  }

  [Test]
  public void Create_FromBaseAddressAndPrefixLength_IPv6()
  {
    Assert.Throws<NotImplementedException>(
      () => IPNetworkProfile.Create(
        baseAddress: IPAddress.IPv6Loopback,
        prefixLength: 8
      )
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Create_FromBaseAddressAndPrefixLength_ArgumentException()
  {
    yield return new object?[] { null, 24, typeof(ArgumentNullException) };
    yield return new object?[] { IPAddress.Any, 0, typeof(ArgumentOutOfRangeException) };
    yield return new object?[] { IPAddress.Any, 33, typeof(ArgumentOutOfRangeException) };
  }

  [TestCaseSource(nameof(YieldTestCases_Create_FromBaseAddressAndPrefixLength_ArgumentException))]
  public void Create_FromBaseAddressAndPrefixLength_ArgumentException(
    IPAddress? baseAddress,
    int prefixLength,
    Type typeOfExpectedException
  )
  {
    Assert.Throws(
      typeOfExpectedException,
      () => IPNetworkProfile.Create(
        baseAddress: baseAddress!,
        prefixLength: prefixLength
      )
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Create_FromBaseAddressAndSubnetMask_IPv4()
  {
    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      IPAddress.Parse("255.255.255.0"), // /24
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.AreEqual(255, addresses.Count);
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.1"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.255"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.0.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.1.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("127.0.0.1"));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      IPAddress.Parse("255.255.255.252"), // /30
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.AreEqual(3, addresses.Count);
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.1"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.3"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.0.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.0.4"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("127.0.0.1"));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      IPAddress.Parse("255.255.240.0"), // /20
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.AreEqual(4095, addresses.Count);
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.1"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.255"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.15.0"));
        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.15.255"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.0.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("192.168.16.0"));
        CollectionAssert.DoesNotContain(addresses, IPAddress.Parse("127.0.0.1"));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      IPAddress.Parse("255.255.255.255"), // /32
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.AreEqual(1, addresses.Count);

        CollectionAssert.Contains(addresses, IPAddress.Parse("192.168.0.1"));
      })
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Create_FromBaseAddressAndSubnetMask_IPv4))]
  public void Create_FromBaseAddressAndSubnetMask_IPv4(
    IPAddress baseAddress,
    IPAddress subnetMask,
    Action<IReadOnlyList<IPAddress>> assertAddresses
  )
  {
    var profile = IPNetworkProfile.Create(
      baseAddress: baseAddress,
      subnetMask: subnetMask
    );

    Assert.IsNull(profile.NetworkInterface, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange()?.ToList();

    Assert.IsNotNull(addresses, nameof(addresses));

    assertAddresses(addresses!);
  }

  [Test]
  public void Create_FromBaseAddressAndSubnetMask_IPv6()
  {
    Assert.Throws<NotImplementedException>(
      () => IPNetworkProfile.Create(
        baseAddress: IPAddress.IPv6Loopback,
        subnetMask: IPAddress.IPv6Any
      )
    );
  }

  private static System.Collections.IEnumerable YieldTestCases_Create_FromBaseAddressAndSubnetMask_ArgumentException()
  {
    yield return new object?[] { null, IPAddress.Any, typeof(ArgumentNullException) };
    yield return new object?[] { IPAddress.Any, null, typeof(ArgumentNullException) };
  }

  [TestCaseSource(nameof(YieldTestCases_Create_FromBaseAddressAndSubnetMask_ArgumentException))]
  public void Create_FromBaseAddressAndSubnetMask_ArgumentException(
    IPAddress? baseAddress,
    IPAddress? subnetMask,
    Type typeOfExpectedException
  )
  {
    Assert.Throws(
      typeOfExpectedException,
      () => IPNetworkProfile.Create(
        baseAddress: baseAddress!,
        subnetMask: subnetMask!
      )
    );
  }

  [Test]
  public void Create_FromNetworkInterface()
  {
    IPNetworkProfile profile;

    try {
      profile = IPNetworkProfile.Create();
    }
    catch (InvalidOperationException) {
      Assert.Inconclusive("could not create IPNetworkProfile");
      return;
    }

    Assert.IsNotNull(profile.NetworkInterface, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange();

    Assert.IsNotNull(addresses, nameof(addresses));
    CollectionAssert.IsNotEmpty(addresses, nameof(addresses));
  }

  [Test]
  public void Create_FromAddressRangeGenerator()
  {
    const int count = 4;

    static IEnumerable<IPAddress> GenerateAddressRange()
      => Enumerable.Range(0, count).Select(static d => new IPAddress(new byte[] { 192, 168, 2, (byte)d }));

    var profile = IPNetworkProfile.Create(addressRangeGenerator: GenerateAddressRange);

    Assert.IsNull(profile.NetworkInterface, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange()?.ToList();

    Assert.IsNotNull(addresses, nameof(addresses));
    CollectionAssert.IsNotEmpty(addresses, nameof(addresses));
    Assert.AreEqual(addresses!.Count, count, nameof(addresses.Count));
  }

  [Test]
  public void Create_FromAddressRangeGenerator_NullGenerator()
  {
    static IEnumerable<IPAddress>? GenerateNullAddressRange() => null;

    var profile = IPNetworkProfile.Create(addressRangeGenerator: GenerateNullAddressRange);

    Assert.IsNull(profile.NetworkInterface, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange();

    Assert.IsNull(addresses, nameof(addresses));
  }

  [Test]
  public void Create_FromAddressRangeGenerator_ArgumentNull()
    => Assert.Throws<ArgumentNullException>(static () => IPNetworkProfile.Create(addressRangeGenerator: null!));
}
