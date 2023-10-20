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
}
