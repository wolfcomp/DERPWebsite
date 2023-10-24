using System.Text;
using Discord;
using Discord.WebSocket;
using FFXIVWeather.Lumina;
using Lumina.Excel.GeneratedSheets;

namespace PDPWebsite.Discord;

[SlashCommand("weather", "Collection of weather related commands for FFXIV")]
public class Weather : ISlashCommandProcessor
{
    private ILogger<Weather> _logger;
    private SocketSlashCommand _arg;
    private FFXIVWeatherLuminaService _weather;
    private GameClient _client;
    private const string TimeLeft = "In %d";

    public Weather(ILogger<Weather> logger, SocketSlashCommand arg, FFXIVWeatherLuminaService weather, GameClient client)
    {
        _logger = logger;
        _arg = arg;
        _weather = weather;
    }

    [SlashCommand("forecast", "Get the weather forecast for a given area.")]
    public async Task GetForecast([SlashCommand("area-name")] string area)
    {
        try
        {
            var areaData = _client.FindRowFromName<TerritoryType>(area, t => t.PlaceName.Value?.Name ?? "");

            _logger.LogTrace($"Finding first 10 weather patterns for area {areaData.Name}");
            var weathers = _weather.GetForecast(areaData, 10);

            if (weathers == null || !weathers.Any())
            {
                _logger.LogTrace($"Could not find weather for area {areaData.Name}");
                await _arg.ModifyOriginalResponseAsync(msg =>
                {
                    msg.Content = $"No weather forecast found for {areaData.PlaceName.Value!.Name}";
                });
                return;
            }

            _logger.LogTrace($"Found {weathers.Count} weather patterns for area {areaData.Name}");
            var embed = new EmbedBuilder().WithTitle($"Weather for {areaData.PlaceName.Value!.Name}");
            var currentWeather = weathers.First();
            var pendingWeather = weathers.Skip(1).ToList();
            var sb = new StringBuilder();
            sb.AppendLine($"Current weather: {currentWeather.Item1.Name}");
            var curTime = DateTime.UtcNow;
            foreach (var (weather, time) in pendingWeather)
            {
                sb.AppendLine($"{weather.Name}: {TimeLeft.Replace("%d", (curTime - time).ToHumanFormat())}");
            }

            embed.WithDescription(sb.ToString());

            await _arg.ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = null;
                msg.Embed = embed.Build();
            });
        }
        catch (MultipleFoundException ex)
        {
            await _arg.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = ex.BuildEmbed();
                msg.Content = null;
            });
        }
        catch (NoneFoundException ex)
        {
            await _arg.ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = ex.Message;
            });
        }
    }

    [SlashCommand("search", "Search for specific weather in area.")]
    public async Task FindWeather([SlashCommand("area")] string area, [SlashCommand("weather")] string weather)
    {
        var sb = new StringBuilder();
        try
        {
            var areaData = _client.FindRowFromName<TerritoryType>(area, t => t.PlaceName.Value?.Name ?? "");

            var weatherData = _client.FindRowFromName<Lumina.Excel.GeneratedSheets.Weather>(weather, t => t.Name);

            _logger.LogTrace($"Finding first 100 weather patterns for area {areaData.Name}");
            var weathers = _weather.GetForecast(areaData, 100);

            _logger.LogTrace($"Checking if weather {weatherData.Name} exists in list");
            while (weathers.Any(t => t.Item1.RowId != weatherData.RowId))
            {
                _logger.LogTrace($"Weather {weatherData.Name} not found in list, getting next 100");
                weathers = _weather.GetForecast(areaData, 100, initialOffset: (weathers[0].Item2 - weathers.Last().Item2).TotalSeconds);
            }

            _logger.LogTrace($"Found weather {weatherData.Name} in list");
            var (_, weatherTime) = weathers.First(t => t.Item1.RowId == weatherData.RowId);

            var now = DateTime.UtcNow;

            await _arg.ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = $"Next weather of {areaData.PlaceName.Value!.Name} is in {(weatherTime - now).ToHumanFormat()}";
            });
        }
        catch (MultipleFoundException ex)
        {
            await _arg.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = ex.BuildEmbed();
                msg.Content = null;
            });
        }
        catch (NoneFoundException ex)
        {
            await _arg.ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = ex.Message;
            });
        }
    }
}