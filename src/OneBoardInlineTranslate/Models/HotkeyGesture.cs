using OneBoardInlineTranslate.Infrastructure;

namespace OneBoardInlineTranslate.Models;

internal sealed record HotkeyGesture(uint Modifiers, int VirtualKey, string DisplayText)
{
    internal static bool TryParse(string? value, out HotkeyGesture? gesture)
    {
        gesture = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var parts = value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        uint modifiers = NativeMethods.ModNoRepeat;
        var normalized = new List<string>();
        for (var index = 0; index < parts.Length - 1; index++)
        {
            if (parts[index].Equals("Alt", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModAlt;
                normalized.Add("Alt");
            }
            else if (parts[index].Equals("Shift", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModShift;
                normalized.Add("Shift");
            }
            else if (parts[index].Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                     parts[index].Equals("Control", StringComparison.OrdinalIgnoreCase))
            {
                modifiers |= NativeMethods.ModControl;
                normalized.Add("Ctrl");
            }
            else
            {
                return false;
            }
        }

        var keyText = parts[^1].ToUpperInvariant();
        if (keyText.Length != 1 || keyText[0] is < 'A' or > 'Z')
        {
            return false;
        }

        var meaningfulModifiers = modifiers & ~NativeMethods.ModNoRepeat;
        if (meaningfulModifiers == 0)
        {
            return false;
        }

        normalized.Add(keyText);
        gesture = new HotkeyGesture(modifiers, keyText[0], string.Join('+', normalized));
        return true;
    }
}
