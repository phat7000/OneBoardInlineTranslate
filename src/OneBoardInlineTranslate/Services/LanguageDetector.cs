using OneBoardInlineTranslate.Models;

namespace OneBoardInlineTranslate.Services;

internal interface ILanguageDetector
{
    Language Detect(string text);
}

internal sealed class LanguageDetector : ILanguageDetector
{
    private const string VietnameseCharacters = "ăâđêôơưĂÂĐÊÔƠƯáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ";

    public Language Detect(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Any(character => character is >= '\u3400' and <= '\u9fff'))
        {
            return Language.SimplifiedChinese;
        }

        if (text.Any(VietnameseCharacters.Contains))
        {
            return Language.Vietnamese;
        }

        return Language.English;
    }
}
