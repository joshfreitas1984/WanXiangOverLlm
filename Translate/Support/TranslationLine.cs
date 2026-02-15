using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace Translate;

public class TranslationLine
{
    [YamlMember(ScalarStyle = ScalarStyle.DoubleQuoted)]
    public string Raw { get; set; } = string.Empty;

    public string RawIndex { get; set; }

    [YamlIgnore]
    public string Translated { get; set; } = string.Empty;

    public List<TranslationSplit> Splits { get; set; } = [];

    public TranslationLine() { }

    public TranslationLine(string raw)
    {
        Raw = raw;
    }
}