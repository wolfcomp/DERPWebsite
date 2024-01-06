using System.Globalization;
using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Discord;

[SlashCommand("quote", "collection of quote related commands")]
public class Quote : ISlashCommandProcessor
{
    private readonly Database _database;
    private readonly SocketSlashCommand _arg;

    public Quote(Database database, SocketSlashCommand arg)
    {
        _database = database;
        _arg = arg;
    }

    [SlashCommand("all")]
    public async Task RandomQuote()
    {
        var quotes = await _database.Quotes.ToListAsync();
        var weightedQuoteList = new WeightedList<Models.Quote>();
        quotes.ForEach(quote => weightedQuoteList.Add(quote, quote.Chance ?? 1));
        var quote = weightedQuoteList.Next();
        var embed = new EmbedBuilder()
            .WithAuthor(quote.Title)
            .WithDescription(quote.Text)
            .WithFooter($"Added by <@{quote.Creator}>")
            .WithTimestamp(quote.CreatedAt)
            // .WithColor()
            .Build();

    }
}

[SlashCommand("quote-admin", "collection of quote administration related commands", GuildPermission.ManageMessages)]
public class QuoteAdmin : ISlashCommandProcessor
{
    private readonly Database _database;
    private readonly SocketSlashCommand _arg;

    private const string ExpectedFormat =
        """
        ffffff
        #ffffff
        0xffffff
        rgb(255,255,255)
        rgba(255,255,255,1)
        """;

    public QuoteAdmin(Database database, SocketSlashCommand arg)
    {
        _database = database;
        _arg = arg;
    }

    [SlashCommand("add")]
    public async Task AddQuote([SlashCommand("text", "the text of the quote")] string text,
        [SlashCommand("title", "the title of the quote")] string title,
        [SlashCommand("user", "the user targeted for the quote")] SocketUser user,
        [SlashCommand("chance", "the chance of the quote being selected")] float? chance = null,
        [SlashCommand("color", "the color to have on the left side of the embed")] string? color = null)
    {
        var parsedColor = 0;
        if (color is not null)
        {
            if (color[0] == '#')
                color = color[1..];
            else if (color[..2] == "0x")
                color = color[2..];
            else if (color[0] == 'r')
            {
                var rgb = new byte[3];
                var a = 1f;
                if (color[3] == 'a')
                {
                    var arrStrings = color[5..^1].Split(',');
                    if (arrStrings.Length != 4)
                    {
                        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Invalid color format\nExpected:\n{ExpectedFormat}");
                        return;
                    }
                    for (var i = 0; i < 3; i++)
                        rgb[i] = byte.Parse(arrStrings[i]);
                }
                else
                {
                    var arrStrings = color[4..^1].Split(',');
                    if (arrStrings.Length != 3)
                    {
                        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Invalid color format\nExpected:\n{ExpectedFormat}");
                        return;
                    }
                    for (var i = 0; i < 3; i++)
                        rgb[i] = byte.Parse(arrStrings[i]);
                }
                parsedColor = new Color(rgb[0], rgb[1], rgb[2]).RawValue;
                goto colorParsed;
            }
            parsedColor = int.Parse(color, NumberStyles.HexNumber);
        }
    colorParsed:
        var quote = new Models.Quote(null, text, title, chance, _arg.User.Id, user.Id, DateTime.UtcNow, 0);
        await _database.Quotes.AddAsync(quote);
        await _database.SaveChangesAsync();
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Added quote with id {quote.Id}");
    }

    [SlashCommand("remove")]
    public async Task RemoveQuote([SlashCommand("id", "the id of the quote")] Guid id)
    {
        var quote = await _database.Quotes.FindAsync(id);
        if (quote is null)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find quote with id {id}");
            return;
        }
        _database.Quotes.Remove(quote);
        await _database.SaveChangesAsync();
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Removed quote with id {id}");
    }

    [SlashCommand("edit")]
    public async Task EditQuote([SlashCommand("id", "the id of the quote")] Guid id,
        [SlashCommand("text", "the text of the quote")] string? text,
        [SlashCommand("title", "the title of the quote")] string? title,
        [SlashCommand("user", "the user targeted for the quote")] SocketUser? user,
        [SlashCommand("chance", "the chance of the quote being selected")] float? chance = null)
    {
        var quote = await _database.Quotes.FindAsync(id);
        if (quote is null)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find quote with id {id}");
            return;
        }

        var query = "UPDATE Quotes SET ";
        var @params = new List<object>();
        if (text is not null)
        {
            query += $"Text = {@params.Count}";
            @params.Add(text);
        }

        if (title is not null)
        {
            query += $"Title = {@params.Count}";
            @params.Add(title);
        }

        if (chance is not null)
        {
            query += $"Chance = {@params.Count}";
            @params.Add(chance);
        }

        if (user is not null)
        {
            query += $"Target = {@params.Count}";
            @params.Add(user.Id);
        }

        if (@params.Count == 0)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"No changes were made to quote with id {id}");
            return;
        }

        query += $" WHERE Id = {@params.Count}";
        @params.Add(id);

        await _database.Database.ExecuteSqlRawAsync(query, @params.ToArray());
        await _database.SaveChangesAsync();
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Edited quote with id {id}");
    }

    [SlashCommand("list")]
    public async Task ListQuotes()
    {
        var quotes = await _database.Quotes.ToListAsync();
        var embed = new EmbedBuilder()
            .WithTitle("Quotes")
            .WithDescription(string.Join("\n", quotes.Select(quote => $"**{quote.Id}** - {quote.Title}")))
            .Build();
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Embed = embed;
            msg.Content = null;
        });
    }
}