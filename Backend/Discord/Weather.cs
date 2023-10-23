using Discord.WebSocket;
using FFXIVWeather.Lumina;

namespace PDPWebsite.Discord;

[SlashCommand("weather")]
public class Weather // Planning to expand this beyond Field Operations, but its limited to Eureka/Bozja for now.
{
    private ILogger<Weather> _logger;
    private SocketSlashCommand _arg;
    private FFXIVWeatherLuminaService _weather;
    
    

    public Weather(ILogger<Weather> logger, SocketSlashCommand arg, FFXIVWeatherLuminaService weather)
    {
        _logger = logger;
        _arg = arg;
        _weather = weather;
    }

    public async Task GetForecast()
    {
        await _arg.RespondAsync("Thinking...");

        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = "Not implemented.";
        });
    }
}
