// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Net.NetworkInformation;
using NUnit.Framework;

namespace Smdn.Net;

[TestFixture]
public partial class PhysicalAddressExtensionsTests {
  private static readonly PhysicalAddress TestMacAddress = PhysicalAddress.Parse("00:00:5E:00:53:00");

  [Test]
  public static void ToMacAddressString()
    => Assert.AreEqual("00:00:5E:00:53:00", TestMacAddress.ToMacAddressString());

  [Test]
  public static void ToMacAddressString_ArgumentNull(
    [Values(':', '-', '\0')] char delimiter
  )
  {
    PhysicalAddress? nullMacAddress = null!;

    Assert.Throws<ArgumentNullException>(() => nullMacAddress!.ToMacAddressString(delimiter: delimiter));
  }

  [TestCase(':', "00:00:5E:00:53:00")]
  [TestCase('-', "00-00-5E-00-53-00")]
  [TestCase('\0', "00005E005300")]
  public static void ToMacAddressString(char delimiter, string expected)
    => Assert.AreEqual(expected, TestMacAddress.ToMacAddressString(delimiter: delimiter));
}
