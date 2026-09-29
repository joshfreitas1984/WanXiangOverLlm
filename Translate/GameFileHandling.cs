using FanslationStudio.LlmKit.Configuration;

namespace Translate;

/// <summary>
/// Game-specific extension points for this project - see <see cref="GameHooks"/>'s own doc comments
/// for what each hook is invoked with/expected to return.
/// </summary>
public static class GameFileHandling
{
    /// <summary>
    /// SOURCE -> in-game "year N" number for the "N年春节" (Year N Spring Festival) achievement/
    /// condition chain in Condition.json/Event.json (rawIndex 770100-789xxx, "Name"/"GroupName"
    /// columns). Both the primary translation pass and QC repeatedly mis-parsed this chain's mixed
    /// Chinese numeral conventions (an ordinal "第N年" prefix for years 3-10, plain "十N"/"二十" for
    /// 11-20, and an ABBREVIATED two-single-digit form - e.g. "五九" for 59, not the grammatically
    /// correct "五十九" - for every other year up to 80) into wildly inconsistent and frequently
    /// outright wrong English: "February Spring Festival" (二一 misread as a month), "Spring
    /// Festival of 2023" (二三 misread as a real calendar year), "The Year of the Rabbit Spring
    /// Festival" (二五 hallucinated into a zodiac sign), "Year 321 Spring Festival" (三二 garbled),
    /// "Year of the Year Spring Festival" (四九 garbled into nonsense) - see this repo's
    /// investigation session for the full catalogue (78 unique source strings, ~700 occurrences
    /// across both files, found via a downstream QC false-positive report).
    ///
    /// This is a closed, finite, already-known set - not a general Chinese-numeral parser - so it's
    /// a plain lookup table rather than an algorithm that could itself introduce a new parsing bug.
    /// Registered as <see cref="GameHooks.CustomColumnRepair"/> so ANY future LLM output for one of
    /// these exact SOURCE strings (a retranslation, a QC correction, a repair-loop attempt) is force-
    /// corrected to the canonical "Year {N} Spring Festival" form regardless of what the model
    /// produced - this class of defect is a deterministic lookup, not a translation judgment call,
    /// so it should never depend on an LLM (translation or QC) getting it right.
    /// </summary>
    private static readonly Dictionary<string, int> YearSpringFestivalYears = BuildYearSpringFestivalYears();

    private static Dictionary<string, int> BuildYearSpringFestivalYears()
    {
        // Exact source-string order as they appear in Files/Converted/Condition.json.yaml /
        // Event.json.yaml by ascending rawIndex - years 3 through 80 inclusive, sequential, no gaps
        // (confirmed against the corpus before building this table).
        string[] rawOrder =
        [
            "第三年春节", "第四年春节", "第五年春节", "第六年春节", "第七年春节", "第八年春节", "第九年春节", "第十年春节",
            "十一年春节", "十二年春节", "十三年春节", "十四年春节", "十五年春节", "十六年春节", "十七年春节", "十八年春节", "十九年春节", "二十年春节",
            "二一年春节", "二二年春节", "二三年春节", "二四年春节", "二五年春节", "二六年春节", "二七年春节", "二八年春节", "二九年春节", "三十年春节",
            "三一年春节", "三二年春节", "三三年春节", "三四年春节", "三五年春节", "三六年春节", "三七年春节", "三八年春节", "三九年春节", "四十年春节",
            "四一年春节", "四二年春节", "四三年春节", "四四年春节", "四五年春节", "四六年春节", "四七年春节", "四八年春节", "四九年春节", "五十年春节",
            "五一年春节", "五二年春节", "五三年春节", "五四年春节", "五五年春节", "五六年春节", "五七年春节", "五八年春节", "五九年春节", "六十年春节",
            "六一年春节", "六二年春节", "六三年春节", "六四年春节", "六五年春节", "六六年春节", "六七年春节", "六八年春节", "六九年春节", "七十年春节",
            "七一年春节", "七二年春节", "七三年春节", "七四年春节", "七五年春节", "七六年春节", "七七年春节", "七八年春节", "七九年春节", "八十年春节",
        ];

        var map = new Dictionary<string, int>();
        for (var i = 0; i < rawOrder.Length; i++)
            map[rawOrder[i]] = i + 3;
        return map;
    }

    public static readonly GameHooks Hooks = new()
    {
        CustomColumnRepair = (_, _, raw, result) =>
            YearSpringFestivalYears.TryGetValue(raw, out var year) ? $"Year {year} Spring Festival" : result,
    };
}
