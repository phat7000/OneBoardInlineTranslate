using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Local;

internal sealed class LocalModelManager
{
    private static readonly JsonSerializerOptions MarkerJsonOptions = new() { WriteIndented = true };
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _installGate = new(1, 1);

    internal LocalModelManager(HttpClient httpClient, string? baseDirectory = null)
    {
        _httpClient = httpClient;
        BaseDirectory = Path.GetFullPath(baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneBoardInlineTranslate",
            "models"));
        RuntimeDirectory = ResolveUnderRoot(Path.Combine("_runtime", LocalModelManifest.EngineVersion));
    }

    internal string BaseDirectory { get; }

    internal string RuntimeDirectory { get; }

    internal bool IsRuntimeInstalled =>
        File.Exists(Path.Combine(RuntimeDirectory, "python.exe")) &&
        File.Exists(Path.Combine(RuntimeDirectory, "local_engine.py")) &&
        Directory.Exists(Path.Combine(RuntimeDirectory, "ctranslate2")) &&
        Directory.EnumerateFiles(
            Path.Combine(RuntimeDirectory, "ctranslate2"),
            "_ext*.pyd",
            SearchOption.TopDirectoryOnly).Any();

    internal IReadOnlyList<LocalModelManifestEntry> InstalledModels =>
        LocalModelManifest.Models.Where(IsInstalled).ToArray();

    internal bool IsInstalled(LocalModelManifestEntry model) =>
        File.Exists(Path.Combine(GetModelDirectory(model), "installed.json")) &&
        File.Exists(Path.Combine(GetModelDirectory(model), "model", "model.bin")) &&
        File.Exists(Path.Combine(GetModelDirectory(model), "sentencepiece.model"));

    internal string GetModelDirectory(LocalModelManifestEntry model) =>
        ResolveUnderRoot(Path.Combine(model.Id, model.Version));

    internal long GetStorageSize()
    {
        if (!Directory.Exists(BaseDirectory))
        {
            return 0;
        }

        return Directory.EnumerateFiles(BaseDirectory, "*", SearchOption.AllDirectories)
            .Select(path => new FileInfo(path).Length)
            .Sum();
    }

    internal async Task InstallAsync(
        LocalModelManifestEntry model,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        if (!LocalModelManifest.Models.Contains(model))
        {
            throw new InvalidOperationException("The local model is not in the trusted manifest.");
        }

        await _installGate.WaitAsync(cancellationToken);
        try
        {
            if (IsInstalled(model))
            {
                return;
            }

            Directory.CreateDirectory(BaseDirectory);
            await EnsureRuntimeAsync(progress, cancellationToken);
            var archivePath = await DownloadVerifiedAsync(
                model.Id,
                model.DownloadUri,
                model.DownloadSize,
                model.Sha256,
                progress,
                cancellationToken);
            var staging = ResolveUnderRoot(Path.Combine(".staging", $"{model.Id}-{Guid.NewGuid():N}"));
            try
            {
                Directory.CreateDirectory(staging);
                SafeExtractArchive(archivePath, staging);
                var metadataPath = Directory.EnumerateFiles(
                    staging,
                    "metadata.json",
                    SearchOption.AllDirectories).SingleOrDefault();
                if (metadataPath is null)
                {
                    throw new InvalidDataException("The local model package is incomplete.");
                }

                var packageRoot = Path.GetDirectoryName(metadataPath)!;
                ValidateModelPackage(packageRoot, model);
                var destination = GetModelDirectory(model);
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                if (Directory.Exists(destination))
                {
                    Directory.Delete(destination, recursive: true);
                }

                Directory.Move(packageRoot, destination);
                await File.WriteAllTextAsync(
                    Path.Combine(destination, "installed.json"),
                    JsonSerializer.Serialize(new
                    {
                        model.Id,
                        model.Version,
                        model.SourceLanguage,
                        model.TargetLanguage,
                        model.Sha256,
                        installedAt = DateTimeOffset.UtcNow
                    }, MarkerJsonOptions),
                    cancellationToken);
            }
            finally
            {
                TryDeleteFile(archivePath);
                TryDeleteDirectory(staging);
            }
        }
        finally
        {
            _installGate.Release();
        }
    }

    internal async Task RemoveAsync(LocalModelManifestEntry model, CancellationToken cancellationToken)
    {
        await _installGate.WaitAsync(cancellationToken);
        try
        {
            var path = GetModelDirectory(model);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        finally
        {
            _installGate.Release();
        }
    }

    internal string ResolveUnderRoot(string relativePath)
    {
        var candidate = Path.GetFullPath(Path.Combine(BaseDirectory, relativePath));
        var rootPrefix = BaseDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The local model path is outside the configured model store.");
        }

        return candidate;
    }

    internal static void SafeExtractArchive(string archivePath, string destination)
    {
        var destinationRoot = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        using var archive = ZipFile.OpenRead(archivePath);
        foreach (var entry in archive.Entries)
        {
            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!target.StartsWith(destinationRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The archive contains an unsafe path.");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(target);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    private async Task EnsureRuntimeAsync(
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (IsRuntimeInstalled)
        {
            return;
        }

        var staging = ResolveUnderRoot(Path.Combine(".staging", $"runtime-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(staging);
        try
        {
            foreach (var component in LocalModelManifest.RuntimeComponents)
            {
                var archivePath = await DownloadVerifiedAsync(
                    component.FileName,
                    component.DownloadUri,
                    component.DownloadSize,
                    component.Sha256,
                    progress,
                    cancellationToken);
                try
                {
                    SafeExtractArchive(archivePath, staging);
                }
                finally
                {
                    TryDeleteFile(archivePath);
                }
            }

            var runnerSource = Path.Combine(AppContext.BaseDirectory, "Local", "local_engine.py");
            if (!File.Exists(runnerSource))
            {
                throw new FileNotFoundException("The packaged local translation worker is missing.");
            }

            File.Copy(runnerSource, Path.Combine(staging, "local_engine.py"), overwrite: true);
            var pthPath = Path.Combine(staging, "python312._pth");
            await File.WriteAllTextAsync(
                pthPath,
                "python312.zip" + Environment.NewLine + "." + Environment.NewLine + "import site" + Environment.NewLine,
                cancellationToken);
            Directory.CreateDirectory(Path.GetDirectoryName(RuntimeDirectory)!);
            if (Directory.Exists(RuntimeDirectory))
            {
                Directory.Delete(RuntimeDirectory, recursive: true);
            }

            Directory.Move(staging, RuntimeDirectory);
        }
        finally
        {
            TryDeleteDirectory(staging);
        }
    }

    private async Task<string> DownloadVerifiedAsync(
        string itemName,
        Uri uri,
        long expectedSize,
        string expectedSha256,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Local components must be downloaded over HTTPS.");
        }

        var downloadDirectory = ResolveUnderRoot(".downloads");
        Directory.CreateDirectory(downloadDirectory);
        var temporaryPath = ResolveUnderRoot(Path.Combine(".downloads", $"{Guid.NewGuid():N}.tmp"));
        try
        {
            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                65_536,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            var buffer = new byte[65_536];
            long total = 0;
            while (true)
            {
                var read = await input.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                {
                    break;
                }

                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                total += read;
                progress?.Report(new ModelDownloadProgress(itemName, total, expectedSize));
            }

            await output.FlushAsync(cancellationToken);
            await output.DisposeAsync();
            await VerifyFileAsync(temporaryPath, expectedSize, expectedSha256, cancellationToken);

            return temporaryPath;
        }
        catch
        {
            TryDeleteFile(temporaryPath);
            throw;
        }
    }

    internal static async Task VerifyFileAsync(
        string path,
        long expectedSize,
        string expectedSha256,
        CancellationToken cancellationToken)
    {
        if (new FileInfo(path).Length != expectedSize)
        {
            throw new InvalidDataException("The downloaded component size did not match the trusted manifest.");
        }

        var actualHash = await ComputeSha256Async(path, cancellationToken);
        if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The downloaded component failed SHA-256 verification.");
        }
    }

    internal static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            65_536,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    internal static void ValidateModelPackage(string packageRoot, LocalModelManifestEntry model)
    {
        var metadata = JsonDocument.Parse(File.ReadAllText(Path.Combine(packageRoot, "metadata.json")));
        var root = metadata.RootElement;
        var source = LanguageCatalog.NormalizeCode(root.GetProperty("from_code").GetString());
        var target = LanguageCatalog.NormalizeCode(root.GetProperty("to_code").GetString());
        if (!string.Equals(source, model.SourceLanguage, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(target, model.TargetLanguage, StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(Path.Combine(packageRoot, "model", "model.bin")) ||
            !File.Exists(Path.Combine(packageRoot, "sentencepiece.model")))
        {
            throw new InvalidDataException("The local model package does not match its trusted manifest entry.");
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
