using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace BiblePlatform.UsfmReferences.Tests;

/// <summary>
/// Exhaustive, independently-transcribed coverage of the data tables in <see cref="BookCatalog"/>
/// (every canon entry, every book-name alias, every numbered-book stem/ordinal). Each expected
/// value here is copied by hand from the source at the time of writing -- it is deliberately NOT
/// derived from the production data at runtime, so that a corrupted entry in production (e.g. a
/// single mutated string literal) is caught instead of silently matching.
/// </summary>
public sealed class BookCatalogDataTests
{
    private readonly UsfmReferenceService _service = new();

    public static TheoryData<string, Canon> AllCanonEntries()
    {
        var data = new TheoryData<string, Canon>();
        data.Add("GEN", Canon.OldTestament);
        data.Add("EXO", Canon.OldTestament);
        data.Add("LEV", Canon.OldTestament);
        data.Add("NUM", Canon.OldTestament);
        data.Add("DEU", Canon.OldTestament);
        data.Add("JOS", Canon.OldTestament);
        data.Add("JDG", Canon.OldTestament);
        data.Add("RUT", Canon.OldTestament);
        data.Add("1SA", Canon.OldTestament);
        data.Add("2SA", Canon.OldTestament);
        data.Add("1KI", Canon.OldTestament);
        data.Add("2KI", Canon.OldTestament);
        data.Add("1CH", Canon.OldTestament);
        data.Add("2CH", Canon.OldTestament);
        data.Add("EZR", Canon.OldTestament);
        data.Add("NEH", Canon.OldTestament);
        data.Add("EST", Canon.OldTestament);
        data.Add("JOB", Canon.OldTestament);
        data.Add("PSA", Canon.OldTestament);
        data.Add("PRO", Canon.OldTestament);
        data.Add("ECC", Canon.OldTestament);
        data.Add("SNG", Canon.OldTestament);
        data.Add("ISA", Canon.OldTestament);
        data.Add("JER", Canon.OldTestament);
        data.Add("LAM", Canon.OldTestament);
        data.Add("EZK", Canon.OldTestament);
        data.Add("DAN", Canon.OldTestament);
        data.Add("HOS", Canon.OldTestament);
        data.Add("JOL", Canon.OldTestament);
        data.Add("AMO", Canon.OldTestament);
        data.Add("OBA", Canon.OldTestament);
        data.Add("JON", Canon.OldTestament);
        data.Add("MIC", Canon.OldTestament);
        data.Add("NAM", Canon.OldTestament);
        data.Add("HAB", Canon.OldTestament);
        data.Add("ZEP", Canon.OldTestament);
        data.Add("HAG", Canon.OldTestament);
        data.Add("ZEC", Canon.OldTestament);
        data.Add("MAL", Canon.OldTestament);
        data.Add("MAT", Canon.NewTestament);
        data.Add("MRK", Canon.NewTestament);
        data.Add("LUK", Canon.NewTestament);
        data.Add("JHN", Canon.NewTestament);
        data.Add("ACT", Canon.NewTestament);
        data.Add("ROM", Canon.NewTestament);
        data.Add("1CO", Canon.NewTestament);
        data.Add("2CO", Canon.NewTestament);
        data.Add("GAL", Canon.NewTestament);
        data.Add("EPH", Canon.NewTestament);
        data.Add("PHP", Canon.NewTestament);
        data.Add("COL", Canon.NewTestament);
        data.Add("1TH", Canon.NewTestament);
        data.Add("2TH", Canon.NewTestament);
        data.Add("1TI", Canon.NewTestament);
        data.Add("2TI", Canon.NewTestament);
        data.Add("TIT", Canon.NewTestament);
        data.Add("PHM", Canon.NewTestament);
        data.Add("HEB", Canon.NewTestament);
        data.Add("JAS", Canon.NewTestament);
        data.Add("1PE", Canon.NewTestament);
        data.Add("2PE", Canon.NewTestament);
        data.Add("1JN", Canon.NewTestament);
        data.Add("2JN", Canon.NewTestament);
        data.Add("3JN", Canon.NewTestament);
        data.Add("JUD", Canon.NewTestament);
        data.Add("REV", Canon.NewTestament);
        data.Add("TOB", Canon.Apocrypha);
        data.Add("JDT", Canon.Apocrypha);
        data.Add("ESG", Canon.Apocrypha);
        data.Add("WIS", Canon.Apocrypha);
        data.Add("SIR", Canon.Apocrypha);
        data.Add("BAR", Canon.Apocrypha);
        data.Add("LJE", Canon.Apocrypha);
        data.Add("S3Y", Canon.Apocrypha);
        data.Add("SUS", Canon.Apocrypha);
        data.Add("BEL", Canon.Apocrypha);
        data.Add("1MA", Canon.Apocrypha);
        data.Add("2MA", Canon.Apocrypha);
        data.Add("3MA", Canon.Apocrypha);
        data.Add("4MA", Canon.Apocrypha);
        data.Add("1ES", Canon.Apocrypha);
        data.Add("2ES", Canon.Apocrypha);
        data.Add("MAN", Canon.Apocrypha);
        data.Add("PS2", Canon.Apocrypha);
        data.Add("ODA", Canon.Apocrypha);
        data.Add("PSS", Canon.Apocrypha);
        data.Add("3ES", Canon.Apocrypha);
        data.Add("EZA", Canon.Apocrypha);
        data.Add("5EZ", Canon.Apocrypha);
        data.Add("6EZ", Canon.Apocrypha);
        data.Add("DAG", Canon.Apocrypha);
        data.Add("PS3", Canon.Apocrypha);
        data.Add("2BA", Canon.Apocrypha);
        data.Add("LBA", Canon.Apocrypha);
        data.Add("JUB", Canon.Apocrypha);
        data.Add("ENO", Canon.Apocrypha);
        data.Add("1MQ", Canon.Apocrypha);
        data.Add("2MQ", Canon.Apocrypha);
        data.Add("3MQ", Canon.Apocrypha);
        data.Add("REP", Canon.Apocrypha);
        data.Add("4BA", Canon.Apocrypha);
        data.Add("LAO", Canon.Apocrypha);
        data.Add("LKA", Canon.NewTestament);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllCanonEntries))]
    public void GetCanon_AllKnownCodes_ReturnExactCanon(string code, Canon expected)
    {
        BookCatalog.GetCanon(code).Should().Be(expected);
        BookCatalog.IsKnownBook(code).Should().BeTrue();
    }

    [Fact]
    public void Books_HasExactlyExpectedCount()
    {
        BookCatalog.Books.Should().HaveCount(103);
    }

    public static TheoryData<string, string> AllBookNameAliases()
    {
        var data = new TheoryData<string, string>();
        data.Add("genesis", "GEN");
        data.Add("gen", "GEN");
        data.Add("gn", "GEN");
        data.Add("exodus", "EXO");
        data.Add("exod", "EXO");
        data.Add("exo", "EXO");
        data.Add("ex", "EXO");
        data.Add("leviticus", "LEV");
        data.Add("lev", "LEV");
        data.Add("lv", "LEV");
        data.Add("numbers", "NUM");
        data.Add("num", "NUM");
        data.Add("nm", "NUM");
        data.Add("deuteronomy", "DEU");
        data.Add("deut", "DEU");
        data.Add("deu", "DEU");
        data.Add("dt", "DEU");
        data.Add("joshua", "JOS");
        data.Add("josh", "JOS");
        data.Add("jos", "JOS");
        data.Add("jsh", "JOS");
        data.Add("judges", "JDG");
        data.Add("judg", "JDG");
        data.Add("jdg", "JDG");
        data.Add("jdgs", "JDG");
        data.Add("ruth", "RUT");
        data.Add("rut", "RUT");
        data.Add("rth", "RUT");
        data.Add("ezra", "EZR");
        data.Add("ezr", "EZR");
        data.Add("nehemiah", "NEH");
        data.Add("neh", "NEH");
        data.Add("esther", "EST");
        data.Add("esth", "EST");
        data.Add("est", "EST");
        data.Add("job", "JOB");
        data.Add("jb", "JOB");
        data.Add("psalms", "PSA");
        data.Add("psalm", "PSA");
        data.Add("psa", "PSA");
        data.Add("pss", "PSA");
        data.Add("psm", "PSA");
        data.Add("ps", "PSA");
        data.Add("proverbs", "PRO");
        data.Add("prov", "PRO");
        data.Add("pro", "PRO");
        data.Add("prv", "PRO");
        data.Add("ecclesiastes", "ECC");
        data.Add("eccles", "ECC");
        data.Add("eccl", "ECC");
        data.Add("ecc", "ECC");
        data.Add("qoh", "ECC");
        data.Add("qoheleth", "ECC");
        data.Add("songofsongs", "SNG");
        data.Add("songofsolomon", "SNG");
        data.Add("song", "SNG");
        data.Add("sng", "SNG");
        data.Add("sos", "SNG");
        data.Add("canticles", "SNG");
        data.Add("cant", "SNG");
        data.Add("isaiah", "ISA");
        data.Add("isa", "ISA");
        data.Add("jeremiah", "JER");
        data.Add("jer", "JER");
        data.Add("lamentations", "LAM");
        data.Add("lam", "LAM");
        data.Add("ezekiel", "EZK");
        data.Add("ezek", "EZK");
        data.Add("ezk", "EZK");
        data.Add("eze", "EZK");
        data.Add("daniel", "DAN");
        data.Add("dan", "DAN");
        data.Add("dn", "DAN");
        data.Add("hosea", "HOS");
        data.Add("hos", "HOS");
        data.Add("joel", "JOL");
        data.Add("jol", "JOL");
        data.Add("jl", "JOL");
        data.Add("amos", "AMO");
        data.Add("amo", "AMO");
        data.Add("obadiah", "OBA");
        data.Add("obad", "OBA");
        data.Add("oba", "OBA");
        data.Add("ob", "OBA");
        data.Add("jonah", "JON");
        data.Add("jon", "JON");
        data.Add("jnh", "JON");
        data.Add("micah", "MIC");
        data.Add("mic", "MIC");
        data.Add("nahum", "NAM");
        data.Add("nah", "NAM");
        data.Add("nam", "NAM");
        data.Add("habakkuk", "HAB");
        data.Add("hab", "HAB");
        data.Add("hb", "HAB");
        data.Add("zephaniah", "ZEP");
        data.Add("zeph", "ZEP");
        data.Add("zep", "ZEP");
        data.Add("haggai", "HAG");
        data.Add("hag", "HAG");
        data.Add("hg", "HAG");
        data.Add("zechariah", "ZEC");
        data.Add("zech", "ZEC");
        data.Add("zec", "ZEC");
        data.Add("malachi", "MAL");
        data.Add("mal", "MAL");
        data.Add("matthew", "MAT");
        data.Add("matt", "MAT");
        data.Add("mat", "MAT");
        data.Add("mt", "MAT");
        data.Add("mark", "MRK");
        data.Add("mrk", "MRK");
        data.Add("mar", "MRK");
        data.Add("mk", "MRK");
        data.Add("luke", "LUK");
        data.Add("luk", "LUK");
        data.Add("lk", "LUK");
        data.Add("john", "JHN");
        data.Add("jhn", "JHN");
        data.Add("jn", "JHN");
        data.Add("acts", "ACT");
        data.Add("act", "ACT");
        data.Add("romans", "ROM");
        data.Add("rom", "ROM");
        data.Add("galatians", "GAL");
        data.Add("gal", "GAL");
        data.Add("ephesians", "EPH");
        data.Add("eph", "EPH");
        data.Add("philippians", "PHP");
        data.Add("phil", "PHP");
        data.Add("php", "PHP");
        data.Add("colossians", "COL");
        data.Add("col", "COL");
        data.Add("titus", "TIT");
        data.Add("tit", "TIT");
        data.Add("philemon", "PHM");
        data.Add("philem", "PHM");
        data.Add("phlm", "PHM");
        data.Add("phm", "PHM");
        data.Add("hebrews", "HEB");
        data.Add("heb", "HEB");
        data.Add("james", "JAS");
        data.Add("jas", "JAS");
        data.Add("jude", "JUD");
        data.Add("jud", "JUD");
        data.Add("revelation", "REV");
        data.Add("rev", "REV");
        data.Add("rv", "REV");
        data.Add("apocalypse", "REV");
        data.Add("apoc", "REV");
        data.Add("tobit", "TOB");
        data.Add("tob", "TOB");
        data.Add("tb", "TOB");
        data.Add("judith", "JDT");
        data.Add("jdt", "JDT");
        data.Add("jdth", "JDT");
        data.Add("wisdomofsolomon", "WIS");
        data.Add("wisdom", "WIS");
        data.Add("wis", "WIS");
        data.Add("wissol", "WIS");
        data.Add("sirach", "SIR");
        data.Add("ecclesiasticus", "SIR");
        data.Add("sir", "SIR");
        data.Add("bensira", "SIR");
        data.Add("baruch", "BAR");
        data.Add("bar", "BAR");
        data.Add("letterofjeremiah", "LJE");
        data.Add("letjer", "LJE");
        data.Add("lje", "LJE");
        data.Add("epistleofjeremiah", "LJE");
        data.Add("songofthethreeyoungmen", "S3Y");
        data.Add("songofthree", "S3Y");
        data.Add("prayerofazariah", "S3Y");
        data.Add("praz", "S3Y");
        data.Add("s3y", "S3Y");
        data.Add("susanna", "SUS");
        data.Add("sus", "SUS");
        data.Add("belandthedragon", "BEL");
        data.Add("bel", "BEL");
        data.Add("prayerofmanasseh", "MAN");
        data.Add("prman", "MAN");
        return data;
    }

    [Theory]
    [MemberData(nameof(AllBookNameAliases))]
    public void ConvertBookNameToUsfm_AllKnownAliases_ResolveToExactCode(string alias, string expected)
    {
        _service.ConvertBookNameToUsfm(alias).Should().Be(expected);
    }

    public static TheoryData<string, int, string> AllNumberedBookOrdinals()
    {
        var data = new TheoryData<string, int, string>();
        data.Add("samuel", 1, "1SA");
        data.Add("samuel", 2, "2SA");
        data.Add("sam", 1, "1SA");
        data.Add("sam", 2, "2SA");
        data.Add("sa", 1, "1SA");
        data.Add("sa", 2, "2SA");
        data.Add("sm", 1, "1SA");
        data.Add("sm", 2, "2SA");
        data.Add("kings", 1, "1KI");
        data.Add("kings", 2, "2KI");
        data.Add("kgs", 1, "1KI");
        data.Add("kgs", 2, "2KI");
        data.Add("kg", 1, "1KI");
        data.Add("kg", 2, "2KI");
        data.Add("ki", 1, "1KI");
        data.Add("ki", 2, "2KI");
        data.Add("chronicles", 1, "1CH");
        data.Add("chronicles", 2, "2CH");
        data.Add("chron", 1, "1CH");
        data.Add("chron", 2, "2CH");
        data.Add("chr", 1, "1CH");
        data.Add("chr", 2, "2CH");
        data.Add("ch", 1, "1CH");
        data.Add("ch", 2, "2CH");
        data.Add("corinthians", 1, "1CO");
        data.Add("corinthians", 2, "2CO");
        data.Add("cor", 1, "1CO");
        data.Add("cor", 2, "2CO");
        data.Add("co", 1, "1CO");
        data.Add("co", 2, "2CO");
        data.Add("thessalonians", 1, "1TH");
        data.Add("thessalonians", 2, "2TH");
        data.Add("thess", 1, "1TH");
        data.Add("thess", 2, "2TH");
        data.Add("thes", 1, "1TH");
        data.Add("thes", 2, "2TH");
        data.Add("th", 1, "1TH");
        data.Add("th", 2, "2TH");
        data.Add("timothy", 1, "1TI");
        data.Add("timothy", 2, "2TI");
        data.Add("tim", 1, "1TI");
        data.Add("tim", 2, "2TI");
        data.Add("ti", 1, "1TI");
        data.Add("ti", 2, "2TI");
        data.Add("peter", 1, "1PE");
        data.Add("peter", 2, "2PE");
        data.Add("pet", 1, "1PE");
        data.Add("pet", 2, "2PE");
        data.Add("pe", 1, "1PE");
        data.Add("pe", 2, "2PE");
        data.Add("john", 1, "1JN");
        data.Add("john", 2, "2JN");
        data.Add("john", 3, "3JN");
        data.Add("jn", 1, "1JN");
        data.Add("jn", 2, "2JN");
        data.Add("jn", 3, "3JN");
        data.Add("jhn", 1, "1JN");
        data.Add("jhn", 2, "2JN");
        data.Add("jhn", 3, "3JN");
        data.Add("jo", 1, "1JN");
        data.Add("jo", 2, "2JN");
        data.Add("jo", 3, "3JN");
        data.Add("maccabees", 1, "1MA");
        data.Add("maccabees", 2, "2MA");
        data.Add("macc", 1, "1MA");
        data.Add("macc", 2, "2MA");
        data.Add("mac", 1, "1MA");
        data.Add("mac", 2, "2MA");
        data.Add("ma", 1, "1MA");
        data.Add("ma", 2, "2MA");
        data.Add("esdras", 1, "1ES");
        data.Add("esdras", 2, "2ES");
        data.Add("esd", 1, "1ES");
        data.Add("esd", 2, "2ES");
        data.Add("es", 1, "1ES");
        data.Add("es", 2, "2ES");
        return data;
    }

    [Theory]
    [MemberData(nameof(AllNumberedBookOrdinals))]
    public void ConvertBookNameToUsfm_AllNumberedBookStems_ResolveToExactCode(string stem, int number, string expected)
    {
        _service.ConvertBookNameToUsfm($"{number}{stem}").Should().Be(expected);
    }
}
