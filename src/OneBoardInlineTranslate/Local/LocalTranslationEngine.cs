using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Local;

internal interface ILocalTranslationEngine : IDisposable
{
    Task<string> TranslateAsync(
        LocalModelManifestEntry model,
        string text,
        CancellationToken cancellationToken);
}

internal sealed class LocalTranslationEngine(
    LocalModelManager modelManager,
    LocalTranslationSettings settings) : ILocalTranslationEngine
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Process? _process;
    private long _nextRequestId;

    public async Task<string> TranslateAsync(
        LocalModelManifestEntry model,
        string text,
        CancellationToken cancellationToken)
    {
        if (!modelManager.IsInstalled(model))
        {
            throw new LocalModelUnavailableException(model.Id);
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            EnsureProcess();
            var process = _process!;
            var requestId = Interlocked.Increment(ref _nextRequestId);
            var request = JsonSerializer.Serialize(new
            {
                id = requestId,
                modelPath = modelManager.GetModelDirectory(model),
                text
            });
            try
            {
                await process.StandardInput.WriteLineAsync(request.AsMemory(), cancellationToken);
                await process.StandardInput.FlushAsync(cancellationToken);
                var responseLine = await process.StandardOutput.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(responseLine))
                {
                    StopProcess();
                    throw new InvalidOperationException("The local translation engine stopped unexpectedly.");
                }

                using var response = JsonDocument.Parse(responseLine);
                var root = response.RootElement;
                if (root.GetProperty("id").GetInt64() != requestId ||
                    !root.GetProperty("ok").GetBoolean())
                {
                    throw new InvalidOperationException("The local translation engine could not translate this text.");
                }

                var translated = root.GetProperty("text").GetString();
                if (string.IsNullOrWhiteSpace(translated))
                {
                    throw new InvalidOperationException("The local translation engine returned an empty result.");
                }

                return translated;
            }
            catch (OperationCanceledException)
            {
                StopProcess();
                throw;
            }
            catch (IOException)
            {
                StopProcess();
                throw new InvalidOperationException("The local translation engine stopped unexpectedly.");
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        StopProcess();
        _gate.Dispose();
    }

    private void EnsureProcess()
    {
        if (_process is { HasExited: false })
        {
            return;
        }

        if (!modelManager.IsRuntimeInstalled)
        {
            throw new LocalModelUnavailableException("runtime");
        }

        StopProcess();
        var pythonPath = Path.Combine(modelManager.RuntimeDirectory, "python.exe");
        var runnerPath = Path.Combine(modelManager.RuntimeDirectory, "local_engine.py");
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false),
            WindowStyle = ProcessWindowStyle.Hidden
        };
        startInfo.ArgumentList.Add("-X");
        startInfo.ArgumentList.Add("utf8");
        startInfo.ArgumentList.Add("-u");
        startInfo.ArgumentList.Add(runnerPath);
        startInfo.Environment["PYTHONUTF8"] = "1";
        startInfo.Environment["ONEBOARD_MAX_MODELS"] = settings.MaximumLoadedModels.ToString(
            System.Globalization.CultureInfo.InvariantCulture);
        _process = Process.Start(startInfo) ??
            throw new InvalidOperationException("The local translation engine could not be started.");
        _ = _process.StandardError.ReadToEndAsync();
    }

    private void StopProcess()
    {
        var process = _process;
        _process = null;
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(2_000);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
        finally
        {
            process.Dispose();
        }
    }
}

internal sealed class LocalModelUnavailableException(string modelId) : InvalidOperationException(
    "Local model not installed. Open Settings → Local to download the required language model.")
{
    internal string ModelId { get; } = modelId;
}
