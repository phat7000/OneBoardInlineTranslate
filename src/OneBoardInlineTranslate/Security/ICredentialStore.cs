namespace OneBoardInlineTranslate.Security;

internal interface ICredentialStore
{
    Task<string?> GetAsync(string name, CancellationToken cancellationToken = default);

    Task SetAsync(string name, string secret, CancellationToken cancellationToken = default);

    Task RemoveAsync(string name, CancellationToken cancellationToken = default);
}
