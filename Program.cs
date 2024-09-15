using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System;
using System.IO.Compression;

namespace AutoUpdater
{
    public class Program
    {
        private static string VersionPath => Path.Combine(Directory.GetCurrentDirectory(), "Version.txt");
        public static void Main(string[] args)
        {
            Settings settings = Settings.Load();

            if (string.IsNullOrEmpty(settings.BasePath))
            {
                Log("Missing base path, setting to default.");
                settings.BasePath = Directory.GetCurrentDirectory();
            }

            if (string.IsNullOrEmpty(settings.FileName))
                throw new Exception("Missing FileName");

            Release release = GetRelease();

            if (!File.Exists(VersionPath))
            {
                Update(release);

                Log($"Installation Complete. Launching program..");
                LaunchProgram();
                return;
            }

            string currentVersion = File.ReadAllText(VersionPath);

            if (currentVersion != release.TagName)
            {
                Log($"Update likely needed, updated to version tag '{release.TagName}'");
                Update(release);

                Log($"Installation Complete. Launching program..");
                LaunchProgram();
                return;
            }

            Log("No update needed, opening file..");
            LaunchProgram();
        }

        private static void Update(Release release)
        {
            Log($"Update found. Beginning to download update '{release.TagName}'");

            var task =  Task.Run(async () =>
            {
                using (HttpClient client = new())
                {
                    string downloadUrl = release.Assets[Settings.Instance.AssetIndex].BrowserDownloadUrl;

                    if (string.IsNullOrEmpty(downloadUrl))
                    {
                        Log("Failed to get download url. Launching Program..");
                        return;
                    }

                    HttpResponseMessage downloadResponse = await client.GetAsync(downloadUrl);

                    if (!downloadResponse.IsSuccessStatusCode)
                    {
                        Log($"Failed to download. " +
                                          $"\nStatus Code: {downloadResponse.StatusCode}" +
                                          $"\nReason: {downloadResponse.ReasonPhrase}");
                        return;
                    }

                    string updatePath = Path.Combine(Directory.GetCurrentDirectory(), "update.zip");

                    using (var contentStream = await downloadResponse.Content.ReadAsStreamAsync())
                    {
                        using (FileStream fileStream = File.Create(updatePath))
                        {
                            long totalRead = 0;
                            int bufferSize = 8096; // Start with a reasonably small buffer size
                            byte[] buffer = new byte[bufferSize];
                            int bytesRead;

                            Log("%0 Downloaded..");
                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                int percentage = ((int)((double)totalRead / contentStream.Length * 100));

                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                Log($"%{percentage} Downloaded...");
                            }

                            fileStream.Close();
                            UnpackUpdate(release, updatePath);
                        }
                    }
                };
            });

            task.Wait();
        }

        private static void UnpackUpdate(Release release, string updatePath)
        {
            using (var update = ZipFile.OpenRead(updatePath))
            {
                if (Settings.Instance.DeleteAll)
                    Util.DeleteAll(Settings.Instance.BasePath);

                foreach (var entry in update.Entries)
                {
                    try
                    {
                        string path = Path.Combine(Settings.Instance.BasePath, entry.FullName);
                        string directory = Path.GetDirectoryName(path);

                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        entry.ExtractToFile(Path.Combine(Settings.Instance.BasePath, entry.FullName));
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to extract: {entry.FullName}. Skipping...");
                        Log(ex.Message);
                        continue;
                    }
                }

                update.Dispose();
                File.Delete(updatePath);
            }

            File.WriteAllText(VersionPath, release.TagName);
        }
        private static Release GetRelease()
        {
            using (HttpClient client = new())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("User-Agent", "GithubAutoUpdater");
                HttpResponseMessage response = client.GetAsync(Settings.Instance.GithubAPI).Result;

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = response.Content.ReadAsStringAsync().Result;
                    var releaseData = JsonSerializer.Deserialize<Release>(responseBody);
                    return releaseData;
                }
                else
                    return null;
            }
        }

        private static void LaunchProgram()
        {
            if (!Settings.Instance.AutoOpen)
            {
                Log("Press Enter to launch program");
                var key = Console.ReadKey();

                if (key.Key != ConsoleKey.Enter)
                    Process.GetCurrentProcess().Kill();
            }

            string launchFile = Util.FindFile(Settings.Instance.BasePath, Settings.Instance.FileName);

            if (launchFile == null)
                throw new Exception("Failed to find launch file.");

            ProcessStartInfo processInfo = new ProcessStartInfo
            {
                FileName = launchFile,
                UseShellExecute = true
            };

            Process.Start(processInfo);
        }

        private static void Log(string txt)
        {
            Console.WriteLine(txt);
        }
    }

    public class Asset
    {
        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }

    public class Release
    {
        [JsonPropertyName("assets")]
        public List<Asset> Assets { get; set; }

        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }
    }
}
