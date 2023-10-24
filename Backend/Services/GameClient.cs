using System.Text;
using Discord;
using Lumina;
using Lumina.Data;
using Lumina.Data.Files;
using Lumina.Excel;
using PDPWebsite.FFXIV;

namespace PDPWebsite.Services;

public class GameClient : IDisposable
{
    private readonly GameData _gameData;
    private readonly UniversalisClient _client;
    private readonly ILogger<GameClient> _logger;

    private List<Item> _marketItems = new();
    public IReadOnlyList<Item> MarketItems => _marketItems;

    public GameClient(UniversalisClient client, ILogger<GameClient> logger)
    {
#if DEBUG
        var gameDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "SquareEnix", "FINAL FANTASY XIV - A Realm Reborn", "game", "sqpack");
#else
        var gameDataPath = Path.Combine(AppContext.BaseDirectory, "ffxiv", "sqpack");
#endif
        _gameData = new GameData(gameDataPath)
        {
            Options =
            {
                PanicOnSheetChecksumMismatch = false,
                LoadMultithreaded = true,
                CacheFileResources = false
            }
        };
        _client = client;
        LoadMarket().GetAwaiter().GetResult();
        _logger = logger;
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

    /// <summary>
    /// Finds the row with closest matching name
    /// </summary>
    /// <typeparam name="T">The sheet to use for lookup</typeparam>
    /// <param name="name">The value that is used for matching</param>
    /// <param name="nameFunc">Where in the sheet should it try to be matched</param>
    /// <returns>Returns the row of matching name</returns>
    /// <exception cref="NoneFoundException">Thrown if no row can be found</exception>
    /// <exception cref="MultipleFoundException">Thrown if multiple rows can be found</exception>
    public T FindRowFromName<T>(string name, Func<T, string> nameFunc) where T : ExcelRow
    {
        _logger.LogInformation($"Finding {name} from sheet {typeof(T)}");
        var items = _gameData.GetExcelSheet<T>()!.Where(t => nameFunc(t).Contains(name, StringComparison.InvariantCultureIgnoreCase)).ToList();

        if (!items.Any())
        {
            _logger.LogTrace($"Could not find any with the name {name} for type {typeof(T).Name}");
            throw new NoneFoundException($"Could not find any with the name {name} for type {typeof(T).Name}");
        }

        if (items.Count > 1)
        {
            _logger.LogTrace($"Multiple found for {name} with type {typeof(T).Name}");
            throw new MultipleFoundException<T>(items, nameFunc, $"Multiple found for {name} with type {typeof(T).Name}");
        }

        _logger.LogTrace($"Found {name} with type {typeof(T).Name}");
        return items.First();
    }

    public GameData Game => _gameData;

    public void Dispose()
    {
    }
}

public class MultipleFoundException<T> : MultipleFoundException
{
    private List<T> _items;
    private Func<T, string> _nameFunc;
    private const string CountLeft = "With %d more.";

    public MultipleFoundException(List<T> items, Func<T, string> nameFunc, string message) : base(message)
    {
        _items = items;
    }

    public override Embed BuildEmbed()
    {
        var embed = new EmbedBuilder().WithTitle(Message);
        var sb = new StringBuilder();
        for (var i = 0; i < _items.Count; i++)
        {
            var countLeft = CountLeft.Replace("%d", $"{_items.Count - i}");
            var str = $"`{i + 1}`: {_nameFunc(_items[i])}";
            if (sb.Length + str.Length < 4096 - countLeft.Length)
                sb.AppendLine(str);
            else
            {
                sb.AppendLine(countLeft);
                break;
            }
        }
        return embed.Build();
    }
}

public abstract class MultipleFoundException : Exception
{
    protected MultipleFoundException(string message) : base(message) { }

    public abstract Embed BuildEmbed();
}

public class NoneFoundException : Exception
{
    public NoneFoundException(string message) : base(message)
    {
    }
}