using Discord.WebSocket;

namespace PDPWebsite.Discord.SaveTheQueen;

[SlashCommand("drn")]
public class DelubrumReginaeNormal : ISlashCommandProcessor
{
    private ILogger<DelubrumReginaeNormal> _logger;
    private SocketSlashCommand _arg;

    public DelubrumReginaeNormal(ILogger<DelubrumReginaeNormal> logger, SocketSlashCommand arg)
    {
        _logger = logger;
        _arg = arg;
    }
}

[SlashCommand("drs")]
public class DelubrumReginaeSavage
{
    private ILogger<DelubrumReginaeSavage> _logger;
    private SocketSlashCommand _arg;

    public DelubrumReginaeSavage(ILogger<DelubrumReginaeSavage> logger, SocketSlashCommand arg)
    {
        _logger = logger;
        _arg = arg;
    }
}
