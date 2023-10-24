using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Discord.SaveTheQueen;

[SlashCommand("drn")]
public class DelubrumReginaeNormal : ISlashCommandProcessor/*<DelubrumReginaeNormal>*/
{
    private ILogger<DelubrumReginaeNormal> _logger;
    private SocketSlashCommand _arg;

    public DelubrumReginaeNormal(ILogger<DelubrumReginaeNormal> logger, SocketSlashCommand arg)
    {
        _logger = logger;
        _arg = arg;
    }

    [SlashCommand("loadouts")]
    public async Task Loadouts()
    {
        var embed = new EmbedBuilder();
        await _arg.RespondAsync(embed: embed.WithTitle("Optimized DRN Loadouts").WithImageUrl("set this to the loadout image").Build());
    }
}

[SlashCommand("drs")]
public class DelubrumReginaeSavage : ISlashCommandProcessor /*<DelubrumReginaeSavage>*/
{
    private ILogger<DelubrumReginaeSavage> _logger;
    private SocketSlashCommand _arg;

    public DelubrumReginaeSavage(ILogger<DelubrumReginaeSavage> logger, SocketSlashCommand arg)
    {
        _logger = logger;
        _arg = arg;
    }

    [SlashCommand("encounters")]
    public async Task Fights([SlashCommand("fight")] Fight fight)
    {
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "TODO: Implement ALL the fights :)");
    }
}

public enum Fight 
{
    SLIME = 0,
    GOLEM,
    TRINITY_SEEKER,
    DAHU,
    QUEENS_GUARD,
    BOZJAN_PHANTOM,
    TRINITY_AVOWED,
    QUEEN,
}
