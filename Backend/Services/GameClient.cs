using System.Collections.Immutable;
using System.Text;
using DERPWebsite.FFXIV;
using DERPWebsite.Patching;
using Discord;
using Discord.Rest;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel;

namespace DERPWebsite.Services;

public class GameClient : IDisposable
{
    private GameData _gameData;
    private readonly UniversalisClient _client;
    private readonly string _gameDataPath;
    private readonly ILogger<GameClient> _logger;
    private static Thread _updateThread = null!;
    private readonly PatchInstaller _patchInstaller;

    private List<Item> _marketItems = new();
    public IReadOnlyList<Item> MarketItems => _marketItems;

    public GameClient(UniversalisClient client, ILogger<GameClient> logger, IServiceProvider provider)
    {
#if DEBUG
        _gameDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SquareEnix", "FINAL FANTASY XIV - A Realm Reborn", "game", "sqpack");
#else
        _gameDataPath = Path.Combine(AppContext.BaseDirectory, "ffxiv", "sqpack");
#endif
        _patchInstaller = new PatchInstaller(provider.GetRequiredService<ILogger<PatchInstaller>>());
        _logger = logger;
        _client = client;

        if (!Directory.Exists(_gameDataPath))
        {
            _logger.LogWarning("Could not find game files downloading");

        }

        _gameData = new GameData(_gameDataPath)
        {
            Options =
            {
                PanicOnSheetChecksumMismatch = false
            }
        };
        LoadMarket().GetAwaiter().GetResult();
    }

    private async Task LoadMarket()
    {
        var ids = await _client.GetMarketItems();

        var items = _gameData.Excel.GetSheet<Lumina.Excel.GeneratedSheets.Item>(Language.English);
        if (items != null)
            foreach (var item in items)
            {
                if (ids.Contains(item.RowId))
                    _marketItems.Add(new Item(this)
                    {
                        Id = item.RowId,
                        Name = item.Name,
                        Singular = item.Singular,
                        Plural = item.Plural,
                        Icon = item.Icon
                    });
            }
    }

    public TexFile? GetTexFile(string path) => _gameData.GetFile<TexFile>(path);

    public ExcelSheet<T>? GetSheet<T>() where T : ExcelRow => _gameData.GetExcelSheet<T>();
    public ExcelSheet<T>? GetSheet<T>(Language language) where T : ExcelRow => _gameData.GetExcelSheet<T>(language);

    public Task RefreshGameData()
    {
        _gameData = new GameData(_gameDataPath)
        {
            Options =
            {
                PanicOnSheetChecksumMismatch = false
            }
        };

        return LoadMarket();
    }

    public async Task Update(RestInteractionMessage message)
    {
        if (_updateThread != null && _updateThread.IsAlive)
        {
            await message.ModifyAsync(t => t.Content = "Update already in progress.");
            return;
        }
        _updateThread = new Thread(async () =>
        {
            var builder = new EmbedBuilder().WithTitle("Update Status.")
                .WithDescription("Initializing");
            await message.ModifyAsync(t =>
            {
                t.Content = null;
                t.Embed = builder.Build();
            });
            var updateTask = UpdateInternal(async str =>
            {
                builder.WithDescription(str);
                await message.ModifyAsync(msg => { msg.Embed = builder.Build(); });
            }, (e, str) =>
            {
                if (e != null)
                    _logger.LogError(e, str);
                else
                    _logger.LogError(str);
            });

            if (updateTask.IsFaulted)
            {
                updateTask.Exception?.Flatten().Handle(exception =>
                {
                    _logger.LogError(exception, "Update failed.");
                    return true;
                });
                builder.WithDescription("Update failed.");
            }
            else
            {
                builder.WithDescription("Update Complete");
            }

            await RefreshGameData();

            await message.ModifyAsync(msg =>
            {
                msg.Embed = builder.Build();
            });
        });
        _updateThread.Start();
    }

    private async Task UpdateInternal(Action<string> pout, Action<Exception?, string> perr)
    {
        var updateTask = _patchInstaller.Update();
        while (!updateTask.IsCompleted)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            var sb = new StringBuilder();
            if (_patchInstaller.DownloadProgress.Any())
            {
                sb.AppendLine("Download progress:");
                foreach (var (_, download) in _patchInstaller.DownloadProgress.ToImmutableDictionary())
                {
                    var (desc, ver, progress) = download;
                    sb.AppendLine($"{desc} - {ver}\n{progress:P}");
                }
            }

            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine("Install progress:");
                var (desc, ver, goalVer, progress, chunkProgress, fileProgress) = _patchInstaller.CurrentInstallProgress;
                sb.AppendLine($"{desc} - {ver} {chunkProgress:P} {fileProgress:P} -> {goalVer}\n{progress:P}");
            }
            pout(sb.ToString());
        }
    }

    public Task<List<Tuple<string, string, string, string>>> CheckVersions() => _patchInstaller.CheckVersions();

    public void Dispose()
    {
        _logger.LogInformation($"Disposed");
    }
}
