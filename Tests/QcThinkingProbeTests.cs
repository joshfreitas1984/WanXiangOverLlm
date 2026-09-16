using FanslationStudio.LlmKit.Workflow;

namespace Translate.Tests;

/// <summary>
/// Diagnostic-only, NOT part of the numbered QualityControlWorkflowTests pipeline - probes the QC
/// model's actual reasoning (thinking mode turned back on via
/// QualityReviewWorkflow.ProbeReviewReasoningAsync's enableThinking: true) against a hand-picked set
/// of SOURCE/TRANSLATION pairs, so a human can compare what the model actually reasoned against what
/// BaseQualityReviewPrompt.txt/BaseQualityReviewVerificationPrompt.txt (in
/// ../FanslationStudio.LlmKit/FanslationStudio.LlmKit/BaseFiles/Qwen38/Prompts/) intended, before
/// tweaking prompt/rubric wording. Production QC (QualityReviewWorkflow.RunAsync/RunBruteForce) never
/// enables thinking - both prompts explicitly forbid a reasoning preamble in their output format -
/// so this exists purely to see the reasoning trace on demand, not to change production behavior.
///
/// Fill in Samples below with real SOURCE/TRANSLATION pairs (a mix of ones QC flagged low-score,
/// ones it auto-accepted that still look off, and DEFECT: UNCERTAIN cases) before running this - it
/// is a no-op list right now.
/// </summary>
public class QcThinkingProbeTests
{
    public const string WorkingDirectory = TranslationWorkflowTests.WorkingDirectory;

    // Pulled from DragonHierOverLlm's Files/TestResults/QcTriageLowScoreSample.yaml (a different
    // downstream repo's real low-score QC output, not this repo's). Source = "text" (SOURCE), and
    // Translation = "qcReviewedText" (not "qcTranslated") - per FlaggedQcReview's doc comment,
    // qcReviewedText is "the exact text QC actually judged to produce this flag", which is what call
    // 1/call 2 actually saw; qcTranslated is whatever came out the other end (a correction, or
    // unchanged). Spans every score band in the sample (0/10/15/20) and most DEFECT categories, plus
    // one outright anomaly (#1's qcReviewedText is leaked/garbled protocol-sounding text, not an
    // actual translation - worth seeing what the model "thinks" happened there).
    private static readonly QualityReviewWorkflow.QcProbeSample[] Samples =
    [
        // score 0, OtherNamedDefect - qcReviewedText looks like leaked/garbled protocol text, not a translation
        //new("招式粗浅，以命相搏的刀法，多为贼寇所使", "Explanation was provided when none should be given. Alternatives were provided when none should be given."),

        // score 0, DroppedContent - classical poem, multi-\n
        //new("关东有义士，兴兵讨群凶。初期会盟津，乃心在咸阳。\\n军合力不齐，踌躇而雁行。势利使人争，嗣还自相戕。",
        //    "Eastern Jing Province Has Righteous Warriors, Rising to Conquer Many Evils. Initially Gathering at Muye, Their Hearts Set on Xianyang.\\nWith military strength not united, they hesitated and moved like geese in formation. Power and interest drive them to compete, then to mutually destroy one another."),

        //// score 0, OtherNamedDefect - names swapped (Immortal Li vs Immortal Zhisida, Sect Leader Sima vs Sect Leader Li)
        //new("李真人给我们分发药品之时，司马掌门也在场，\\n众目睽睽下，自然不会有下毒机会。",
        //    "When Immortal Zhisida distributed the medicine, Sect Leader Li was also present.\\nWith so many people around, there would naturally be no opportunity to poison anyone."),

        //// score 0, HardToParseSeam - {0} placeholder garbled into "The Japanese"
        //new("还需等待{0}日方能再次贿赂狱卒。", "Still waiting {0} The Japanese could again bribe the jailer"),

        //// score 10, UntranslatedPinyin - whole sentence left as Pinyin instead of translated
        //new("清风徐来，水波不兴，此剑法不急不躁，舒缓平稳，旨在锻炼经脉而非逞强伤人",
        //    "Qingfeng xu lai, shuibo bu xing, ci jianfa bu ji bu zao, shufan pingwen, zhan zhi lianlian jingmai er bu chengqiang sharen."),

        //// score 10, UntranslatedPinyin - short skill name, garbled/wrong Pinyin rendering
        //new("照夜白", "Zhonghuabeiyang"),

        //// score 10, GarbledNumber - <b> tag + name placement issues around a date
        new("口说无凭，正好<b>二月十五日</b>临近，\\n本门将举行每年一度的<b>门派大比</b>。",
            "Words are but wind, this is perfect timing.<b>February 15th </b>Approaching,\\nThe sect will hold its annual ceremony <b>Sect Tournament</b>。"),

        //// score 20, GarbledNumber - structured skill-effect text with #Placeholder# tokens and a numeric range
        //new("<b>奇技</b>：七十二洞研究奇门兵器，提升奇门威力。\\n<b>百出</b>：每满级一门奇门武学，根据武学品级额外提升0.5/1/1.5/2/2.5/3点奇门。",
        //    "<b>Strange Art</b>：Research the 72 Caves to enhance the power of the qimen weapons.\\n<b>Constant problems</b>：For each advanced Unorthodox martial art you master at level‑up, you receive an additional bonus based on the quality level of the martial art: 0.5/1/1.5/2/2.5/3 points of Qimen"),

        //// score 20, LostIdiom - classical poem, tests idiom/register judgment
        //new("贵逼人来不自由，龙翔凤翥势难收。满堂花醉三千客，一剑霜寒十四州。",
        //    "Compelled by one's status, the dragon soars and the phoenix rises, a momentum that cannot be halted. A sword's chill spans fourteen states as it intoxicates three thousand guests with its floral fragrance."),

        //// score 20, LostIdiom - short inn name, idiom-derived
        //new("悦来客栈", "Yue Coming Inn"),
    ];

    [Fact(DisplayName = "Probe Quality Review Reasoning (thinking mode)")]
    public async Task ProbeReviewReasoning()
    {
        if (Samples.Length == 0)
        {
            Console.WriteLine("QcThinkingProbeTests.Samples is empty - fill it in with real SOURCE/TRANSLATION pairs before running this.");
            return;
        }

        var results = await QualityReviewWorkflow.ProbeReviewReasoningAsync(WorkingDirectory, Samples, hooks: GameFileHandling.Hooks);

        var serializer = FanslationStudio.LlmKit.Utility.YamlHelper.CreateSerializer();
        var yaml = serializer.Serialize(results);
        FanslationStudio.LlmKit.Utility.FileHelper.WriteAllTextWithRetry($"{WorkingDirectory}/TestResults/QcThinkingProbe.yaml", yaml);
    }
}
