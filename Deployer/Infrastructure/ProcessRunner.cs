using System.Diagnostics;
using Deployer.Abstractions;
using Deployer.Models;
using Microsoft.Extensions.Logging;

namespace Deployer.Infrastructure
{
    public sealed class ProcessRunner : IProcessRunner
    {
        private readonly ILogger<ProcessRunner> _logger;

        public ProcessRunner(ILogger<ProcessRunner> logger)
        {
            _logger = logger;
        }

        public async Task<SProcessResult> RunAsync(
            SProcessStart start,
            Action<string>? onOutput,
            CancellationToken cancellationToken)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = start.FileName,
                WorkingDirectory = start.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            foreach (var argument in start.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            if (start.EnvironmentVariables is not null)
            {
                foreach (var pair in start.EnvironmentVariables)
                {
                    startInfo.Environment[pair.Key] = pair.Value;
                }
            }

            _logger.LogInformation(
                "Starting {FileName} in {WorkingDirectory}",
                start.FileName,
                start.WorkingDirectory);

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            var outputLock = new object();
            var outputLines = new List<string>();

            void HandleLine(string? line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    return;
                }

                lock (outputLock)
                {
                    outputLines.Add(line);
                    onOutput?.Invoke(line);
                    _logger.LogInformation("{ProcessOutput}", line);
                }
            }

            process.OutputDataReceived += (_, args) => HandleLine(args.Data);
            process.ErrorDataReceived += (_, args) => HandleLine(args.Data);

            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start {start.FileName}.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            try
            {
                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                process.WaitForExit();
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                throw;
            }

            List<string> snapshot;
            lock (outputLock)
            {
                snapshot = [.. outputLines];
            }

            _logger.LogInformation("{FileName} exited with {ExitCode}", start.FileName, process.ExitCode);
            return new SProcessResult
            {
                ExitCode = process.ExitCode,
                OutputLines = snapshot
            };
        }

        private void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    _logger.LogWarning("Killed {FileName} because the deploy was cancelled", process.StartInfo.FileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill {FileName}", process.StartInfo.FileName);
            }
        }
    }
}
