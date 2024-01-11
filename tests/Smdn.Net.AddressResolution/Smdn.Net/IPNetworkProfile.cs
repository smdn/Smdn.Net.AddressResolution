// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;

using NUnit.Framework;

namespace Smdn.Net;

[TestFixture]
public partial class IPNetworkProfileTests {
#if SYSTEM_NET_IPNETWORK
  private static System.Collections.IEnumerable YieldTestCases_Create_FromIPNetwork_IPv4()
  {
    yield return new object[] {
      IPNetwork.Parse("192.168.0.0/24"),
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(254));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.254")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.255")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.1.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("127.0.0.1")));
      })
    };

    yield return new object[] {
      IPNetwork.Parse("192.168.0.0/30"),
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(2));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.2")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.3")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("127.0.0.1")));
      })
    };

    yield return new object[] {
      IPNetwork.Parse("192.168.0.0/20"),
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(4094));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.255")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.15.0")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.15.254")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.15.255")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("127.0.0.1")));
      })
    };

    yield return new object[] {
      IPNetwork.Parse("192.168.0.0/32"),
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(1));

        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.0")));
      })
    };
  }

  [TestCaseSource(nameof(YieldTestCases_Create_FromIPNetwork_IPv4))]
  public void Create_FromIPNetwork_IPv4(
    IPNetwork ipNetwork,
    Action<IReadOnlyList<IPAddress>> assertAddresses
  )
  {
    var profile = IPNetworkProfile.Create(ipNetwork);

    Assert.That(profile.NetworkInterface, Is.Null, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange()?.ToList();

    Assert.That(addresses, Is.Not.Null, nameof(addresses));

    assertAddresses(addresses!);
  }

  private static System.Collections.IEnumerable YieldTestCases_Create_FromIPNetwork_IPv6()
  {
    yield return new object?[] { IPNetwork.Parse("2001:db8::/32") };
    yield return new object?[] { IPNetwork.Parse("2001:db8:3c4d::/48") };
    yield return new object?[] { IPNetwork.Parse("2001:db8:3c4d:15::/64") };
  }

  [TestCaseSource(nameof(YieldTestCases_Create_FromIPNetwork_IPv6))]
  public void Create_FromIPNetwork_IPv6(IPNetwork network)
  {
    Assert.Throws<NotImplementedException>(
      () => IPNetworkProfile.Create(network: network)
    );
  }
#endif

  private static System.Collections.IEnumerable YieldTestCases_Create_FromBaseAddressAndPrefixLength_IPv4()
  {
    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      24,
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(254));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.254")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.255")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.1.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("127.0.0.1")));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      30,
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(2));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.2")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.3")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("127.0.0.1")));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      20,
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(4094));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.255")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.15.0")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.15.254")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.15.255")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("127.0.0.1")));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      32,
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(1));

        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
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

    Assert.That(profile.NetworkInterface, Is.Null, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange()?.ToList();

    Assert.That(addresses, Is.Not.Null, nameof(addresses));

    assertAddresses(addresses!);
  }

  private static System.Collections.IEnumerable YieldTestCases_Create_FromBaseAddressAndPrefixLength_IPv6()
  {
    yield return new object?[] { IPAddress.Parse("2001:db8::"), 32 };
    yield return new object?[] { IPAddress.Parse("2001:db8:3c4d::"), 48 };
    yield return new object?[] { IPAddress.Parse("2001:db8:3c4d:15::"), 64 };
  }

  [TestCaseSource(nameof(YieldTestCases_Create_FromBaseAddressAndPrefixLength_IPv6))]
  public void Create_FromBaseAddressAndPrefixLength_IPv6(
    IPAddress baseAddress,
    int prefixLength
  )
  {
    Assert.Throws<NotImplementedException>(
      () => IPNetworkProfile.Create(
        baseAddress: baseAddress,
        prefixLength: prefixLength
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
        Assert.That(addresses.Count, Is.EqualTo(254));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.254")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.255")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.1.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("127.0.0.1")));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      IPAddress.Parse("255.255.255.252"), // /30
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(2));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.2")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.3")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("127.0.0.1")));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      IPAddress.Parse("255.255.240.0"), // /20
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(4094));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.255")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.15.0")));
        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.15.254")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.0.0")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("192.168.15.255")));
        Assert.That(addresses, Has.No.Member(IPAddress.Parse("127.0.0.1")));
      })
    };

    yield return new object[] {
      IPAddress.Parse("192.168.0.1"),
      IPAddress.Parse("255.255.255.255"), // /32
      new Action<IReadOnlyList<IPAddress>>(static addresses => {
        Assert.That(addresses.Count, Is.EqualTo(1));

        Assert.That(addresses, Has.Member(IPAddress.Parse("192.168.0.1")));
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

    Assert.That(profile.NetworkInterface, Is.Null, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange()?.ToList();

    Assert.That(addresses, Is.Not.Null, nameof(addresses));

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

    Assert.That(profile.NetworkInterface, Is.Not.Null, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange();

    Assert.That(addresses, Is.Not.Null, nameof(addresses));
    Assert.That(addresses, Is.Not.Empty, nameof(addresses));
  }

  [Test]
  public void CreateFromNetworkInterface_String_ArgumentNull()
    => Assert.Throws<ArgumentNullException>(
      () => IPNetworkProfile.CreateFromNetworkInterface(id: (string)null!)
    );

  [Test]
  public void CreateFromNetworkInterface_String()
    => Assert.Throws<InvalidOperationException>(
      () => IPNetworkProfile.CreateFromNetworkInterface(id: "<!non-existent-network-interface-id!>")
    );

  [Test]
  public void CreateFromNetworkInterface_Guid()
    => Assert.Throws<InvalidOperationException>(
      () => IPNetworkProfile.CreateFromNetworkInterface(id: Guid.Empty)
    );

  [Test]
  public void CreateFromNetworkInterface_PhysicalAddress_ArgumentNull()
    => Assert.Throws<ArgumentNullException>(
      () => IPNetworkProfile.CreateFromNetworkInterface(physicalAddress: null!)
    );

  [Test]
  public void CreateFromNetworkInterface_PhysicalAddress()
    => Assert.Throws<InvalidOperationException>(
      () => IPNetworkProfile.CreateFromNetworkInterface(physicalAddress: PhysicalAddress.Parse("00:00:5E:00:53:00"))
    );

  [Test]
  public void CreateFromNetworkInterfaceName_ArgumentNull()
    => Assert.Throws<ArgumentNullException>(
      () => IPNetworkProfile.CreateFromNetworkInterfaceName(name: null!)
    );

  [Test]
  public void CreateFromNetworkInterfaceName()
    => Assert.Throws<InvalidOperationException>(
      () => IPNetworkProfile.CreateFromNetworkInterfaceName(name: "<!non-existent-network-interface-name!>")
    );

  [Test]
  public void Create_PredicateOfNetworkInterface()
  {
    int numberOfNetworkInterfaces;

    try {
      numberOfNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces().Length;
    }
    catch {
      numberOfNetworkInterfaces = 0;
    }

    if (numberOfNetworkInterfaces == 0) {
      Assert.Ignore($"can not test ({nameof(numberOfNetworkInterfaces)} == 0)");
      return;
    }

    int numberOfEnumeratedNetworkInterfaces = 0;

    try {
      Assert.That(
        IPNetworkProfile.Create(predicateForNetworkInterface: _ => {
          numberOfEnumeratedNetworkInterfaces++;
          return true;
        }),
        Is.Not.Null
      );
    }
    catch (InvalidOperationException) when (numberOfEnumeratedNetworkInterfaces == 0) {
      // expected exception (no interface enumerated)
    }
  }

  [Test]
  public void Create_PredicateOfNetworkInterface_ArgumentNull()
    => Assert.That(() => IPNetworkProfile.Create(predicateForNetworkInterface: null!), Throws.TypeOf<ArgumentNullException>());

  [Test]
  public void Create_PredicateOfNetworkInterface_ExceptionThrownByPredicate()
  {
    int numberOfNetworkInterfaces;

    try {
      numberOfNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces().Length;
    }
    catch {
      numberOfNetworkInterfaces = 0;
    }

    if (numberOfNetworkInterfaces == 0) {
      Assert.Ignore($"can not test ({nameof(numberOfNetworkInterfaces)} == 0)");
      return;
    }

    int numberOfEnumeratedNetworkInterfaces = 0;
    string? enumeratedNetworkInterfaceName = null;

    try {
      Assert.That(
        IPNetworkProfile.Create(predicateForNetworkInterface: iface => {
          numberOfEnumeratedNetworkInterfaces++;
          enumeratedNetworkInterfaceName = iface.Name;
          throw new NotSupportedException();
        }),
        Is.Not.Null
      );
    }
    catch (InvalidOperationException) when (numberOfEnumeratedNetworkInterfaces == 0) {
      // expected exception (no interface enumerated)
    }
    catch (InvalidOperationException ex) when (0 < numberOfEnumeratedNetworkInterfaces) {
      Assert.That(ex.Message, Does.Contain(enumeratedNetworkInterfaceName!));

      Assert.That(ex.InnerException, Is.Not.Null);
      Assert.That(ex.InnerException, Is.TypeOf<NotSupportedException>());
    }
  }

  [Test]
  public void Create_FromAddressRangeGenerator()
  {
    const int Count = 4;

    static IEnumerable<IPAddress> GenerateAddressRange()
      => Enumerable.Range(0, Count).Select(static d => new IPAddress(new byte[] { 192, 168, 2, (byte)d }));

    var profile = IPNetworkProfile.Create(addressRangeGenerator: GenerateAddressRange);

    Assert.That(profile.NetworkInterface, Is.Null, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange()?.ToList();

    Assert.That(addresses, Is.Not.Null, nameof(addresses));
    Assert.That(addresses, Is.Not.Empty, nameof(addresses));
    Assert.That(addresses!.Count, Is.EqualTo(Count), nameof(addresses.Count));
  }

  [Test]
  public void Create_FromAddressRangeGenerator_NullGenerator()
  {
    static IEnumerable<IPAddress>? GenerateNullAddressRange() => null;

    var profile = IPNetworkProfile.Create(addressRangeGenerator: GenerateNullAddressRange);

    Assert.That(profile.NetworkInterface, Is.Null, nameof(profile.NetworkInterface));

    var addresses = profile.GetAddressRange();

    Assert.That(addresses, Is.Null, nameof(addresses));
  }

  [Test]
  public void Create_FromAddressRangeGenerator_ArgumentNull()
    => Assert.Throws<ArgumentNullException>(static () => IPNetworkProfile.Create(addressRangeGenerator: null!));
}
