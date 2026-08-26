using System.Text;

namespace SmartPOS.Application.Utilities;

internal static class ArabicChartText
{
    private sealed record ArabicForms(char Isolated, char Final, char? Initial = null, char? Medial = null)
    {
        public bool ConnectsToPrevious => Final != '\0';
        public bool ConnectsToNext => Initial.HasValue || Medial.HasValue;
    }

    private static readonly IReadOnlyDictionary<char, ArabicForms> Forms = new Dictionary<char, ArabicForms>
    {
        ['ء'] = new('\uFE80', '\uFE80'),
        ['آ'] = new('\uFE81', '\uFE82'),
        ['أ'] = new('\uFE83', '\uFE84'),
        ['ؤ'] = new('\uFE85', '\uFE86'),
        ['إ'] = new('\uFE87', '\uFE88'),
        ['ئ'] = new('\uFE89', '\uFE8A', '\uFE8B', '\uFE8C'),
        ['ا'] = new('\uFE8D', '\uFE8E'),
        ['ب'] = new('\uFE8F', '\uFE90', '\uFE91', '\uFE92'),
        ['ة'] = new('\uFE93', '\uFE94'),
        ['ت'] = new('\uFE95', '\uFE96', '\uFE97', '\uFE98'),
        ['ث'] = new('\uFE99', '\uFE9A', '\uFE9B', '\uFE9C'),
        ['ج'] = new('\uFE9D', '\uFE9E', '\uFE9F', '\uFEA0'),
        ['ح'] = new('\uFEA1', '\uFEA2', '\uFEA3', '\uFEA4'),
        ['خ'] = new('\uFEA5', '\uFEA6', '\uFEA7', '\uFEA8'),
        ['د'] = new('\uFEA9', '\uFEAA'),
        ['ذ'] = new('\uFEAB', '\uFEAC'),
        ['ر'] = new('\uFEAD', '\uFEAE'),
        ['ز'] = new('\uFEAF', '\uFEB0'),
        ['س'] = new('\uFEB1', '\uFEB2', '\uFEB3', '\uFEB4'),
        ['ش'] = new('\uFEB5', '\uFEB6', '\uFEB7', '\uFEB8'),
        ['ص'] = new('\uFEB9', '\uFEBA', '\uFEBB', '\uFEBC'),
        ['ض'] = new('\uFEBD', '\uFEBE', '\uFEBF', '\uFEC0'),
        ['ط'] = new('\uFEC1', '\uFEC2', '\uFEC3', '\uFEC4'),
        ['ظ'] = new('\uFEC5', '\uFEC6', '\uFEC7', '\uFEC8'),
        ['ع'] = new('\uFEC9', '\uFECA', '\uFECB', '\uFECC'),
        ['غ'] = new('\uFECD', '\uFECE', '\uFECF', '\uFED0'),
        ['ف'] = new('\uFED1', '\uFED2', '\uFED3', '\uFED4'),
        ['ق'] = new('\uFED5', '\uFED6', '\uFED7', '\uFED8'),
        ['ك'] = new('\uFED9', '\uFEDA', '\uFEDB', '\uFEDC'),
        ['ل'] = new('\uFEDD', '\uFEDE', '\uFEDF', '\uFEE0'),
        ['م'] = new('\uFEE1', '\uFEE2', '\uFEE3', '\uFEE4'),
        ['ن'] = new('\uFEE5', '\uFEE6', '\uFEE7', '\uFEE8'),
        ['ه'] = new('\uFEE9', '\uFEEA', '\uFEEB', '\uFEEC'),
        ['و'] = new('\uFEED', '\uFEEE'),
        ['ى'] = new('\uFEEF', '\uFEF0'),
        ['ي'] = new('\uFEF1', '\uFEF2', '\uFEF3', '\uFEF4')
    };

    public static string Shape(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text ?? string.Empty;

        var output = new StringBuilder(text.Length * 2);
        var run = new StringBuilder();
        var inArabicRun = false;

        foreach (var character in text)
        {
            var isArabic = IsArabicCharacter(character);
            if (run.Length > 0 && isArabic != inArabicRun)
            {
                output.Append(inArabicRun ? ShapeArabicRun(run.ToString()) : run);
                run.Clear();
            }

            run.Append(character);
            inArabicRun = isArabic;
        }

        if (run.Length > 0)
            output.Append(inArabicRun ? ShapeArabicRun(run.ToString()) : run);

        return output.ToString();
    }

    private static string ShapeArabicRun(string run)
    {
        var shaped = new List<char>(run.Length);

        for (var index = 0; index < run.Length; index++)
        {
            var current = run[index];
            if (!Forms.TryGetValue(current, out var currentForms))
            {
                shaped.Add(current);
                continue;
            }

            var previous = FindPreviousArabic(run, index - 1);
            var next = FindNextArabic(run, index + 1);

            var joinsPrevious = previous.HasValue && CanConnect(previous.Value, current);
            var joinsNext = next.HasValue && CanConnect(current, next.Value);

            var glyph = joinsPrevious && joinsNext && currentForms.Medial.HasValue ? currentForms.Medial.Value
                : joinsPrevious ? currentForms.Final
                : joinsNext && currentForms.Initial.HasValue ? currentForms.Initial.Value
                : currentForms.Isolated;

            shaped.Add(glyph);
        }

        shaped.Reverse();
        return "\u200F" + new string(shaped.ToArray()) + "\u200F";
    }

    private static char? FindPreviousArabic(string text, int index)
    {
        for (var i = index; i >= 0; i--)
        {
            if (Forms.ContainsKey(text[i]))
                return text[i];
        }

        return null;
    }

    private static char? FindNextArabic(string text, int index)
    {
        for (var i = index; i < text.Length; i++)
        {
            if (Forms.ContainsKey(text[i]))
                return text[i];
        }

        return null;
    }

    private static bool CanConnect(char left, char right)
    {
        return Forms.TryGetValue(left, out var leftForms)
               && Forms.TryGetValue(right, out var rightForms)
               && leftForms.ConnectsToNext
               && rightForms.ConnectsToPrevious;
    }

    private static bool IsArabicCharacter(char value)
    {
        return Forms.ContainsKey(value)
               || value is >= '\u0600' and <= '\u06FF'
               || value is >= '\u0750' and <= '\u077F'
               || value is >= '\u08A0' and <= '\u08FF';
    }
}
