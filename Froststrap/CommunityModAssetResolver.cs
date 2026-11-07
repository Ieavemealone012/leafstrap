// SPDX-License-Identifier: MPL-2.0

namespace Froststrap;

internal static class CommunityModAssetResolver
{
    // Immutable snapshot from immediately before Froststrap moved the mod
    // assets off GitHub. This keeps downloads working during CDN outages.
    private const string ArchivedAssetRoot =
        "https://raw.githubusercontent.com/Froststrap/config/c734f271b523aaea3f7517991d8a528b191cff4c/";

    public static async Task<HttpResponseMessage> GetAsync(
        HttpClient client,
        Uri url,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
    {
        Exception? primaryException = null;

        try
        {
            var response = await client.GetAsync(url, completionOption);
            if (response.IsSuccessStatusCode)
                return response;

            response.Dispose();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            primaryException = ex;
        }

        Uri? archivedUrl = GetArchivedUrl(url);
        if (archivedUrl is null)
            throw primaryException ?? new HttpRequestException($"Community Mod download failed: {url}");

        var archivedResponse = await client.GetAsync(archivedUrl, completionOption);
        archivedResponse.EnsureSuccessStatusCode();
        return archivedResponse;
    }

    private static Uri? GetArchivedUrl(Uri uri)
    {
        string path = uri.AbsolutePath.TrimStart('/');
        int assetPathIndex = path.IndexOf("Download/", StringComparison.OrdinalIgnoreCase);
        if (assetPathIndex < 0)
            assetPathIndex = path.IndexOf("Thumbnails/", StringComparison.OrdinalIgnoreCase);

        return assetPathIndex < 0
            ? null
            : new Uri(ArchivedAssetRoot + path[assetPathIndex..]);
    }
}
