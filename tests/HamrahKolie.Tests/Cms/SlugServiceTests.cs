using HamrahKolie.Infrastructure.Services;

namespace HamrahKolie.Tests.Cms;

public class SlugServiceTests
{
    private readonly SlugService _slug = new();

    [Fact]
    public void Persian_title_becomes_dashed_slug()
    {
        var result = _slug.Generate("آشنایی با دیالیز");
        Assert.Equal("آشنایی-با-دیالیز", result);
    }

    [Fact]
    public void Arabic_letters_are_normalized_to_persian()
    {
        // ورودی با «ي» و «ك» عربی
        var result = _slug.Generate("علي كريم");
        Assert.Equal("علی-کریم", result);
    }

    [Fact]
    public void English_title_is_lowercased_and_punctuation_removed()
    {
        var result = _slug.Generate("Hello, World!");
        Assert.Equal("hello-world", result);
    }

    [Fact]
    public void Multiple_separators_collapse_to_single_dash()
    {
        var result = _slug.Generate("a   b---c");
        Assert.Equal("a-b-c", result);
    }

    [Fact]
    public async Task GenerateUnique_appends_suffix_when_taken()
    {
        var taken = new HashSet<string> { "test", "test-2" };
        var result = await _slug.GenerateUniqueAsync("test", s => Task.FromResult(taken.Contains(s)));
        Assert.Equal("test-3", result);
    }
}
