namespace Bloxstrap
{
    internal static class CommunityModAssetResolver
    {
        // Froststrap removed these assets from the config repository when it
        // migrated to its CDN. Keep a pinned, immutable GitHub snapshot as a
        // fallback so Community Mods do not depend on the CDN staying online.
        private const string ArchivedAssetRoot =
            "https://raw.githubusercontent.com/Froststrap/config/c734f271b523aaea3f7517991d8a528b191cff4c/";

        public static async Task<HttpResponseMessage> GetAsync(
            HttpClient client,
            string url,
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

            string? archivedUrl = GetArchivedUrl(url);
            if (archivedUrl is null)
                throw primaryException ?? new HttpRequestException($"Community Mod download failed: {url}");

            var archivedResponse = await client.GetAsync(archivedUrl, completionOption);
            archivedResponse.EnsureSuccessStatusCode();
            return archivedResponse;
        }

        private static string? GetArchivedUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return null;

            string path = uri.AbsolutePath.TrimStart('/');
            int assetPathIndex = path.IndexOf("Download/", StringComparison.OrdinalIgnoreCase);
            if (assetPathIndex < 0)
                assetPathIndex = path.IndexOf("Thumbnails/", StringComparison.OrdinalIgnoreCase);

            if (assetPathIndex < 0)
                return null;

            return ArchivedAssetRoot + path[assetPathIndex..];
        }
    }
}
