using HamrahKolie.Application.PageBuilder;
using HamrahKolie.Domain.Entities;

namespace HamrahKolie.Tests.PageBuilder;

public class SectionSettingsTests
{
    [Fact]
    public void GetStats_parses_items()
    {
        var s = new PageSection { SettingsJson = """{"stats":[{"value":"۱۲۰","label":"بیمار"},{"value":"۴۵","label":"روستا"}]}""" };
        var stats = s.GetStats();
        Assert.Equal(2, stats.Count);
        Assert.Equal("۱۲۰", stats[0].Value);
        Assert.Equal("روستا", stats[1].Label);
    }

    [Fact]
    public void GetCards_parses_items()
    {
        var s = new PageSection { SettingsJson = """{"cards":[{"title":"الف","text":"ب"}]}""" };
        var cards = s.GetCards();
        Assert.Single(cards);
        Assert.Equal("الف", cards[0].Title);
    }

    [Fact]
    public void GetCount_returns_value_or_fallback()
    {
        Assert.Equal(5, new PageSection { SettingsJson = """{"count":5}""" }.GetCount(3));
        Assert.Equal(3, new PageSection { SettingsJson = null }.GetCount(3));
        Assert.Equal(3, new PageSection { SettingsJson = "invalid json" }.GetCount(3));
    }

    [Fact]
    public void Invalid_json_returns_empty()
    {
        var s = new PageSection { SettingsJson = "not json" };
        Assert.Empty(s.GetStats());
        Assert.Empty(s.GetCards());
    }
}
