using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Services;

internal static class ResultWindowPolicy
{
    internal static bool ShowQuickReplacementConfirmation(ResultWindowMode mode) =>
        mode != ResultWindowMode.Hidden;

    internal static bool ShowExplicitResult(ResultWindowMode mode) => true;
}
