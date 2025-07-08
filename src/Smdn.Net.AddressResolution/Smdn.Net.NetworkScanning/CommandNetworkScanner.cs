// SPDX-FileCopyrightText: 2023 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT
#pragma warning disable CA1815 // TODO: implement equality comparison

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Smdn.Net.NetworkScanning;

public abstract class CommandNetworkScanner : INetworkScanner {
#pragma warning disable CA1034
  public interface IProcessFactory {
#pragma warning restore CA1034
    Process CreateProcess(ProcessStartInfo processStartInfo);
  }

  private sealed class DefaultProcessFactory : IProcessFactory {
    public static readonly DefaultProcessFactory Instance = new();

    public Process CreateProcess(ProcessStartInfo processStartInfo)
      => new() { StartInfo = processStartInfo };
  }

  private static readonly Lazy<IReadOnlyCollection<string>> LazyDefaultCommandPaths = new(
    valueFactory: GetDefaultCommandCommandPaths,
    isThreadSafe: true
  );

  protected static IReadOnlyCollection<string> DefaultCommandPaths => LazyDefaultCommandPaths.Value;

#pragma warning disable CA1859
  private static IReadOnlyCollection<string> GetDefaultCommandCommandPaths()
#pragma warning restore CA1859
  {
    var paths = new HashSet<string>(
      comparer: RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal
    );

    var envVarPath = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
#if SYSTEM_STRINGSPLITOPTIONS_TRIMENTRIES
      .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
#elif SYSTEM_STRING_SPLIT_CHAR
      .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
#else
      .Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries);
#endif

    foreach (var path in envVarPath) {
#if SYSTEM_STRINGSPLITOPTIONS_TRIMENTRIES
      paths.Add(path);
#else
      paths.Add(path.Trim());
#endif
    }

    return paths;
  }

  protected readonly struct Command {
    public string Name { get; }
    public bool IsAvailable => executablePath is not null;
    private readonly string? executablePath;

    public Command(string name, string? executablePath)
    {
      Name = name;
      this.executablePath = executablePath;
    }

#pragma warning disable CA1024
    public string GetExecutablePathOrThrow()
#pragma warning restore CA1024
      => executablePath is null
        ? throw new NotSupportedException(
            $"'{Name}' is not available. Make sure that the PATH environment variable is set properly."
          )
        : executablePath;
  }

  protected static Command FindCommand(
    string command,
    IEnumerable<string> paths
  )
  {
    if (command is null)
      throw new ArgumentNullException(nameof(command));
    if (paths is null)
      throw new ArgumentNullException(nameof(paths));

    string? commandWithExeExtension = null;

    if (
      RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
      string.IsNullOrEmpty(Path.GetExtension(command))
    ) {
      commandWithExeExtension = command + ".exe";
    }

    return new(
      name: command,
      executablePath: paths
        .SelectMany(path => CombineCommandPath(path, command, commandWithExeExtension))
        .FirstOrDefault(static pathToCommand => File.Exists(pathToCommand))
    );

    static IEnumerable<string> CombineCommandPath(string path, string command, string? commandWithExeExtension)
    {
      yield return Path.Combine(path, command);

      if (commandWithExeExtension is not null)
        yield return Path.Combine(path, commandWithExeExtension);
    }
  }

  /*
   * instance members
   */
  private readonly ILogger? logger;
  private IProcessFactory processFactory; // if null, it indicates a 'disposed' state.

  protected CommandNetworkScanner(
    ILogger? logger,
    IServiceProvider? serviceProvider
  )
  {
    this.logger = logger;
    processFactory = serviceProvider?.GetService<IProcessFactory>() ?? DefaultProcessFactory.Instance;
  }

  public void Dispose()
  {
    Dispose(true);
    GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing)
  {
    if (!disposing)
      return;

    processFactory = null!;
  }

  protected void ThrowIfDisposed()
  {
    if (processFactory is null)
      throw new ObjectDisposedException(GetType().FullName);
  }

  /// <inheritdoc cref="GetCommandLineArguments(IEnumerable{IPAddress}, out string, out string)" />
  protected abstract bool GetCommandLineArguments(
    out string executable,
    out string? arguments
  );

  /// <summary>
  /// Gets the path of the executable file and the arguments pass to the command for performing the network scan.
  /// </summary>
  /// <param name="addressesToScan">
  /// The target list of <see cref="IPAddress" /> for performing the network scan.
  /// </param>
  /// <param name="executable">
  /// The path to the executable file of the command.
  /// </param>
  /// <param name="arguments">
  /// The arguments pass to the command.
  /// </param>
  /// <return>
  /// If <see langword="true" />, performs the network scan by invoking the command with specified <paramref name="executable"/> and <paramref name="arguments"/>.
  /// If <see langword="false" />, the network scan is not performed.
  /// </return>
  protected abstract bool GetCommandLineArguments(
    IEnumerable<IPAddress> addressesToScan,
    out string executable,
    out string? arguments
  );

  public virtual ValueTask ScanAsync(
    CancellationToken cancellationToken = default
  )
  {
    if (cancellationToken.IsCancellationRequested) {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      return ValueTask.FromCanceled(cancellationToken);
#else
      return ValueTaskShim.FromCanceled(cancellationToken);
#endif
    }

    ThrowIfDisposed();

    if (GetCommandLineArguments(out var executable, out var args)) {
      return RunCommandAsync(
        commandFileName: executable,
        commandArguments: args,
        processFactory: processFactory,
        logger: logger,
        cancellationToken: cancellationToken
      );
    }

    return default; // do nothing
  }

  public virtual ValueTask ScanAsync(
    IEnumerable<IPAddress> addresses,
    CancellationToken cancellationToken = default
  )
  {
    if (addresses is null)
      throw new ArgumentNullException(nameof(addresses));

    if (cancellationToken.IsCancellationRequested) {
#if SYSTEM_THREADING_TASKS_VALUETASK_FROMCANCELED
      return ValueTask.FromCanceled(cancellationToken);
#else
      return ValueTaskShim.FromCanceled(cancellationToken);
#endif
    }

    ThrowIfDisposed();

    if (GetCommandLineArguments(addresses, out var executable, out var args)) {
      return RunCommandAsync(
        commandFileName: executable,
        commandArguments: args,
        processFactory: processFactory,
        logger: logger,
        cancellationToken: cancellationToken
      );
    }

    return default; // do nothing
  }

  private static async ValueTask RunCommandAsync(
    string commandFileName,
    string? commandArguments,
    IProcessFactory processFactory,
    ILogger? logger,
    CancellationToken cancellationToken
  )
  {
    var commandProcessStartInfo = new ProcessStartInfo() {
      FileName = commandFileName,
      Arguments = commandArguments ?? string.Empty,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      UseShellExecute = false,
    };

    logger?.LogDebug(
      "{ProcessStartInfoFileName} {ProcessStartInfoArguments}",
      commandProcessStartInfo.FileName,
      commandProcessStartInfo.Arguments
    );

    using var commandProcess = processFactory.CreateProcess(commandProcessStartInfo);

    cancellationToken.ThrowIfCancellationRequested();

    try {
      commandProcess.Start();

#if SYSTEM_DIAGNOSTICS_PROCESS_WAITFOREXITASYNC
      await commandProcess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
#else
      commandProcess.WaitForExit(); // TODO: cancellation
#endif

      if (logger is not null) {
        const LogLevel LogLevelForStandardOutput = LogLevel.Trace;
        const LogLevel LogLevelForStandardError = LogLevel.Error;

        static IEnumerable<(StreamReader, LogLevel)> EnumerateLogTarget(StreamReader stdout, StreamReader stderr)
        {
          yield return (stdout, LogLevelForStandardOutput);
          yield return (stderr, LogLevelForStandardError);
        }

        foreach (var (stdio, logLevel) in EnumerateLogTarget(commandProcess.StandardOutput, commandProcess.StandardError)) {
          if (!logger.IsEnabled(logLevel))
            continue;

          for (; ; ) {
            var line =
#if SYSTEM_IO_TEXTREADER_READLINEASYNC_CANCELLATIONTOKEN
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
#pragma warning disable CA1031
    catch (Exception ex) {
      logger?.LogError(
        ex,
        "Failed to run command: '{ProcessStartInfoFileName} {ProcessStartInfoArguments}'",
        commandProcessStartInfo.FileName,
        commandProcessStartInfo.Arguments
      );
    }
#pragma warning restore CA1031
  }
}
