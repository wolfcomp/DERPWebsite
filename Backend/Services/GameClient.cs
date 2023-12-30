using System.Text;
using Discord;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel;
using PDPWebsite.FFXIV;
using PDPWebsite.Patching;

namespace PDPWebsite.Services;

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
        _gameData = new GameData(_gameDataPath)
        {
            Options =
            {
                PanicOnSheetChecksumMismatch = false
            }
        };
        _logger = logger;
        _client = client;
        _patchInstaller = new PatchInstaller(provider.GetRequiredService<ILogger<PatchInstaller>>());
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

    public async Task Update(IMessageChannel channel)
    {
        if (_updateThread != null && _updateThread.IsAlive)
        {
            await channel.SendMessageAsync("Update already in progress.");
            return;
        }
        _updateThread = new Thread(async () =>
        {
            var builder = new EmbedBuilder().WithTitle("Update Status.")
                .WithDescription("Initializing");
            var message = await channel.SendMessageAsync(embed: builder.Build());
            var updateTask = _patchInstaller.Update();
            while (!updateTask.IsCompleted)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                if (_patchInstaller.DownloadProgress.Any())
                {
                    var sb = new StringBuilder();
                    foreach (var (_, download) in _patchInstaller.DownloadProgress)
                    {
                        var (desc, ver, progress) = download;
                        sb.AppendLine($"{desc} - {ver}\n{progress:P}");
                    }
                    builder.WithDescription(sb.ToString());
                }
                else
                {
                    var (desc, ver, goalVer, progress) = _patchInstaller.CurrentInstallProgress;
                    builder.WithDescription($"{desc} - {ver} -> {goalVer}\n{progress:P}");
                }
                await message.ModifyAsync(msg => { msg.Embed = builder.Build(); });
            }

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

    public Task<List<Tuple<string, string, string, string>>> CheckVersions() => _patchInstaller.CheckVersions();

    public void Dispose()
    {
        _logger.LogInformation($"Disposed");
    }
}
