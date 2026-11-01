using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Platform.API.Clients;
using Platform.API.Extensions;

using BiblePlatform.UsfmReferences;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>();
builder.Services.AddBibleApiClients(builder.Configuration);

using var host = builder.Build();

var bibleClient = host.Services.GetRequiredService<IBibleClient>();
var passageClient = host.Services.GetRequiredService<IPassageClient>();
var languageClient = host.Services.GetRequiredService<ILanguageClient>();
var fontClient = host.Services.GetRequiredService<IFontClient>();
var licenseClient = host.Services.GetRequiredService<ILicenseClient>();
var organizationClient = host.Services.GetRequiredService<IOrganizationClient>();
var imageClient = host.Services.GetRequiredService<IImageClient>();
var verseOfTheDayClient = host.Services.GetRequiredService<IVerseOfTheDayClient>();
var searchClient = host.Services.GetRequiredService<ISearchClient>();

Console.WriteLine("Bible Platform SDK - Console Sample");
Console.WriteLine("====================================");

var versions = await bibleClient.GetVersionsAsync();
Console.WriteLine($"\nFound {versions.Data.Count} Bible version(s) on this page:");
foreach (var version in versions.Data.Take(5))
{
    Console.WriteLine($"  {version.Id,-6} {version.Abbreviation,-8} {version.Title}");
}

var versionId = versions.Data[0].Id;

var books = await bibleClient.GetBooksAsync(versionId);
Console.WriteLine($"\nFirst 5 books in version {versionId}:");
foreach (var book in books.Take(5))
{
    Console.WriteLine($"  {book.Usfm,-6} {book.Human} ({book.ChapterCount} chapters)");
}

var reference = Reference.FromString("JHN.3.16");
var passage = await passageClient.GetPassageAsync(versionId, reference);
Console.WriteLine($"\n{passage.Reference}:");
Console.WriteLine(passage.Content);

var languages = await languageClient.GetLanguagesAsync(pageSize: 5);
Console.WriteLine($"\nFirst {languages.Data.Count} language(s):");
foreach (var language in languages.Data)
{
    Console.WriteLine($"  {language.Id,-8} {language.LanguageSubtag,-6} {language.LocalizedName}");
}

var fonts = await fontClient.GetFontsAsync();
Console.WriteLine($"\nFound {fonts.Count} font famil(y/ies); first: {fonts.FirstOrDefault()?.Family ?? "(none)"}");

var licenses = await licenseClient.GetLicensesAsync(versionId);
Console.WriteLine($"\nFound {licenses.Count} license(s) for version {versionId}.");

var organizations = await organizationClient.GetOrganizationsAsync(pageSize: 5);
Console.WriteLine($"\nFirst {organizations.Data.Count} organization(s):");
foreach (var organization in organizations.Data)
{
    Console.WriteLine($"  {organization.Id,-8} {organization.Name}");
}

var images = await imageClient.GetImagesAsync(passageId: "JHN.3.16", pageSize: 3);
Console.WriteLine($"\nFound {images.Data.Count} verse-art image(s) for JHN.3.16.");

var verseOfTheDay = await verseOfTheDayClient.GetVerseOfTheDayAsync(DateTime.UtcNow.DayOfYear);
Console.WriteLine($"\nToday's verse of the day: {verseOfTheDay.PassageId}");

var searchResult = await searchClient.SearchVersesAsync("love", versionId, pageSize: 5);
Console.WriteLine($"\nFound {searchResult.Verses.Count} verse(s) matching 'love':");
foreach (var verse in searchResult.Verses)
{
    Console.WriteLine($"  {verse.Reference}");
}
