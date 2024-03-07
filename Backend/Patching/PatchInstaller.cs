/* Copyright (c) FFXIVQuickLauncher https://github.com/goatcorp/FFXIVQuickLauncher/blob/master/LICENSE
 *
 * Modified to fit the needs of the project.
 */

using System.Collections.Concurrent;
using System.Net;
using DERPWebsite.Patching.ZiPatch;
using DERPWebsite.Patching.ZiPatch.Util;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;

namespace DERPWebsite.Patching;

public class PatchInstaller
{
    private readonly HttpClient _httpClient = new();
    private readonly GraphQLHttpClient _graphQLClient = new("https://thaliak.xiv.dev/graphql/2022-08-14", new SystemTextJsonSerializer());
    private readonly IReadOnlyList<PatchInstallerRepoOptions> _repoOptions = new[] { new PatchInstallerRepoOptions("", "4e9a232b"), new PatchInstallerRepoOptions("sqpack/ex1", "6b936f08"), new PatchInstallerRepoOptions("sqpack/ex2", "f29a3eb2"), new PatchInstallerRepoOptions("sqpack/ex3", "859d0e24"), new PatchInstallerRepoOptions("sqpack/ex4", "1bf99b87") };
    private readonly ILogger<PatchInstaller> _logger;

    public (string description, string versionString, string discoveredVer, float progress, float chunkProgress, float fileProgress) CurrentInstallProgress = ("", "", "", 0, 0, 0);
    public ConcurrentDictionary<Guid, Tuple<string, string, float>> DownloadProgress = new();

    public PatchInstaller(ILogger<PatchInstaller> logger)
    {
        _logger = logger;
    }

    public async Task<List<Tuple<string, string, string, string>>> CheckVersions()
    {
        var result = new List<Tuple<string, string, string, string>>();

        foreach (var repo in _repoOptions)
        {
            var slug = repo.Slug;
            var verPath = repo.VerPath;
#if DEBUG
            verPath = Path.Combine(@"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game", verPath);
#else
            verPath = Path.Combine(AppContext.BaseDirectory, "ffxiv", verPath);
#endif
            var ver = "0.0.0.0.0";
            if (File.Exists(verPath))
            {
                ver = await File.ReadAllTextAsync(verPath);
            }
            else
            {
                _logger.LogWarning($"Could not find file {verPath}");
            }
            var version = await _graphQLClient.SendQueryAsync<ThaliakVersionQueryResponse>(new GraphQLRequest
            {
                Query =
"""
query($repoId:String) {
  repository(slug: $repoId) {
    description
    slug
    name
    latestVersion {
      versionString
    }
  }
}
""",
                Variables = new
                {
                    repoId = slug
                }
            });
            result.Add(new Tuple<string, string, string, string>(slug, version.Data.Repository.Description!, ver, version.Data.Repository.LatestVersion!.VersionString));
        }

        return result;
    }

    public async Task Update()
    {
        var tempDir = Directory.CreateTempSubdirectory();
        _logger.LogInformation($"Created temp dir: {tempDir.FullName}");
#if DEBUG
        var gamePath = Path.Combine(@"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game");
#else
        var gamePath = Path.Combine(AppContext.BaseDirectory, "ffxiv");
#endif
        _logger.LogInformation($"Assembled gamePath to {gamePath}");
        foreach (var (slug, description, curVer, discoveredVer) in await CheckVersions())
        {
            DownloadProgress = new ConcurrentDictionary<Guid, Tuple<string, string, float>>();
            CurrentInstallProgress = (description, curVer, discoveredVer, 0, 0, 0);
            var verPath = _repoOptions.First(t => t.Slug == slug).VerPath;
#if DEBUG
            verPath = Path.Combine(@"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game", verPath);
#else
            verPath = Path.Combine(AppContext.BaseDirectory, "ffxiv", verPath);
#endif
            _logger.LogInformation($"Checking update for {slug}");
            if (curVer == discoveredVer)
            {
                _logger.LogInformation($"No update for {slug}");
                continue;
            }
            var patches = (GraphQLHttpResponse<ThaliakPatchListQueryResponse>)await _graphQLClient.SendQueryAsync<ThaliakPatchListQueryResponse>(new GraphQLRequest
            {
                Query =
                    """
                    query($repoId:String) {
                      repository(slug: $repoId) {
                        versions{
                          versionString
                          isActive
                          prerequisiteVersions{
                            versionString
                          }
                          patches{
                            url
                          }
                        }
                      }
                    }
                    """,
                Variables = new
                {
                    repoId = slug
                }
            });
            _logger.LogInformation($"Queried Thaliak and got statusCode: {patches.StatusCode:G}");
            if (patches.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError($"Could not get patches for {slug}");
                continue;
            }
            _logger.LogInformation($"Recieved {patches.Data.Repository.Versions?.Length} versions");
            _logger.LogInformation($"Created list");
            var where = patches.Data.Repository.Versions!.Where(t => t.IsActive!.Value).ToArray();
            _logger.LogInformation($"Filtered pre list");
            var order = where.OrderByVersion(t => (FFXIVVersion)t.VersionString);
            _logger.LogInformation($"Ordered pre list");
            var list = order.SkipWhile(t => (FFXIVVersion)t.VersionString <= (FFXIVVersion)curVer).ToList();
            _logger.LogInformation($"Filtered list");
            if (list.Count == 0 && curVer != discoveredVer) list = patches.Data.Repository.Versions!.Where(t => t.IsActive!.Value).OrderByVersion(t => (FFXIVVersion)t.VersionString).ToList();
            _logger.LogInformation($"Found {list.Count} updates for {slug}");
            var orderedList = OrderVersionPrereq(list.First(t => t.VersionString == discoveredVer), list, discoveredVer, new List<ThaliakVersion>());
            _logger.LogInformation($"Ordered list");
            var downloadTasks = orderedList.Select(patch => new Task<Tuple<ThaliakVersion, FileInfo>>(() => DownloadPatch(patch, tempDir, description).ConfigureAwait(false).GetAwaiter().GetResult())).ToList();
            var timeCheck = TimeSpan.FromMilliseconds(500);
            var downloadInitTask = Task.Run(async () =>
            {
                while (downloadTasks.Any(t => t.Status == TaskStatus.Created))
                {
                    while (downloadTasks.Count(t => t.Status is TaskStatus.WaitingToRun or TaskStatus.Running) > 8)
                    {
                        await Task.Delay(timeCheck);
                    }
                    var task = downloadTasks.First(t => t.Status == TaskStatus.Created);
                    task.Start();
                    await Task.Delay(timeCheck);
                }
            });
            var patchIndex = 0;
            var patchCheck = Task.Run(async () =>
            {
                while (patchIndex != downloadTasks.Count)
                {
                    var ((versionString, _, _, _), fileInfo) = await downloadTasks[patchIndex].WaitAsync(CancellationToken.None);
                    CurrentInstallProgress = (description, versionString, discoveredVer, patchIndex / (float)downloadTasks.Count, 0, 0);
                    _logger.LogInformation($"Installing {versionString} for {slug}");
                    InstallPatch(fileInfo.FullName, gamePath);
                    await File.WriteAllTextAsync(verPath, versionString);
                    patchIndex++;
                }
            });
            await Task.WhenAll(downloadInitTask, patchCheck);
            await File.WriteAllTextAsync(verPath, discoveredVer);
            _logger.LogInformation($"Done installing updates for {slug}");
        }
        tempDir.Delete();
    }

    ThaliakVersion[] OrderVersionPrereq(ThaliakVersion top,
        List<ThaliakVersion> list, string discoveredVer, List<ThaliakVersion> traced)
    {
        traced.Add(top);
        if (top.PrerequisiteVersions == null || top.PrerequisiteVersions.Any(t => t.VersionString == discoveredVer)) return
        new[] { top };
        var ret = Array.Empty<ThaliakVersion>();
        foreach (var (versionString, _, _, _) in top.PrerequisiteVersions.OrderByVersion(t => (FFXIVVersion)t.VersionString))
        {
            var prev = list.FirstOrDefault(t => t.VersionString == versionString);
            if (prev == null || ret.Contains(prev) || traced.Contains(prev)) continue;
            ret = ret.Concat(OrderVersionPrereq(prev, list, discoveredVer, traced)).ToArray();
        }
        return ret.Concat(new[] { top }).ToArray();
    }

    async Task<Tuple<ThaliakVersion, FileInfo>> DownloadPatch(ThaliakVersion version, DirectoryInfo tempDir, string desc)
    {
        var guid = Guid.NewGuid();
        DownloadProgress.TryAdd(guid, new Tuple<string, string, float>(desc, version.VersionString, 0));
        var patch = version.Patches!.First();
        var patchFile = new FileInfo(Path.Combine(tempDir.FullName, Path.GetFileName(patch.Url)));
        if (patchFile.Exists)
        {
            _logger.LogInformation("Patch {0} already downloaded", patchFile.Name);
            DownloadProgress.TryRemove(guid, out _);
            return new Tuple<ThaliakVersion, FileInfo>(version, patchFile);
        }
        _logger.LogInformation("Downloading {0}", patchFile.Name);
        await using var fileStream = patchFile.OpenWrite();
        await _httpClient.DownloadAsync(patch.Url, fileStream, new Progress<float>(f =>
        {
            if (DownloadProgress.TryGetValue(guid, out var tuple))
            {
                var (i1, i2, _) = tuple;
                DownloadProgress[guid] = new Tuple<string, string, float>(i1, i2, f);
            }
        }));
        DownloadProgress.TryRemove(guid, out _);
        return new Tuple<ThaliakVersion, FileInfo>(version, patchFile);
    }

    public void InstallPatch(string patchPath, string gamePath)
    {
        _logger.LogInformation("Installing {0} to {1}", patchPath, gamePath);

        using var patchFile = ZiPatchFile.FromFileName(patchPath);

        using (var store = new SqexFileStreamStore())
        {
            var config = new ZiPatchConfig(gamePath) { Store = store };

            var chunks = patchFile.GetChunks().ToArray();

            for (var i = 0; i < chunks.Length; i++)
            {
                var t = chunks[i];
                var progress = new Progress<float>(pro => CurrentInstallProgress.chunkProgress = pro);
                t.ApplyChunk(config, progress);
                CurrentInstallProgress.fileProgress = (i + 1) / (float)chunks.Length;
            }
        }

        _logger.LogInformation("Patch {0} installed", patchPath);
        File.Delete(patchPath);
    }
}

public record PatchInstallerRepoOptions(string GamePath, string Slug)
{
    public string VerPath => GamePath.Contains('/') ? Path.Combine(GamePath, GamePath[(GamePath.LastIndexOf('/') + 1)..] + ".ver") : Path.Combine(GamePath, "ffxivgame.ver");
}

public record ThaliakVersionQueryResponse(ThaliakRepository Repository);

public record ThaliakRepository(string? Description, string? Slug, string? Name, ThaliakVersion? LatestVersion, ThaliakVersion[]? Versions);

public record ThaliakVersion(string VersionString, ThaliakVersion[]? PrerequisiteVersions, ThaliakPatches[]? Patches, bool? IsActive);

public record ThaliakPatchListQueryResponse(ThaliakRepository Repository);

public record ThaliakPatches(string Url);

public class FFXIVVersion
{
    public uint Year { get; private init; }
    public uint Month { get; private init; }
    public uint Day { get; private init; }
    public uint Part { get; private init; }
    public uint Revision { get; private set; }

    public static explicit operator FFXIVVersion(string versionString)
    {
        var parts = versionString.Split('.');

        if (parts[0][0] == 'H')
            parts[0] = parts[0][1..];

        char lastChar = parts[4][^1];
        if (!"0123456789".Contains(parts[4][^1]))
            parts[4] = parts[4][..^1];

        var ret = new FFXIVVersion
        {
            Year = uint.Parse(parts[0]),
            Month = uint.Parse(parts[1]),
            Day = uint.Parse(parts[2]),
            Part = uint.Parse(parts[3]),
            Revision = uint.Parse(parts[4])
        };

        if (lastChar != parts[4][^1])
        {
            ret.Revision += (uint)(lastChar - 'A' + 1);
        }

        return ret;
    }

    public static bool operator <(FFXIVVersion a, FFXIVVersion b)
    {
        if (a.Year < b.Year)
            return true;
        if (a.Year > b.Year)
            return false;
        if (a.Month < b.Month)
            return true;
        if (a.Month > b.Month)
            return false;
        if (a.Day < b.Day)
            return true;
        if (a.Day > b.Day)
            return false;
        if (a.Part < b.Part)
            return true;
        if (a.Part > b.Part)
            return false;
        if (a.Revision < b.Revision)
            return true;
        return a.Revision > b.Revision && false;
    }

    public static bool operator >(FFXIVVersion a, FFXIVVersion b)
    {
        return !(a < b);
    }

    public static bool operator <=(FFXIVVersion a, FFXIVVersion b)
    {
        return a < b || a == b;
    }

    public static bool operator >=(FFXIVVersion a, FFXIVVersion b)
    {
        return a > b || a == b;
    }

    public static bool operator ==(FFXIVVersion a, FFXIVVersion b)
    {
        return a.Year == b.Year && a.Month == b.Month && a.Day == b.Day && a.Part == b.Part && a.Revision == b.Revision;
    }

    public static bool operator !=(FFXIVVersion a, FFXIVVersion b)
    {
        return !(a == b);
    }

    public override bool Equals(object? obj)
    {
        return obj is FFXIVVersion version && this == version;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Year, Month, Day, Part, Revision);
    }
}

public static class FFXIVVersionExtensions
{
    public static T[] OrderByVersion<T>(this IEnumerable<T> source, Func<T, FFXIVVersion> keySelector)
    {
        var ret = source.ToArray();
        Array.Sort(ret, (a, b) =>
        {
            var aVersion = keySelector(a);
            var bVersion = keySelector(b);
            return aVersion < bVersion ? -1 : aVersion > bVersion ? 1 : 0;
        });
        return ret;
    }

    public static T[] OrderByVersionDesc<T>(this IEnumerable<T> source, Func<T, string> keySelector)
    {
        var ret = source.ToArray();
        Array.Sort(ret, (a, b) =>
        {
            var aVersion = (FFXIVVersion)keySelector(a);
            var bVersion = (FFXIVVersion)keySelector(b);
            return aVersion > bVersion ? -1 : aVersion < bVersion ? 1 : 0;
        });
        return ret;
    }
}

public static class HttpClientExtensions
{
    public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination, IProgress<float> progress = null!, CancellationToken cancellationToken = default)
    {
        // Get the http headers first to examine the content length
        using (var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead))
        {
            var contentLength = response.Content.Headers.ContentLength;

            using (var download = await response.Content.ReadAsStreamAsync(cancellationToken))
            {

                // Ignore progress reporting when no progress reporter was 
                // passed or when the content length is unknown
                if (progress == null || !contentLength.HasValue)
                {
                    await download.CopyToAsync(destination);
                    return;
                }

                // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
                var relativeProgress = new Progress<long>(totalBytes => progress.Report((float)totalBytes / contentLength.Value));
                // Use extension method to report progress while downloading
                await download.CopyToAsync(destination, 81920, relativeProgress, cancellationToken);
                progress.Report(1);
            }
        }
    }
}
public static class StreamExtensions
{
    public static async Task CopyToAsync(this Stream source, Stream destination, int bufferSize, IProgress<long> progress = null!, CancellationToken cancellationToken = default)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }

    public static void CopyTo(this Stream source, Stream destination, int bufferSize, IProgress<long> progress = null!)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        if (!source.CanRead)
            throw new ArgumentException("Has to be readable", nameof(source));
        if (destination == null)
            throw new ArgumentNullException(nameof(destination));
        if (!destination.CanWrite)
            throw new ArgumentException("Has to be writable", nameof(destination));
        if (bufferSize < 0)
            throw new ArgumentOutOfRangeException(nameof(bufferSize));
        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = source.Read(buffer, 0, buffer.Length)) != 0)
        {
            destination.Write(buffer, 0, bytesRead);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }
}