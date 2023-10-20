using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.NodeServices;

namespace PDPWebsite.Discord.TheForbiddenLand;

[SlashCommand("eureka", "")]
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
    public async Task FairyMap()
    {
        await _arg.RespondAsync("Thinking...");

        var embed = new EmbedBuilder();
        embed.WithTitle("Eureka Fairy Locations");
        embed.WithDescription("This the recommended path when quickly scouting fairies for either BA, or otherwise.");
        embed.WithImageUrl("https://pdp.wildwolf.dev/Eureka/Maps/hydatos_elem.png");
        
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
