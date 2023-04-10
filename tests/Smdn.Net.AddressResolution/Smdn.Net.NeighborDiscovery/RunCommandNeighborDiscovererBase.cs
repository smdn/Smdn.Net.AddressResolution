// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Smdn.OperatingSystem;

namespace Smdn.Net.NeighborDiscovery;

[TestFixture]
public class RunCommandNeighborDiscovererBaseTests {
  public class InterceptingProcessFactory : RunCommandNeighborDiscovererBase.IProcessFactory {
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

  private class ConcreteRunCommandNeighborDiscoverer : RunCommandNeighborDiscovererBase {
    public static string FindCommand(string command, bool expectAsAvailable)
    {
      var comm = RunCommandNeighborDiscovererBase.FindCommand(command, DefaultCommandPaths);

      Assert.AreEqual(command, comm.Name, nameof(comm.Name));
      Assert.AreEqual(expectAsAvailable, comm.IsAvailable, nameof(comm.IsAvailable));

      return comm.GetExecutablePathOrThrow();
    }

    public ConcreteRunCommandNeighborDiscoverer(
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

    protected override bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToDiscover, out string executable, out string? arguments)
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

    var commandExecutablePath = ConcreteRunCommandNeighborDiscoverer.FindCommand(
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
      ConcreteRunCommandNeighborDiscoverer.FindCommand(
        "_non.existent.unavailable.command_",
        expectAsAvailable: false
      );
    });
  }

  private class PseudoRunCommandNeighborDiscoverer : RunCommandNeighborDiscovererBase {
    private readonly string commandExecutablePath;
    private readonly string? commandArguments;
    private readonly Func<IEnumerable<IPAddress>, string> commandAddressArgumentsGenerator;
    private readonly bool performNeighborDiscovery;

    public PseudoRunCommandNeighborDiscoverer(
      string commandExecutablePath,
      string? commandArguments,
      Func<IEnumerable<IPAddress>, string> commandAddressArgumentsGenerator,
      bool performNeighborDiscovery,
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
      this.performNeighborDiscovery = performNeighborDiscovery;
    }

    protected override bool GetCommandLineArguments(out string executable, out string? arguments)
    {
      executable = commandExecutablePath;
      arguments = commandArguments;

      return performNeighborDiscovery;
    }

    protected override bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToDiscover, out string executable, out string? arguments)
    {
      executable = commandExecutablePath;
      arguments = (commandArguments ?? string.Empty) + " " + commandAddressArgumentsGenerator(addressesToDiscover);

      return performNeighborDiscovery;
    }
  }

  [Test]
  public void RunCommandAsync_InvokedByDiscoverAsync()
    => RunCommandAsync(withAddressesParameter: false);

  [Test]
  public void RunCommandAsync_InvokedByDiscoverAsync_WithAddresses()
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

    services.AddSingleton<RunCommandNeighborDiscovererBase.IProcessFactory>(
      new InterceptingProcessFactory(AssertProcessStartInfo)
    );

    using var neighborDiscoverer = new PseudoRunCommandNeighborDiscoverer(
      commandExecutablePath: commandExecutablePath,
      commandArguments: commandArguments,
      commandAddressArgumentsGenerator: GenerateAddressesArgument,
      performNeighborDiscovery: true,
      serviceProvider: services.BuildServiceProvider()
    );

    var ex = Assert.CatchAsync(
      async () => {
        if (withAddressesParameter)
          await neighborDiscoverer.DiscoverAsync(addresses: addresses, cts.Token);
        else
          await neighborDiscoverer.DiscoverAsync(cts.Token);
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

    services.AddSingleton<RunCommandNeighborDiscovererBase.IProcessFactory>(
      new InterceptingProcessFactory(AssertProcessStartInfo)
    );

    using var neighborDiscoverer = new PseudoRunCommandNeighborDiscoverer(
      commandExecutablePath: commandExecutablePath,
      commandArguments: null,
      commandAddressArgumentsGenerator: static _ => throw new NotImplementedException(),
      performNeighborDiscovery: true,
      serviceProvider: services.BuildServiceProvider()
    );

    var ex = Assert.CatchAsync(
      async () => await neighborDiscoverer.DiscoverAsync(cts.Token)
    );

    Assert.That(ex, Is.InstanceOf<OperationCanceledException>().Or.InstanceOf<TaskCanceledException>());
  }

  [Test]
  public void DiscoverAsync_CommandNotInvoked()
    => DiscoverAsync_CommandNotInvoked(withAddressesParameter: false);

  [Test]
  public void DiscoverAsync_WithAddresses_CommandNotInvoked()
    => DiscoverAsync_CommandNotInvoked(withAddressesParameter: true);

  private void DiscoverAsync_CommandNotInvoked(bool withAddressesParameter)
  {
    const string commandExecutablePath = "_non.existent.unavailable.command_";
    const string commandArguments = "--args";

    var services = new ServiceCollection();

    services.AddSingleton<RunCommandNeighborDiscovererBase.IProcessFactory>(
      new InterceptingProcessFactory(
        static _ => Assert.Fail("must not be called")
      )
    );

    using var neighborDiscoverer = new PseudoRunCommandNeighborDiscoverer(
      commandExecutablePath: commandExecutablePath,
      commandArguments: commandArguments,
      commandAddressArgumentsGenerator: static _ => string.Empty,
      performNeighborDiscovery: false,
      serviceProvider: services.BuildServiceProvider()
    );

    if (withAddressesParameter) {
      var addresses = new[] {
        IPAddress.Parse("192.0.2.1"),
        IPAddress.Parse("192.0.2.2"),
        IPAddress.Parse("192.0.2.3")
      };

      Assert.DoesNotThrowAsync(
        async () => await neighborDiscoverer.DiscoverAsync(addresses: addresses)
      );
    }
    else {
      Assert.DoesNotThrowAsync(
        async () => await neighborDiscoverer.DiscoverAsync()
      );
    }
  }
}
