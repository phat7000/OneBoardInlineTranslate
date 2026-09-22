using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OneBoardInlineTranslate.Security;

internal sealed class DpapiCredentialStore : ICredentialStore
{
    private const uint CryptProtectUiForbidden = 0x1;
    private readonly string _path;
    private readonly SemaphoreSlim _gate = new(1, 1);

    internal DpapiCredentialStore(string? directory = null)
    {
        var dataDirectory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneBoardInlineTranslate");
        _path = Path.Combine(dataDirectory, "credentials.dat");
    }

    public async Task<string?> GetAsync(string name, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var values = await ReadAsync(cancellationToken);
            return values.TryGetValue(name, out var value) ? value : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetAsync(string name, string secret, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(secret);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var values = await ReadAsync(cancellationToken);
            values[name] = secret;
            await WriteAsync(values, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveAsync(string name, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var values = await ReadAsync(cancellationToken);
            if (values.Remove(name))
            {
                await WriteAsync(values, cancellationToken);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<Dictionary<string, string>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path))
        {
            return new(StringComparer.Ordinal);
        }

        try
        {
            var encrypted = await File.ReadAllBytesAsync(_path, cancellationToken);
            var clear = Unprotect(encrypted);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(clear)
                ?? new(StringComparer.Ordinal);
        }
        catch (Exception exception) when (exception is IOException or JsonException or System.Security.Cryptography.CryptographicException)
        {
            return new(StringComparer.Ordinal);
        }
    }

    private async Task WriteAsync(Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        var clear = JsonSerializer.SerializeToUtf8Bytes(values);
        var encrypted = Protect(clear);
        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = _path + ".tmp";
        await File.WriteAllBytesAsync(temporaryPath, encrypted, cancellationToken);
        File.Move(temporaryPath, _path, overwrite: true);
    }

    private static byte[] Protect(byte[] value) => Transform(value, protect: true);

    private static byte[] Unprotect(byte[] value) => Transform(value, protect: false);

    private static byte[] Transform(byte[] value, bool protect)
    {
        var inputPointer = Marshal.AllocHGlobal(value.Length);
        try
        {
            Marshal.Copy(value, 0, inputPointer, value.Length);
            var input = new DataBlob { Size = value.Length, Data = inputPointer };
            DataBlob output;
            var success = protect
                ? CryptProtectData(ref input, null, nint.Zero, nint.Zero, nint.Zero, CryptProtectUiForbidden, out output)
                : CryptUnprotectData(ref input, nint.Zero, nint.Zero, nint.Zero, nint.Zero, CryptProtectUiForbidden, out output);
            if (!success)
            {
                throw new System.Security.Cryptography.CryptographicException(Marshal.GetLastWin32Error());
            }

            try
            {
                var result = new byte[output.Size];
                Marshal.Copy(output.Data, result, 0, output.Size);
                return result;
            }
            finally
            {
                LocalFree(output.Data);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(inputPointer);
            Array.Clear(value);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        internal int Size;
        internal nint Data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob dataIn,
        string? description,
        nint optionalEntropy,
        nint reserved,
        nint promptStruct,
        uint flags,
        out DataBlob dataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob dataIn,
        nint description,
        nint optionalEntropy,
        nint reserved,
        nint promptStruct,
        uint flags,
        out DataBlob dataOut);

    [DllImport("kernel32.dll")]
    private static extern nint LocalFree(nint memory);
}
