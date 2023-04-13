// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.OperatingSystem;

namespace Smdn.Net.NeighborDiscovery;

[TestFixture]
public class RunCommandNetworkScannerBaseTests {
  public class InterceptingProcessFactory : RunCommandNetworkScannerBase.IProcessFactory {
    private readonly Action<ProcessStartInfo> actionBeforeCreateProcess;

    public InterceptingProcessFactory(
      Action<ProcessStartInfo> actionBeforeCreateProcess
    )
    {
      this.actionBeforeCreateProcess = actionBeforeCreateProcess;
    }

    public Process CreateProcess(ProcessStartInfo processStartInfo)
    {
      actionBeforeCreateProcess(processStartInfo);

      return new() { StartInfo = processStartInfo };
    }
  }

  private class ConcreteRunCommandNetworkScanner : RunCommandNetworkScannerBase {
    public static string FindCommand(string command, bool expectAsAvailable)
    {
      var comm = RunCommandNetworkScannerBase.FindCommand(command, DefaultCommandPaths);

      Assert.AreEqual(command, comm.Name, nameof(comm.Name));
      Assert.AreEqual(expectAsAvailable, comm.IsAvailable, nameof(comm.IsAvailable));

      return comm.GetExecutablePathOrThrow();
    }

    public ConcreteRunCommandNetworkScanner(
      IServiceProvider? serviceProvider = null
    )
      : base(
        logger: null,
        serviceProvider: serviceProvider
      )
    {
    }

    protected override bool GetCommandLineArguments(out string executable, out string? arguments)
      => throw new NotImplementedException();

    protected override bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToScan, out string executable, out string? arguments)
      => throw new NotImplementedException();
  }

  [Test]
  public void FindCommand()
  {
    var pathToNsLookup = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
      ? Shell.Execute("where nslookup")
      : Shell.Execute("which nslookup");

    pathToNsLookup = pathToNsLookup?.Trim();

    if (string.IsNullOrEmpty(pathToNsLookup) || !File.Exists(pathToNsLookup)) {
      Assert.Ignore("cannot test: nslookup is unavailable on this system.");
      return;
    }

    var commandExecutablePath = ConcreteRunCommandNetworkScanner.FindCommand(
      "nslookup",
      expectAsAvailable: true
    );

    FileAssert.Exists(commandExecutablePath);

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      StringAssert.AreEqualIgnoringCase(commandExecutablePath, pathToNsLookup);
    else
      Assert.AreEqual(commandExecutablePath, pathToNsLookup);

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
      StringAssert.EndsWith(".exe", commandExecutablePath);
    else
      StringAssert.DoesNotEndWith(".exe", commandExecutablePath);
  }

  [Test]
  public void FindCommand_CommandNotAvailable()
  {
    Assert.Throws<NotSupportedException>(() => {
      ConcreteRunCommandNetworkScanner.FindCommand(
        "_non.existent.unavailable.command_",
        expectAsAvailable: false
      );
    });
  }

  private class PseudoRunCommandNetworkScanner : RunCommandNetworkScannerBase {
    private readonly string commandExecutablePath;
    private readonly string? commandArguments;
    private readonly Func<IEnumerable<IPAddress>, string> commandAddressArgumentsGenerator;
    private readonly bool performNetworkScan;

    public PseudoRunCommandNetworkScanner(
      string commandExecutablePath,
      string? commandArguments,
      Func<IEnumerable<IPAddress>, string> commandAddressArgumentsGenerator,
      bool performNetworkScan,
      IServiceProvider serviceProvider
    )
      : base(
        logger: null,
        serviceProvider: serviceProvider
      )
    {
      this.commandExecutablePath = commandExecutablePath;
      this.commandArguments = commandArguments;
      this.commandAddressArgumentsGenerator = commandAddressArgumentsGenerator;
      this.performNetworkScan = performNetworkScan;
    }

    protected override bool GetCommandLineArguments(out string executable, out string? arguments)
    {
      executable = commandExecutablePath;
      arguments = commandArguments;

      return performNetworkScan;
    }

    protected override bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToScan, out string executable, out string? arguments)
    {
      executable = commandExecutablePath;
      arguments = (commandArguments ?? string.Empty) + " " + commandAddressArgumentsGenerator(addressesToScan);

      return performNetworkScan;
    }
  }

  [Test]
  public void RunCommandAsync_InvokedByScanAsync()
    => RunCommandAsync(withAddressesParameter: false);

  [Test]
  public void RunCommandAsync_InvokedByScanAsync_WithAddresses()
    => RunCommandAsync(withAddressesParameter: true);

  private void RunCommandAsync(bool withAddressesParameter)
  {
    const string commandExecutablePath = "/bin/command";
    const string commandArguments = "--args";

    using var cts = new CancellationTokenSource();
    var addresses = new[] {
      IPAddress.Parse("192.0.2.1"),
      IPAddress.Parse("192.0.2.2"),
      IPAddress.Parse("192.0.2.3")
    };

    string GenerateAddressesArgument(IEnumerable<IPAddress> addresses)
      => string.Join(" ", addresses);

    void AssertProcessStartInfo(ProcessStartInfo psi)
    {
      Assert.AreEqual(commandExecutablePath, psi.FileName, nameof(psi.FileName));

      var expectedArguments = withAddressesParameter
        ? commandArguments +" " + GenerateAddressesArgument(addresses)
        : commandArguments;

      Assert.AreEqual(expectedArguments, psi.Arguments, nameof(psi.Arguments));

      Assert.IsTrue(psi.RedirectStandardOutput, nameof(psi.RedirectStandardOutput));
      Assert.IsTrue(psi.RedirectStandardError, nameof(psi.RedirectStandardError));

      Assert.IsFalse(psi.UseShellExecute, nameof(psi.UseShellExecute));

      // cancel the subsequent process starting
      cts.Cancel();
    }

    var services = new ServiceCollection();

    services.AddSingleton<RunCommandNetworkScannerBase.IProcessFactory>(
      new InterceptingProcessFactory(AssertProcessStartInfo)
    );

    using var networkScanner = new PseudoRunCommandNetworkScanner(
      commandExecutablePath: commandExecutablePath,
      commandArguments: commandArguments,
      commandAddressArgumentsGenerator: GenerateAddressesArgument,
      performNetworkScan: true,
      serviceProvider: services.BuildServiceProvider()
    );

    var ex = Assert.CatchAsync(
      async () => {
        if (withAddressesParameter)
          await networkScanner.ScanAsync(addresses: addresses, cts.Token);
        else
          await networkScanner.ScanAsync(cts.Token);
      }
    );

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  [Test]
  public void RunCommandAsync_CommandArgumentsNull()
  {
    const string commandExecutablePath = "/bin/command";

    using var cts = new CancellationTokenSource();

    void AssertProcessStartInfo(ProcessStartInfo psi)
    {
      Assert.AreEqual(commandExecutablePath, psi.FileName, nameof(psi.FileName));
      Assert.AreEqual(string.Empty, psi.Arguments, nameof(psi.Arguments));

      // cancel the subsequent process starting
      cts.Cancel();
    }

    var services = new ServiceCollection();

    services.AddSingleton<RunCommandNetworkScannerBase.IProcessFactory>(
      new InterceptingProcessFactory(AssertProcessStartInfo)
    );

    using var networkScanner = new PseudoRunCommandNetworkScanner(
      commandExecutablePath: commandExecutablePath,
      commandArguments: null,
      commandAddressArgumentsGenerator: static _ => throw new NotImplementedException(),
      performNetworkScan: true,
      serviceProvider: services.BuildServiceProvider()
    );

    var ex = Assert.CatchAsync(
      async () => await networkScanner.ScanAsync(cts.Token)
    );

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  [Test]
  public void ScanAsync_CommandNotInvoked()
    => ScanAsync_CommandNotInvoked(withAddressesParameter: false);

  [Test]
  public void ScanAsync_WithAddresses_CommandNotInvoked()
    => ScanAsync_CommandNotInvoked(withAddressesParameter: true);

  private void ScanAsync_CommandNotInvoked(bool withAddressesParameter)
  {
    const string commandExecutablePath = "_non.existent.unavailable.command_";
    const string commandArguments = "--args";

    var services = new ServiceCollection();

    services.AddSingleton<RunCommandNetworkScannerBase.IProcessFactory>(
      new InterceptingProcessFactory(
        static _ => Assert.Fail("must not be called")
      )
    );

    using var networkScanner = new PseudoRunCommandNetworkScanner(
      commandExecutablePath: commandExecutablePath,
      commandArguments: commandArguments,
      commandAddressArgumentsGenerator: static _ => string.Empty,
      performNetworkScan: false,
      serviceProvider: services.BuildServiceProvider()
    );

    if (withAddressesParameter) {
      var addresses = new[] {
        IPAddress.Parse("192.0.2.1"),
        IPAddress.Parse("192.0.2.2"),
        IPAddress.Parse("192.0.2.3")
      };

      Assert.DoesNotThrowAsync(
        async () => await networkScanner.ScanAsync(addresses: addresses)
      );
    }
    else {
      Assert.DoesNotThrowAsync(
        async () => await networkScanner.ScanAsync()
      );
    }
  }
}