namespace Translate;

public class GameTextFiles
{
    // "。" doesnt work like u think it would   
    public static string[] SplitCharactersList = [
            "\\n",
            // "。"
            // ":", "<br>", "\\n", "-", "|"
        ];

    public static string[] FilesNotHandled = [
    ];

    public static readonly TextFileToSplit[] TextFilesToSplit = [
        new() {Path = "Achievement.json", PackageOutput = true, },
        new() {Path = "Assist.json", PackageOutput = true, },
        new() {Path = "Audio.json", PackageOutput = true, },
        new() {Path = "Battle.json", PackageOutput = true, },
        new() {Path = "Birth.json", PackageOutput = true, },
        new() {Path = "Buff.json", PackageOutput = true, },
        new() {Path = "Condition.json", PackageOutput = true, },
        new() {Path = "Dice.json", PackageOutput = true, },
        new() {Path = "Dictionary.json", PackageOutput = true, },
        new() {Path = "Difficulty.json", PackageOutput = true, },
        new() {Path = "Ending.json", PackageOutput = true, },
        new() {Path = "Event.json", PackageOutput = true, },
        new() {Path = "EventDice.json", PackageOutput = true, },
        new() {Path = "EventNormal.json", PackageOutput = true, },
        new() {Path = "EventPuzzle.json", PackageOutput = true, },
        new() {Path = "EventResult.json", PackageOutput = true, },
        new() {Path = "EventSelection.json", PackageOutput = true, },
        new() {Path = "Hero.json", PackageOutput = true, },
        new() {Path = "HotKey.json", PackageOutput = true, },
        new() {Path = "Item.json", PackageOutput = true, },
        new() {Path = "Map.json", PackageOutput = true, },
        new() {Path = "Monster.json", PackageOutput = true, },
        new() {Path = "News.json", PackageOutput = true, },
        new() {Path = "Point.json", PackageOutput = true, },
        new() {Path = "Property.json", PackageOutput = true, },
        new() {Path = "PuzzleGroup.json", PackageOutput = true, },
        new() {Path = "Relation.json", PackageOutput = true, },
        new() {Path = "Switch.json", PackageOutput = true, },
        new() {Path = "Talent.json", PackageOutput = true, },
        new() {Path = "UIText.json", PackageOutput = true, },

        new() {Path = "Skill.json", PackageOutput = true, },
        new() {Path = "EventDialog.json", PackageOutput = true, },

    ];
}
