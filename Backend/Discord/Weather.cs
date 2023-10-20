using Discord.WebSocket;

namespace PDPWebsite.Discord;

[SlashCommand("weather")]
public class Weather // Planning to expand this beyond Field Operations, but its limited to Eureka/Bozja for now.
{
    private ILogger<Weather> _logger;
    private SocketSlashCommand _arg;

    public Weather(ILogger<Weather> logger, SocketSlashCommand arg)
    {
        _logger = logger;
        _arg = arg;
    }
}
