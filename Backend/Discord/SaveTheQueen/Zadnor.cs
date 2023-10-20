using Discord.WebSocket;

namespace PDPWebsite.Discord.SaveTheQueen;

[SlashCommand("zadnor")]
public class Zadnor : ISlashCommandProcessor
{
    private ILogger<Zadnor> _logger;
    private SocketSlashCommand _arg;

    public Zadnor(ILogger<Zadnor> logger, SocketSlashCommand arg)
    {
        _logger = logger;
        _arg = arg;
    }
}
