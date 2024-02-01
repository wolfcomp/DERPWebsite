using System.Globalization;
using Discord;
using Discord.WebSocket;

namespace PDPWebsite.Discord;

[SlashCommand("quote", "collection of quote related commands")]
public class Quote : ISlashCommandProcessor
{
    private readonly Database _database;
    private readonly SocketSlashCommand _arg;
    private readonly DiscordConnection _discord;
    private readonly ILogger<Quote> _logger;

    public Quote(Database database, SocketSlashCommand arg, DiscordConnection discord, ILogger<Quote> logger)
    {
        _database = database;
        _arg = arg;
        _discord = discord;
        _logger = logger;
    }

    [SlashCommand("all", "gets a random quote from the entire list")]
    public async Task RandomQuote()
    {
        var quotes = await _database.Quotes.ToListAsync();
        if (quotes.Count == 0)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Could not find any quotes");
            return;
        }
        var weightedQuoteList = new WeightedList<Models.Quote>();
        quotes.ForEach(quote => weightedQuoteList.Add(quote, quote.Chance ?? 1));
        var quote = weightedQuoteList.Next();
        var creator = _discord.Guild?.GetUser(quote.Creator);
        SocketGuildUser? guildCreator = null;
        if (creator is SocketGuildUser socketGuildCreator)
            guildCreator = socketGuildCreator;
        var author = _discord.Guild?.GetUser(quote.Target);
        Embed embed;
        try
        {
            var url = new Uri(quote.Text);
            embed = new EmbedBuilder()
                .WithAuthor(quote.Title)
                .WithFooter($"Added by {guildCreator?.DisplayName ?? ""}, Quote by {author?.DisplayName ?? ""}")
                .WithTimestamp(quote.CreatedAt)
                .WithColor(quote.Color)
                .WithImageUrl(url.ToString())
                .Build();
        }
        catch
        {
            embed = new EmbedBuilder()
                .WithAuthor(quote.Title)
                .WithDescription(quote.Text)
                .WithFooter($"Added by {guildCreator?.DisplayName ?? ""}, Quote by {author?.DisplayName ?? ""}")
                .WithTimestamp(quote.CreatedAt)
                .WithColor(quote.Color)
                .Build();
        }

        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = embed;
        });
    }

    [SlashCommand("user", "gets a random quote for a user")]
    public async Task RandomQuote([SlashCommand("user", "the user for a targeted quote")] SocketUser user)
    {
        var quotes = await _database.Quotes.Where(quote => quote.Target == user.Id).ToListAsync();
        if (quotes.Count == 0)
        {
            SocketGuildUser? guildUser = null;
            if (user is SocketGuildUser socketGuildUser)
                guildUser = socketGuildUser;
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find any quotes for user {guildUser?.DisplayName ?? user.GlobalName}");
            return;
        }
        var weightedQuoteList = new WeightedList<Models.Quote>();
        quotes.ForEach(quote => weightedQuoteList.Add(quote, quote.Chance ?? 1));
        var quote = weightedQuoteList.Next();
        var creator = _discord.Guild?.GetUser(quote.Creator);
        SocketGuildUser? guildCreator = null;
        if (creator is SocketGuildUser socketGuildCreator)
            guildCreator = socketGuildCreator;
        var author = _discord.Guild?.GetUser(quote.Target);
        Embed embed;
        try
        {
            var url = new Uri(quote.Text);
            embed = new EmbedBuilder()
                .WithAuthor(quote.Title)
                .WithFooter($"Added by {guildCreator?.DisplayName ?? ""}, Quote by {author?.DisplayName ?? ""}")
                .WithTimestamp(quote.CreatedAt)
                .WithColor(quote.Color)
                .WithImageUrl(url.ToString())
                .Build();
        }
        catch (UriFormatException)
        {
            try
            {
                embed = new EmbedBuilder()
                    .WithAuthor(quote.Title)
                    .WithDescription(quote.Text)
                    .WithFooter($"Added by {guildCreator?.DisplayName ?? ""}, Quote by {author?.DisplayName ?? ""}")
                    .WithTimestamp(quote.CreatedAt)
                    .WithColor(quote.Color)
                    .Build();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error building quote embed");
                await _arg.ModifyOriginalResponseAsync(msg => msg.Content = "Error building quote embed");
                return;
            }
        }
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = embed;
        });
    }
}

[SlashCommand("quote-admin", "collection of quote administration related commands", GuildPermission.ManageMessages), AllowedChannel(1072337111564419132)]
public class QuoteAdmin : ISlashCommandProcessor
{
    private readonly Database _database;
    private readonly SocketSlashCommand _arg;
    private readonly DiscordConnection _discord;

    private const string ExpectedFormat =
        """
        ffffff
        #ffffff
        0xffffff
        rgb(255,255,255)
        rgba(255,255,255,1)
        """;

    public QuoteAdmin(Database database, SocketSlashCommand arg, DiscordConnection discord)
    {
        _database = database;
        _arg = arg;
        _discord = discord;
    }

    [SlashCommand("add", "adds a quote")]
    public async Task AddQuote([SlashCommand("text", "the text of the quote")] string text,
        [SlashCommand("title", "the title of the quote")] string title,
        [SlashCommand("user", "the user targeted for the quote")] SocketUser user,
        [SlashCommand("chance", "the chance of the quote being selected, default is 10")] uint? chance = null,
        [SlashCommand("color", "the color to have on the left side of the embed, default is teal")] string? color = null)
    {
        var parsedColor = Color.Teal.RawValue;
        if (color is not null)
        {
            var rgb = new byte[3];
            if (color[0] == '#')
                color = color[1..];
            else if (color[..2] == "0x")
                color = color[2..];
            else if (color[0] == 'r')
            {
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

            if (color.Length != 6)
            {
                await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Invalid color format\nExpected:\n{ExpectedFormat}");
                return;
            }
            for (var i = 0; i < rgb.Length; i++)
            {
                rgb[i] = byte.Parse(color[(i * 2)..(i * 2 + 2)], NumberStyles.HexNumber);
            }
            parsedColor = new Color(rgb[0], rgb[1], rgb[2]).RawValue;
        }
    colorParsed:
        var quote = new Models.Quote(null, text, title, chance, _arg.User.Id, user.Id, DateTime.UtcNow, parsedColor);
        await _database.Quotes.AddAsync(quote);
        await _database.SaveChangesAsync();
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Added quote with id {quote.Id}");
    }

    [SlashCommand("remove", "removes a quote")]
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

    [SlashCommand("edit", "edits a quote")]
    public async Task EditQuote([SlashCommand("id", "the id of the quote")] Guid id,
        [SlashCommand("text", "the text of the quote")] string? text = null,
        [SlashCommand("title", "the title of the quote")] string? title = null,
        [SlashCommand("user", "the user targeted for the quote")] SocketUser? user = null,
        [SlashCommand("chance", "the chance of the quote being selected")] uint? chance = null,
        [SlashCommand("color", "the color to have on the left side of the embed")] string? color = null)
    {
        var quote = _database.Quotes.FirstOrDefault(t => t.Id == id);
        if (quote is null)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find quote with id {id}");
            return;
        }

        var query = @"UPDATE ""Quotes"" SET ";
        var @params = new List<object>();
        if (text is not null)
        {
            query += $@"""Text"" = {{{@params.Count}}},";
            @params.Add(text);
        }

        if (title is not null)
        {
            query += $@"""Title"" = {{{@params.Count}}},";
            @params.Add(title);
        }

        if (chance is not null)
        {
            query += $@"""Chance"" = {{{@params.Count}}},";
            @params.Add(chance);
        }

        if (user is not null)
        {
            query += $@"""Target"" = {{{@params.Count}}},";
            @params.Add(user.Id);
        }

        if (color is not null)
        {
            uint parsedColor;
            var rgb = new byte[3];
            if (color[0] == '#')
                color = color[1..];
            else if (color[..2] == "0x")
                color = color[2..];
            else if (color[0] == 'r')
            {
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

            if (color.Length != 6)
            {
                await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Invalid color format\nExpected:\n{ExpectedFormat}");
                return;
            }
            for (var i = 0; i < rgb.Length; i++)
            {
                rgb[i] = byte.Parse(color[(i * 2)..(i * 2 + 2)], NumberStyles.HexNumber);
            }
            parsedColor = new Color(rgb[0], rgb[1], rgb[2]).RawValue;
        colorParsed:
            query += $@"""Color"" = {{{@params.Count}}},";
            @params.Add(parsedColor);
        }

        if (@params.Count == 0)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"No changes were made to quote with id {id}");
            return;
        }

        query = $$"""{{query[..^1]}} WHERE "Id" = {{{@params.Count}}}""";
        @params.Add(id);

        await _database.Database.ExecuteSqlRawAsync(query, @params.ToArray());
        await _database.SaveChangesAsync();
        _database.ChangeTracker.Clear();
        await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Edited quote with id {id}");
    }

    [SlashCommand("list", "lists all quotes")]
    public async Task ListQuotes()
    {
        var quotes = (await _database.Quotes.ToListAsync()).Select(quote => $"**{quote.Id}** - {quote.Title} | Targets: <@{quote.Id}> Wight: {quote.Chance ?? 1}\n").ToArray();
        var embedStrings = new List<string>();
        var quoteIndex = 0;
        var embedStringIndex = 0;
        while (quoteIndex < quotes.Length)
        {
            if (embedStrings.Count == embedStringIndex)
                embedStrings.Add("");
            if (embedStrings[embedStringIndex].Length + quotes[quoteIndex].Length > EmbedBuilder.MaxDescriptionLength)
            {
                embedStringIndex++;
                continue;
            }
            embedStrings[embedStringIndex] += quotes[quoteIndex];
            quoteIndex++;
        }

        var embeds = embedStrings.Select((embedString, i) => new EmbedBuilder().WithTitle(i == 0 ? "Quotes" : "Continuation")
                .WithDescription(embedString[..^1])
                .Build())
            .ToList();
        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Embeds = embeds.ToArray();
            msg.Content = null;
        });
    }

    [SlashCommand("get", "gets a specific quote")]
    public async Task GetQuote([SlashCommand("id", "the id of the quote")] Guid id)
    {
        var quote = _database.Quotes.FirstOrDefault(t => t.Id == id);
        if (quote is null)
        {
            await _arg.ModifyOriginalResponseAsync(msg => msg.Content = $"Could not find quote with id {id}");
            return;
        }
        var creator = _discord.Guild?.GetUser(quote.Creator);
        SocketGuildUser? guildCreator = null;
        if (creator is SocketGuildUser socketGuildCreator)
            guildCreator = socketGuildCreator;
        var author = _discord.Guild?.GetUser(quote.Target);
        Embed embed;
        try
        {
            var url = new Uri(quote.Text);
            embed = new EmbedBuilder()
                .WithAuthor(quote.Title)
                .WithFooter($"Added by {guildCreator?.DisplayName ?? ""}, Quote by {author?.DisplayName ?? ""}")
                .WithTimestamp(quote.CreatedAt)
                .WithColor(quote.Color)
                .WithImageUrl(url.ToString())
                .Build();
        }
        catch
        {
            embed = new EmbedBuilder()
                .WithAuthor(quote.Title)
                .WithDescription(quote.Text)
                .WithFooter($"Added by {guildCreator?.DisplayName ?? ""}, Quote by {author?.DisplayName ?? ""}")
                .WithTimestamp(quote.CreatedAt)
                .WithColor(quote.Color)
                .Build();
        }

        await _arg.ModifyOriginalResponseAsync(msg =>
        {
            msg.Content = null;
            msg.Embed = embed;
        });
    }
}