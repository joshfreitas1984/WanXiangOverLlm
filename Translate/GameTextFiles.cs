using FanslationStudio.LlmKit.Support;

namespace Translate;

public class GameTextFiles
{
    public static string[] FilesNotHandled = [
    ];

    // Files/Raw/Dumped also contains Effect.json, Equip.json, EventSkillUpdate.json, Formula.json,
    // PlayerPortrait.json and SkillCondition.json - deliberately left out of this list, they don't
    // need translating. Don't add them without checking with the user first.
    public static readonly TextFileToSplit[] TextFilesToSplit = [
        //new() {Path = "dumpedPrefabText.txt", TextFileType = TextFileType.PrefabText, AllowMissingColorTags = false},
        new() {Path = "dynamicStrings.txt", TextFileType = TextFileType.DynamicStrings, AllowMissingColorTags = false},

        new() {Path = "Achievement.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Assist.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        //new() {Path = "Audio.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Battle.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Birth.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Buff.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Condition.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Dice.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Dictionary.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Difficulty.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Ending.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Event.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "EventDice.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "EventNormal.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "EventPuzzle.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "EventResult.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "EventSelection.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Hero.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "HotKey.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Item.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Map.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Monster.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "News.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Point.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Property.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "PuzzleGroup.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Relation.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Switch.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "Talent.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "UIText.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },

        new() {Path = "Skill.json", TextFileType = TextFileType.RawJson, PackageOutput = true, },
        new() {Path = "EventDialog.json", TextFileType = TextFileType.RawJson, PackageOutput = true, IgnoreHtmlTagsInText = true},
    ];
}
