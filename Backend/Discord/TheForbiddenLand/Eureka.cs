using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Discord.TheForbiddenLand;

[SlashCommand("eureka")]
public class Eureka : ISlashCommandProcessor
{
    private ILogger<Eureka> _logger;
    private SocketSlashCommand _arg;

    public Eureka(ILogger<Eureka> logger, SocketSlashCommand arg)
    {
        _logger = logger;
        _arg = arg;
    }

    [SlashCommand("fairy", "Shows the recommended path for scouting fairies")]
    public async Task FairyMap([SlashCommand("area")] EurekaArea area)
    {
        var embed = new EmbedBuilder();
        embed.WithTitle("Eureka Fairy Locations");
        embed.WithDescription("This the recommended path when quickly scouting fairies for either BA, or otherwise.");
        embed.WithImageUrl($"https://pdp.wildwolf.dev/files/04_eureka/02_maps/0{(int)area}_{Enum.GetName(area)!.ToLowerInvariant()}elementals.png");
        
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = embed.Build();
        });
    }

    [SlashCommand("magia", "Gives you an idea of how the magia should be applied.")]
    public async Task Magia()
    {
        await _arg.RespondAsync("Not Implemented");
    }
}

public enum EurekaArea
{
    AMEMOS = 1,
    PAGOS,
    PYROS,
    HYDATOS,
}
