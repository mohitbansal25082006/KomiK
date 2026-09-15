using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Komik.Models;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Archives.SevenZip;

namespace Komik.Services;

/// <summary>
/// Metadata embedded in a comic as <c>ComicInfo.xml</c> (the ComicRack / Anansi schema, written by
/// KomiK Downloader and most comic tools): title, series, numbers, credits, summary and tags.
/// </summary>
public sealed class EmbeddedComicInfo
{
    public string? Title { get; init; }
    public string? Series { get; init; }
    public string? Number { get; init; }
    public string? Volume { get; init; }
    public string? Summary { get; init; }
    public int? Year { get; init; }
    public int? Month { get; init; }
    public int? Day { get; init; }
    public IReadOnlyList<string> Writers { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Artists { get; init; } = Array.Empty<string>();
    public string? Publisher { get; init; }
    public string? Web { get; init; }
    public string? LanguageIso { get; init; }
    public string? Manga { get; init; }

    /// <summary>Genres and tags together: trimmed, de-duplicated (case-insensitive), in their original order.</summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>"2021-03-14", "2021-03" or "2021" depending on how much of the date is known.</summary>
    public string? ReleaseDate
    {
        get
        {
            if (Year is not int y) return null;
            if (Month is not int m) return y.ToString(CultureInfo.InvariantCulture);
            if (Day is not int d) return $"{y:D4}-{m:D2}";
            return $"{y:D4}-{m:D2}-{d:D2}";
        }
    }

    public bool HasMetadata =>
        !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Series) || !string.IsNullOrWhiteSpace(Number) ||
        !string.IsNullOrWhiteSpace(Summary) || !string.IsNullOrWhiteSpace(Publisher) || Year.HasValue ||
        Writers.Count > 0 || Artists.Count > 0;

    public ComicMetadataEntity ToMetadata(long comicId, string libraryTitle) => new()
    {
        ComicId = comicId,
        Title = libraryTitle,
        SeriesName = Series,
        IssueNumber = Number,
        Writers = Writers.Count > 0 ? string.Join(", ", Writers) : null,
        Artists = Artists.Count > 0 ? string.Join(", ", Artists) : null,
        Publisher = Publisher,
        ReleaseDate = ReleaseDate,
        Summary = Summary,
        LastUpdated = DateTime.UtcNow
    };

    /// <summary>
    /// The title the library shows. The embedded title wins when it names the book on its own; a bare
    /// chapter or story title ("The Fight") that doesn't mention its series keeps the file name, which
    /// carries the series and number.
    /// </summary>
    public string ResolveLibraryTitle(string fileTitle)
    {
        string? title = Title?.Trim();
        if (string.IsNullOrEmpty(title)) return fileTitle;
        if (string.IsNullOrWhiteSpace(Series)) return title;

        string seriesKey = Key(Series!);
        string titleKey = Key(title);
        if (seriesKey.Length == 0 || titleKey.Contains(seriesKey, StringComparison.Ordinal)) return title;

        // The file name already holds the series; keep it unless it's just a download id.
        string fileKey = Key(fileTitle);
        return fileKey.Contains(seriesKey, StringComparison.Ordinal) ? fileTitle : $"{Series!.Trim()} - {title}";
    }

    private static string Key(string value) =>
        new string(value.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
}

/// <summary>
/// Finds and reads <c>ComicInfo.xml</c> from CBZ/ZIP, CBR/RAR and CB7/7Z archives and image folders.
/// Only that one small entry is read, never the pages, so library scans stay fast.
/// </summary>
public static class ComicInfoReader
{
    public const string FileName = "ComicInfo.xml";

    private const long MaxXmlBytes = 2 * 1024 * 1024;
    private const int MaxTags = 60;
    private const int MaxTagLength = 64;

    private static readonly Regex ListSeparators = new(@"\s*[,;|]\s*", RegexOptions.Compiled);

    /// <summary>Reads the embedded metadata of a comic file or folder, or null when there is none or it can't be read.</summary>
    public static EmbeddedComicInfo? TryRead(string path)
    {
        byte[]? xml = TryReadRaw(path);
        if (xml == null) return null;
        using var ms = new MemoryStream(xml, writable: false);
        return Parse(ms);
    }

    /// <summary>The raw bytes of the embedded ComicInfo.xml (e.g. to carry it into a converted CBZ), or null.</summary>
    public static byte[]? TryReadRaw(string path)
    {
        try
        {
            if (Directory.Exists(path)) return ReadFromFolder(path);
            if (!File.Exists(path)) return null;

            return Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".cbz" or ".zip" => ReadFromZip(path),
                ".cbr" or ".rar" => ReadFromSharpCompress(path, fs => RarArchive.OpenArchive(fs, new SharpCompress.Readers.ReaderOptions { LeaveStreamOpen = false })),
                ".cb7" or ".7z" => ReadFromSharpCompress(path, fs => SevenZipArchive.OpenArchive(fs, new SharpCompress.Readers.ReaderOptions { LeaveStreamOpen = false })),
                _ => null
            };
        }
        catch
        {
            // A damaged or locked archive simply has no readable metadata.
            return null;
        }
    }

    /// <summary>Parses ComicInfo.xml content. Returns null for malformed XML or a different root element.</summary>
    public static EmbeddedComicInfo? Parse(Stream xml)
    {
        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                MaxCharactersInDocument = MaxXmlBytes * 2
            };
            using var reader = XmlReader.Create(xml, settings);
            var doc = XDocument.Load(reader);
            var root = doc.Root;
            if (root == null || !root.Name.LocalName.Equals("ComicInfo", StringComparison.OrdinalIgnoreCase)) return null;

            string? Text(string name)
            {
                var el = root.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));
                string? value = el?.Value?.Trim();
                return string.IsNullOrEmpty(value) ? null : value;
            }

            int? Int(string name, int min, int max) =>
                int.TryParse(Text(name), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) && v >= min && v <= max ? v : null;

            var writers = People(Text("Writer")).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var artists = People(Text("Penciller"))
                .Concat(People(Text("Artist")))
                .Concat(People(Text("Inker")))
                .Concat(People(Text("Colorist")))
                .Concat(People(Text("CoverArtist")))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var tags = SplitTags(Text("Genre")).Concat(SplitTags(Text("Tags")))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxTags)
                .ToList();

            int? year = Int("Year", 1800, 3000);
            int? month = year.HasValue ? Int("Month", 1, 12) : null;
            int? day = month.HasValue ? Int("Day", 1, 31) : null;

            return new EmbeddedComicInfo
            {
                Title = Text("Title"),
                Series = Text("Series"),
                Number = NormalizeNumber(Text("Number")),
                Volume = NormalizeNumber(Text("Volume")),
                Summary = Text("Summary"),
                Year = year,
                Month = month,
                Day = day,
                Writers = writers,
                Artists = artists,
                Publisher = Text("Publisher"),
                Web = Text("Web"),
                LanguageIso = Text("LanguageISO"),
                Manga = Text("Manga"),
                Tags = tags
            };
        }
        catch
        {
            return null;
        }
    }

    public static EmbeddedComicInfo? Parse(string xml)
    {
        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(xml));
        return Parse(ms);
    }

    private static byte[]? ReadFromZip(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = new ZipArchive(fs, ZipArchiveMode.Read);
        var entry = PickEntry(archive.Entries, e => e.FullName, e => e.Length);
        if (entry == null) return null;
        using var stream = entry.Open();
        return ReadCapped(stream);
    }

    private static byte[]? ReadFromSharpCompress(string path, Func<Stream, IArchive> open)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var archive = open(fs);
        var entry = PickEntry(archive.Entries.Where(e => !e.IsDirectory && !e.IsEncrypted && e.Key != null), e => e.Key!, e => e.Size);
        if (entry == null) return null;
        using var stream = entry.OpenEntryStream();
        return ReadCapped(stream);
    }

    private static byte[]? ReadFromFolder(string path)
    {
        string file = Path.Combine(path, FileName);
        if (!File.Exists(file) || new FileInfo(file).Length > MaxXmlBytes) return null;
        return File.ReadAllBytes(file);
    }

    /// <summary>Reads a stream fully, giving up on anything larger than a sane ComicInfo.xml.</summary>
    private static byte[]? ReadCapped(Stream stream)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length > MaxXmlBytes) return null;
        }
        return buffer.ToArray();
    }

    /// <summary>ComicInfo.xml at the archive root wins; otherwise the shallowest one inside a folder.</summary>
    private static T? PickEntry<T>(IEnumerable<T> entries, Func<T, string> name, Func<T, long> size) where T : class =>
        entries
            .Select(e => (Entry: e, Path: name(e).Replace('\\', '/')))
            .Where(x => !x.Path.StartsWith("__MACOSX", StringComparison.OrdinalIgnoreCase) &&
                        x.Path.Split('/').Last().Equals(FileName, StringComparison.OrdinalIgnoreCase) &&
                        size(x.Entry) <= MaxXmlBytes)
            .OrderBy(x => x.Path.Count(c => c == '/'))
            .Select(x => x.Entry)
            .FirstOrDefault();

    private static IEnumerable<string> People(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? Enumerable.Empty<string>()
            : ListSeparators.Split(value).Select(p => p.Trim()).Where(p => p.Length > 0);

    private static IEnumerable<string> SplitTags(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) yield break;
        foreach (var part in ListSeparators.Split(value))
        {
            string tag = Regex.Replace(part, @"\s+", " ").Trim().Trim('#').Trim();
            if (tag.Length == 0 || tag.Length > MaxTagLength) continue;
            yield return tag;
        }
    }

    /// <summary>"015" → "15", "1.50" → "1.5"; anything that isn't a plain number is kept as written.</summary>
    private static string? NormalizeNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string trimmed = value.Trim();
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) && n >= 0
            ? n.ToString("0.###", CultureInfo.InvariantCulture)
            : trimmed;
    }
}
