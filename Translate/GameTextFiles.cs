namespace Translate;

public class GameTextFiles
{
    // "。" doesnt work like u think it would   
    public static string[] SplitCharactersList = [
            // "。"
            // ":", "<br>", "\\n", "-", "|"
        ];

    public static string[] FilesNotHandled = [
    ];

    public static readonly TextFileToSplit[] TextFilesToSplit = [
        new() {Path = "Achievement.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Assist.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Audio.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Battle.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Birth.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Buff.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Condition.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Dice.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Dictionary.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Difficulty.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Ending.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Event.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "EventDice.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "EventNormal.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "EventPuzzle.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "EventResult.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "EventSelection.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Hero.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "HotKey.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Item.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Map.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Monster.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "News.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Point.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Property.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "PuzzleGroup.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Relation.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Switch.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "Talent.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "UIText.json", PackageOutput = true, IgnoreHtmlTagsInText = true },

        new() {Path = "Skill.json", PackageOutput = true, IgnoreHtmlTagsInText = true },
        new() {Path = "EventDialog.json", PackageOutput = true, IgnoreHtmlTagsInText = true },

    ];
}
