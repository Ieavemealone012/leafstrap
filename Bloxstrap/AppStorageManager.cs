using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Bloxstrap.Enums.AppStoragePresets;

namespace Bloxstrap
{
    internal static class AppStorageManager
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static string FileLocation => Path.Combine(Paths.Roblox, "LocalStorage", "appStorage.json");

        public static void Apply()
        {
            bool background = App.Settings.Prop.EnableRobloxBackgroundApp;
            Mutate(storage =>
            {
                string state = background ? "true" : "false";
                storage["LaunchAtStartup"] = state;
                storage["MinimizeToTray"] = state;
                storage["SystemTrayModalShown"] = "true";
                ApplyTheme(storage);
            });

            if (!background)
                WindowsRegistry.RemoveRobloxStartupEntry();
        }

        private static void ApplyTheme(JsonObject storage)
        {
            RobloxTheme theme = App.Settings.Prop.RobloxTheme;
            if (theme == RobloxTheme.Default)
                return;

            string value = theme == RobloxTheme.Dark ? "dark" : "light";
            storage["AuthenticatedTheme"] = value;

            try
            {
                if (storage["UserId"] is not JsonValue idNode ||
                    !idNode.TryGetValue(out string? userId) ||
                    string.IsNullOrEmpty(userId))
                    return;

                JsonObject map = new();
                if (storage["DeviceLevelTheme"] is JsonValue mapNode &&
                    mapNode.TryGetValue(out string? raw) &&
                    !string.IsNullOrWhiteSpace(raw) &&
                    JsonNode.Parse(raw) is JsonObject parsed)
                    map = parsed;

                map[userId] = value;
                storage["DeviceLevelTheme"] = map.ToJsonString(SerializerOptions);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AppStorageManager::ApplyTheme", ex);
            }
        }

        private static void Mutate(Action<JsonObject> mutate)
        {
            string path = FileLocation;
            string? tempPath = null;
            try
            {
                JsonObject storage = new();
                if (File.Exists(path))
                {
                    path = new FileInfo(path).ResolveLinkTarget(true)?.FullName ?? path;
                    string contents = File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(contents) || JsonNode.Parse(contents) is not JsonObject parsed)
                        return;
                    storage = parsed;
                }

                string original = storage.ToJsonString(SerializerOptions);
                mutate(storage);
                string output = storage.ToJsonString(SerializerOptions);
                if (output == original)
                    return;

                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (!File.Exists(path))
                {
                    File.WriteAllText(path, output, new UTF8Encoding(false));
                    return;
                }

                tempPath = path + ".leafstrap-tmp";
                File.WriteAllText(tempPath, output, new UTF8Encoding(false));
                Filesystem.AssertReadOnly(path);
                File.Replace(tempPath, path, null);
                tempPath = null;
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("AppStorageManager::Mutate", ex);
            }
            finally
            {
                if (tempPath is not null)
                {
                    try { File.Delete(tempPath); }
                    catch { }
                }
            }
        }
    }
}
