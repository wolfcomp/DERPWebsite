using System.Collections.Concurrent;
using System.Net.WebSockets;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using MSLogLevel = Microsoft.Extensions.Logging.LogLevel;
using NLogLevel = NLog.LogLevel;

namespace PDPWebsite.Services;

public partial class DiscordConnection : IDisposable
{
    public DiscordSocketClient DiscordClient { get; }
    public SocketGuild? Guild { get; private set; }
    public SocketTextChannel? LogChannel;
    /// <summary>
    /// Key: VoiceChannelId
    /// Value: UserId
    /// </summary>
    public ConcurrentDictionary<ulong, ulong> TempChannels;
    private readonly EnvironmentContainer _environmentContainer;
    private readonly IServiceProvider _provider;
    private readonly ILogger<DiscordConnection> _logger;
    private readonly RedisClient _redisClient;
    private readonly CancellationTokenSource _cts = new();
    private readonly GameClient _gameClient;
    private Type[] _slashCommandProcessors = Array.Empty<Type>();
    private NLogLevel _logLevel = NLogLevel.Warn;
    private SocketVoiceChannel _tempVoiceChannel = null!;
    private List<Game> Games { get; } = new()
    {
        new("Universalis", ActivityType.Watching),
        new("with the market"),
        new("with the economy"),
        new("the schedule", ActivityType.Watching)
    };

    public static Action? OnReady { get; set; }
    public static DiscordConnection? Instance { get; set; }

    public DiscordConnection(ILogger<DiscordConnection> logger, EnvironmentContainer environmentContainer, RedisClient redisClient, GameClient gameClient, IServiceProvider provider)
    {
        _logger = logger;
        _environmentContainer = environmentContainer;
        _provider = provider;
        _redisClient = redisClient;
        _gameClient = gameClient;
        DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.All,
            SuppressUnknownDispatchWarnings = true,
            AlwaysDownloadUsers = true,
            ConnectionTimeout = 10000,
            DefaultRetryMode = RetryMode.AlwaysRetry
        });
        DiscordClient.Log += Log;
        DiscordClient.Ready += Ready;
        if (!bool.Parse(_environmentContainer.Get("DISCORD_DISABLE")))
        {
            DiscordClient.SlashCommandExecuted += SlashCommandExecuted;
            DiscordClient.UserVoiceStateUpdated += UserVoiceStateUpdated;
            DiscordClient.ButtonExecuted += MessageInteractionExecuted;
            DiscordClient.SelectMenuExecuted += MessageInteractionExecuted;
            DiscordClient.ModalSubmitted += DiscordClientOnModalSubmitted;
        }
        DiscordClient.PresenceUpdated += (_, _, _) => Task.CompletedTask;
        DiscordClient.GuildScheduledEventStarted += _ => Task.CompletedTask;
        DiscordClient.InviteCreated += _ => Task.CompletedTask;
        TempChannels = _redisClient.GetObj<ConcurrentDictionary<ulong, ulong>>("discord_temp_channels") ?? new ConcurrentDictionary<ulong, ulong>();
        Instance = this;
    }

    public async Task Start()
    {
        await DiscordClient.LoginAsync(TokenType.Bot, _environmentContainer.Get("DISCORD_TOKEN"));
        await DiscordClient.StartAsync();
    }

    public Task Log(LogMessage arg)
    {
        var log = arg;
        if (log.Exception is WebSocketException or WebSocketClosedException or GatewayReconnectException || log.Exception?.InnerException is WebSocketException or WebSocketClosedException or GatewayReconnectException || string.IsNullOrWhiteSpace(log.Message))
            return Task.CompletedTask;

        if (log.Severity == LogSeverity.Warning)
        {
            var args = log.Message.Split(' ');
            if (args[0] == "Unknown")
            {
                switch (args[1])
                {
                    case "Channel":
                        return Task.CompletedTask;
                }
            }
        }

        _logger.Log(log.Severity switch
        {
            LogSeverity.Critical => MSLogLevel.Critical,
            LogSeverity.Error => MSLogLevel.Error,
            LogSeverity.Warning => MSLogLevel.Warning,
            LogSeverity.Info => MSLogLevel.Information,
            LogSeverity.Verbose => MSLogLevel.Trace,
            LogSeverity.Debug => MSLogLevel.Debug,
            _ => MSLogLevel.Information
        }, log.Exception, log.Message);
        return Task.CompletedTask;
    }

    public async Task SetActivity()
    {
#pragma warning disable CS4014
        try
        {
            if (DiscordClient.ConnectionState != ConnectionState.Connected)
            {
                if (_cts.IsCancellationRequested)
                    return;
                await Task.Delay(1000, _cts.Token);
                SetActivity();
            }

            var next = Games[Random.Shared.Next(Games.Count)];
            await DiscordClient.SetActivityAsync(next);
            await Task.Delay(TimeSpan.FromMinutes(30), _cts.Token);
            SetActivity();
        }
        catch (TaskCanceledException)
        {
        }
#pragma warning restore CS4014
    }

    private async Task Ready()
    {
        LogChannel = (SocketTextChannel)await DiscordClient.GetChannelAsync(ulong.Parse(_environmentContainer.Get("DISCORD_LOG_CHANNEL")));
        Guild = DiscordClient.GetGuild(ulong.Parse(_environmentContainer.Get("DISCORD_GUILD")));
        _tempVoiceChannel = (SocketVoiceChannel)await DiscordClient.GetChannelAsync(ulong.Parse(_environmentContainer.Get("DISCORD_TEMP_VOICE")));
        if (!bool.Parse(_environmentContainer.Get("DISCORD_DISABLE")))
        {
#pragma warning disable CS4014
            SetActivity();
            CreateCommands();
            CheckVoice();
#pragma warning restore CS4014
        }
        OnReady?.Invoke();
    }

    public async Task DisposeAsync()
    {
        await DiscordClient.StopAsync();
        await DiscordClient.LogoutAsync();
        await DiscordClient.DisposeAsync();
        _redisClient.SetObj("discord_temp_channels", TempChannels);
        _cts.Cancel();
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    public bool ShouldLog(NLogLevel logEventLevel)
    {
        return logEventLevel < _logLevel;
    }

    public void SetLogLevel(MSLogLevel level)
    {
        _logLevel = level switch
        {
            MSLogLevel.Critical => _logLevel = NLogLevel.Fatal,
            MSLogLevel.Error => _logLevel = NLogLevel.Error,
            MSLogLevel.Warning => _logLevel = NLogLevel.Warn,
            MSLogLevel.Information => _logLevel = NLogLevel.Info,
            MSLogLevel.Debug => _logLevel = NLogLevel.Debug,
            MSLogLevel.Trace => _logLevel = NLogLevel.Trace,
            _ => _logLevel = NLogLevel.Info
        };
    }

    public NLogLevel GetLogLevel() => _logLevel;
}