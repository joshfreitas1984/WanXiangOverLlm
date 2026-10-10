using FanslationStudio.LlmKit.Support;
using Translate;

namespace Tests;

public class CharacterLineContextTests
{
    private static TranslationLine Line(string text) => new() { Raw = text, Splits = [new TranslationSplit { Text = text }] };

    [Fact(DisplayName = "CharacterGenders.yaml loads and an empty-gender character is left out")]
    public void Table_Loads()
    {
        var characters = CharacterLineContext.Load(GameFileHandling.WorkingDirectory);

        Assert.NotEmpty(characters);
        Assert.Equal(LineContext.Female, characters["钟离雪"]);
        Assert.Equal(LineContext.Male, characters["万轻舟"]);
        // The player is never listed.
        Assert.False(characters.ContainsKey("主角"));
    }

    [Fact(DisplayName = "CharacterLineContext gives a line naming a listed character that character's gender")]
    public void Provide_NamedCharacter()
    {
        var listed = Line("钟离雪被抓住了。");
        var unlisted = Line("这件事与苏青云无关。");
        var lines = new List<TranslationLine> { listed, unlisted };

        var contexts = CharacterLineContext.Provide(GameFileHandling.WorkingDirectory, new TextFileToSplit { Path = "EventDialog.json" }, lines);

        Assert.Equal(LineContext.Female, contexts[listed.Splits[0]].Gender);
        Assert.True(contexts[listed.Splits[0]].GenderKnown);
        Assert.False(contexts.ContainsKey(unlisted.Splits[0]));
    }
}
