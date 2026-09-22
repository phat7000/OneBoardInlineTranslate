using System.Windows.Automation;
using System.Windows.Automation.Text;
using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Services;

internal sealed class UiaSelectionReader
{
    internal string? TryRead(ForegroundContext context)
    {
        var root = AutomationElement.FromHandle(context.WindowHandle);
        if (root is null)
        {
            return null;
        }

        AutomationElement? focused;
        try
        {
            // FocusedElement is more reliable for provider fragment roots (Chromium/Electron and
            // WPF included). Some providers do not satisfy a focused-property FindFirst issued
            // against the top-level HWND even though their focused fragment supports TextPattern.
            focused = AutomationElement.FocusedElement;
            if (focused is not null && !IsDescendantOrSelf(focused, root))
            {
                focused = null;
            }

            focused ??= root.FindFirst(
                TreeScope.Subtree,
                new PropertyCondition(AutomationElement.HasKeyboardFocusProperty, true));
        }
        catch (ElementNotAvailableException)
        {
            return null;
        }

        if (focused is not null)
        {
            var current = focused;
            for (var depth = 0; current is not null && depth < 16; depth++)
            {
                var selectedText = TryReadFromElement(current);
                if (selectedText is not null)
                {
                    return selectedText;
                }

                if (Automation.Compare(current, root))
                {
                    break;
                }

                try
                {
                    current = TreeWalker.RawViewWalker.GetParent(current);
                }
                catch (ElementNotAvailableException)
                {
                    break;
                }
            }
        }

        return TryReadFromElement(root);
    }

    private static bool IsDescendantOrSelf(AutomationElement candidate, AutomationElement root)
    {
        var current = candidate;
        for (var depth = 0; depth < 64; depth++)
        {
            if (Automation.Compare(current, root))
            {
                return true;
            }

            try
            {
                current = TreeWalker.RawViewWalker.GetParent(current);
                if (current is null)
                {
                    return false;
                }
            }
            catch (ElementNotAvailableException)
            {
                return false;
            }
        }

        return false;
    }

    private static string? TryReadFromElement(AutomationElement element)
    {
        try
        {
            if (!element.TryGetCurrentPattern(TextPattern.Pattern, out var patternObject) ||
                patternObject is not TextPattern textPattern)
            {
                return null;
            }

            var selections = textPattern.GetSelection();
            if (selections.Length == 0)
            {
                return null;
            }

            var selectedParts = new List<string>(selections.Length);
            foreach (var selection in selections)
            {
                if (selection.CompareEndpoints(
                        TextPatternRangeEndpoint.Start,
                        selection,
                        TextPatternRangeEndpoint.End) == 0)
                {
                    continue;
                }

                var text = selection.GetText(-1);
                if (text.Length > 0)
                {
                    selectedParts.Add(text);
                }
            }

            return selectedParts.Count == 0
                ? null
                : string.Join(Environment.NewLine, selectedParts);
        }
        catch (Exception exception) when (
            exception is ElementNotAvailableException or InvalidOperationException or NotSupportedException)
        {
            return null;
        }
    }
}
