using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Discord.SaveTheQueen;

[SlashCommand("bozja")]
public class Bozja : ISlashCommandProcessor
{
    private ILogger<Bozja> _logger;
    private SocketSlashCommand _arg;

    public Bozja(ILogger<Bozja> logger, SocketSlashCommand arg)
    {
        _logger = logger;
        _arg = arg;
    }
    
    [SlashCommand("map")]
    public async Task SouthernFrontMap()
    {
        var area = "";
        var embed = new EmbedBuilder();
        embed.WithTitle("Map of the Bozjan Southern Front");
        embed.WithDescription("Needed to find something in the Bozjan Southern Front? This can help.");
        // embed.WithImageUrl($"https://pdp.wildwolf.dev/files/04_eureka/02_maps/0{(int)area}_{Enum.GetName(area)!.ToLowerInvariant()}map.png");

        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = embed.Build();
        });
    }
}
