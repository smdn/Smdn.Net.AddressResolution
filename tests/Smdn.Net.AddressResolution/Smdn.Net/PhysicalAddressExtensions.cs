// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System.Net.NetworkInformation;

using NUnit.Framework;

namespace Smdn.Net;

[TestFixture]
public partial class PhysicalAddressExtensionsTests {
  private static readonly PhysicalAddress TestMacAddress = PhysicalAddress.Parse("00:00:5E:00:53:00");

  [Test]
  public static void ToMacAddressString()
    => Assert.That(TestMacAddress.ToMacAddressString(), Is.EqualTo("00:00:5E:00:53:00"));

  [Test]
  public static void ToMacAddressString_ArgumentNull(
    [Values(':', '-', '\0')] char delimiter
  )
  {
    PhysicalAddress? nullMacAddress = null!;

    Assert.That(() => nullMacAddress!.ToMacAddressString(delimiter: delimiter), Throws.ArgumentNullException);
  }

  [TestCase(':', "00:00:5E:00:53:00")]
  [TestCase('-', "00-00-5E-00-53-00")]
  [TestCase('\0', "00005E005300")]
  public static void ToMacAddressString(char delimiter, string expected)
    => Assert.That(TestMacAddress.ToMacAddressString(delimiter: delimiter), Is.EqualTo(expected));
}
