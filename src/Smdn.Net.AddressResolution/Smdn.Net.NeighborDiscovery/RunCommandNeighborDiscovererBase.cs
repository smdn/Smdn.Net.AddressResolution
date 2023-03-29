// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#if NET7_0_OR_GREATER
#define SYSTEM_IO_STREAMREADER_READLINEASYNC_CANCELLATIONTOKEN
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.NeighborDiscovery;

public abstract class RunCommandNeighborDiscovererBase : INeighborDiscoverer {
  protected static string FindPathToCommand(string command, IEnumerable<string> paths)
  {
    if (command is null)
      throw new ArgumentNullException(nameof(command));
    if (paths is null)
      throw new ArgumentNullException(nameof(paths));

    return paths
      .Select(path => Path.Combine(path, command))
      .FirstOrDefault(static pathToCommand => File.Exists(pathToCommand))
      ?? throw new NotSupportedException($"'{command}' is not available.");
  }

  /*
   * instance members
   */
  private readonly ILogger? logger;

  protected RunCommandNeighborDiscovererBase(ILogger? logger)
  {
    this.logger = logger;
  }

  protected abstract bool GetCommandLineArguments(out string executable, out string arguments);
  protected abstract bool GetCommandLineArguments(IEnumerable<IPAddress> addressesToDiscover, out string executable, out string arguments);

  public virtual ValueTask DiscoverAsync(CancellationToken cancellationToken)
  {
    if (GetCommandLineArguments(out var executable, out var args)) {
      return RunCommandAsync(
        commandFileName: executable,
        commandArguments: args,
        logger: logger,
        cancellationToken: cancellationToken
      );
    }

    return default; // do nothing
  }

  public virtual ValueTask DiscoverAsync(
    IEnumerable<IPAddress> addresses,
    CancellationToken cancellationToken
  )
  {
    if (GetCommandLineArguments(addresses, out var executable, out var args)) {
      return RunCommandAsync(
        commandFileName: executable,
        commandArguments: args,
        logger: logger,
        cancellationToken: cancellationToken
      );
    }

    return default; // do nothing
  }

  private static async ValueTask RunCommandAsync(
    string commandFileName,
    string commandArguments,
    ILogger? logger,
    CancellationToken cancellationToken
  )
  {
    var commandProcessStartInfo = new ProcessStartInfo() {
      FileName = commandFileName,
      Arguments = commandArguments,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
    };

    logger?.LogDebug(
      "{ProcessStartInfoFileName} {ProcessStartInfoArguments}",
      commandProcessStartInfo.FileName,
      commandProcessStartInfo.Arguments
    );

    using var commandProcess = new Process() {
      StartInfo = commandProcessStartInfo,
    };

    try {
      commandProcess.Start();

#if SYSTEM_DIAGNOSTICS_PROCESS_WAITFOREXITASYNC
      await commandProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
      commandProcess.WaitForExit(); // TODO: cacellation
#endif

      if (logger is not null) {
        const LogLevel logLevelForStandardOutput = LogLevel.Trace;
        const LogLevel logLevelForStandardError = LogLevel.Error;

        static IEnumerable<(StreamReader, LogLevel)> EnumerateLogTarget(StreamReader stdout, StreamReader stderr)
        {
          yield return (stdout, logLevelForStandardOutput);
          yield return (stderr, logLevelForStandardError);
        }

        foreach (var (stdio, logLevel) in EnumerateLogTarget(commandProcess.StandardOutput, commandProcess.StandardError)) {
          if (!logger.IsEnabled(logLevel))
            continue;

          for (; ;) {
            var line =
#if SYSTEM_IO_STREAMREADER_READLINEASYNC_CANCELLATIONTOKEN
              await stdio.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
              await stdio.ReadLineAsync().ConfigureAwait(false);
#endif

            if (line is null)
              break;

#pragma warning disable CA2254
            logger.Log(logLevel: logLevel, message: line);
#pragma warning restore CA2254
          }
        }

        if (!logger.IsEnabled(LogLevel.Error))
          logger.LogDebug("Process exited with code {ExitCode}", commandProcess.ExitCode);
      }

      if (commandProcess.ExitCode != 0) {
        logger?.LogError(
          "Process exited with code {ExitCode}: '{ProcessStartInfoFileName} {ProcessStartInfoArguments}'",
          commandProcess.ExitCode,
          commandProcessStartInfo.FileName,
          commandProcessStartInfo.Arguments
        );
      }
    }
    catch (Exception ex) {
      logger?.LogError(
        ex,
        "Failed to run command: '{ProcessStartInfoFileName} {ProcessStartInfoArguments}'",
        commandProcessStartInfo.FileName,
        commandProcessStartInfo.Arguments
      );
    }
  }
}
