using OneBoardInlineTranslate.Models;
using OneBoardInlineTranslate.Views;

namespace OneBoardInlineTranslate.OCR;

internal interface IRegionSelectionService
{
    Task<ScreenRegion?> SelectAsync(CancellationToken cancellationToken);
}

internal sealed class RegionSelectionService : IRegionSelectionService
{
    public Task<ScreenRegion?> SelectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var selector = new RegionSelectionWindow();
        var accepted = selector.ShowDialog() == true;
        return Task.FromResult(accepted ? selector.SelectedRegion : null);
    }
}
