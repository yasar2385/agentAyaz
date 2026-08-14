namespace ImpactSupport.Api.TestCaseViewer.Services;

public enum GoogleDriveUrlKind
{
    Unknown,
    Spreadsheet,
    Folder
}

public sealed record GoogleDriveUrlInfo(
    GoogleDriveUrlKind Kind,
    string Id,
    int? SheetGid,
    string NormalizedUrl);

public interface IGoogleDriveUrlParser
{
    GoogleDriveUrlInfo Parse(string url);
}

public sealed class GoogleDriveUrlParser : IGoogleDriveUrlParser
{
    public GoogleDriveUrlInfo Parse(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("url must be provided", nameof(url));
        }

        var value = url.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return new GoogleDriveUrlInfo(GoogleDriveUrlKind.Spreadsheet, value, null, $"https://docs.google.com/spreadsheets/d/{value}/edit");
        }

        var gid = ReadGid(uri);
        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var spreadsheetIndex = Array.FindIndex(segments, segment => segment.Equals("d", StringComparison.OrdinalIgnoreCase));
        if (uri.Host.Contains("docs.google", StringComparison.OrdinalIgnoreCase)
            && segments.Any(segment => segment.Equals("spreadsheets", StringComparison.OrdinalIgnoreCase))
            && spreadsheetIndex >= 0
            && spreadsheetIndex + 1 < segments.Length)
        {
            var id = segments[spreadsheetIndex + 1];
            return new GoogleDriveUrlInfo(GoogleDriveUrlKind.Spreadsheet, id, gid, $"https://docs.google.com/spreadsheets/d/{id}/edit");
        }

        var folderIndex = Array.FindIndex(segments, segment => segment.Equals("folders", StringComparison.OrdinalIgnoreCase));
        if (folderIndex >= 0 && folderIndex + 1 < segments.Length)
        {
            var id = segments[folderIndex + 1];
            return new GoogleDriveUrlInfo(GoogleDriveUrlKind.Folder, id, null, $"https://drive.google.com/drive/folders/{id}");
        }

        throw new ArgumentException("Only Google Sheets spreadsheet URLs and Google Drive folder URLs are supported.", nameof(url));
    }

    private static int? ReadGid(Uri uri)
    {
        var query = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in query)
        {
            var pieces = part.Split('=', 2);
            if (pieces.Length == 2
                && pieces[0].Equals("gid", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(Uri.UnescapeDataString(pieces[1]), out var value))
            {
                return value;
            }
        }

        var fragment = uri.Fragment.TrimStart('#');
        foreach (var part in fragment.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            if (pieces.Length == 2
                && pieces[0].Equals("gid", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(Uri.UnescapeDataString(pieces[1]), out var value))
            {
                return value;
            }
        }

        return null;
    }
}
