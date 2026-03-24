using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PIA.Plugin;

/// <summary>
/// Wraps a CLI tool.
/// </summary>
/// <param name="path">The path to the CLI executable.</param>
public class Cli(string path) 
{
    /// <summary>
    /// Run the command and wait for it to finish. Returns the exit code of the process.
    /// </summary>
    public async Task<int> RunAsync(string command)
    {
        using var process = Start(command);
        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    /// <summary>
    /// Run the command and wait for it to finish. Returns the standard output of the process.
    /// </summary>
    public async Task<string> ReadAsync(string command)
    {
        using var process = Start(command, x => x.RedirectStandardOutput = true);
        await process.WaitForExitAsync();
        return process.StandardOutput.ReadToEnd().Trim();
    }

    private Process Start(string command, Action<ProcessStartInfo> configure = null)
    {
        var config = new ProcessStartInfo
        {
            FileName = path,
            Arguments = command,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        configure?.Invoke(config);

        return Process.Start(config);
    }
}
