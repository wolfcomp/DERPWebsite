using System.Reflection;
using DERPWebsite.Discord;
using Discord;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Newtonsoft.Json;

namespace DERPWebsite.Services;

public partial class DiscordConnection
{
    private async Task CreateCommands()
    {
        var commands = new Dictionary<ulong, IReadOnlyCollection<RestGuildCommand>>();
        try
        {
            foreach (var discordClientGuild in DiscordClient.Guilds)
            {
                commands.Add(discordClientGuild.Id, await DiscordClient.Rest.GetGuildApplicationCommands(discordClientGuild.Id));
            }
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            _logger.LogError(exception, json);
        }

        _slashCommandProcessors = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetInterfaces().Any(x => x == typeof(ISlashCommandProcessor)) && t.IsClass).ToArray();

        var commandBuilders = new List<SlashCommandBuilder>();

        foreach (var slashCommandProcessor in _slashCommandProcessors)
        {
            var slashCommandBuilder = new SlashCommandBuilder();
            slashCommandBuilder.WithName(slashCommandProcessor.Name.SanitizeName());
            var slashCommand = slashCommandProcessor.GetCustomAttribute<SlashCommandAttribute>();
            slashCommand?.SetBuilder(slashCommandBuilder);
            var methods = slashCommandProcessor.GetMethods();
            foreach (var methodInfo in methods)
            {
                if (methodInfo.ReturnType != typeof(Task))
                    continue;
                var slashCommandAttribute = methodInfo.GetCustomAttribute<SlashCommandAttribute>();
                var slashCommandOptionBuilder = new SlashCommandOptionBuilder();
                slashCommandOptionBuilder.WithType(ApplicationCommandOptionType.SubCommand);
                slashCommandOptionBuilder.WithName(methodInfo.Name.SanitizeName());
                slashCommandAttribute?.SetBuilder(slashCommandOptionBuilder);
                var parameters = methodInfo.GetParameters();
                foreach (var parameterInfo in parameters)
                {
                    var slashCommandParamBuilder = new SlashCommandOptionBuilder();
                    var slashCommandParamAttribute = parameterInfo.GetCustomAttribute<SlashCommandAttribute>();
                    slashCommandParamBuilder.WithName(parameterInfo.Name!.SanitizeName());
                    slashCommandParamAttribute?.SetBuilder(slashCommandParamBuilder);
                    var required = Nullable.GetUnderlyingType(parameterInfo.ParameterType) == null && !parameterInfo.Attributes.HasFlag(ParameterAttributes.Optional);
                    var paramType = Nullable.GetUnderlyingType(parameterInfo.ParameterType) ?? parameterInfo.ParameterType;
                    var slashCommandOptionType = paramType switch
                    {
                        { IsEnum: true } => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(string) => ApplicationCommandOptionType.String,
                        { } t when t == typeof(bool) => ApplicationCommandOptionType.Boolean,
                        { } t when t == typeof(int) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(ulong) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(long) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(uint) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(short) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(ushort) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(byte) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(sbyte) => ApplicationCommandOptionType.Integer,
                        { } t when t == typeof(double) => ApplicationCommandOptionType.Number,
                        { } t when t == typeof(float) => ApplicationCommandOptionType.Number,
                        { } t when t == typeof(decimal) => ApplicationCommandOptionType.Number,
                        { } t when t == typeof(DateTime) => ApplicationCommandOptionType.String,
                        { } t when t == typeof(DateTimeOffset) => ApplicationCommandOptionType.String,
                        { } t when t == typeof(TimeSpan) => ApplicationCommandOptionType.String,
                        { } t when t == typeof(SocketRole) => ApplicationCommandOptionType.Role,
                        { } t when t == typeof(SocketUser) => ApplicationCommandOptionType.User,
                        { } t when t == typeof(SocketChannel) => ApplicationCommandOptionType.Channel,
                        { } t when t == typeof(Attachment) => ApplicationCommandOptionType.Attachment,
                        { } t when t == typeof(Guid) => ApplicationCommandOptionType.String,
                        _ => throw new ArgumentOutOfRangeException(nameof(paramType), paramType, $"Could not match type with {paramType.Name}")
                    };
                    if (paramType.IsEnum)
                    {
                        //check if enum has more than 25 values
                        var values = Enum.GetValues(paramType);
                        if (values.Length > 25)
                        {
                            _logger.LogError($"Enum {paramType.Name} has more than 25 values, this is not supported by discord.");
                            goto enumEscape;
                        }
                        foreach (var value in values)
                        {
                            slashCommandParamBuilder.AddChoice(value.ToString()!, (int)value);
                        }
                    }
                    slashCommandParamBuilder.WithType(slashCommandOptionType);
                    slashCommandParamBuilder.WithRequired(required);
                    slashCommandOptionBuilder.AddOption(slashCommandParamBuilder);
                }
                slashCommandBuilder.AddOption(slashCommandOptionBuilder);
            }
            commandBuilders.Add(slashCommandBuilder);
        enumEscape:;
        }

        try
        {
            foreach (var discordClientGuild in DiscordClient.Guilds)
            {
                var guildCommands = commands[discordClientGuild.Id].ToList();
                foreach (var commandBuilder in commandBuilders)
                {
                    var command = commandBuilder.Build();
                    if (guildCommands.Any(t => t.Name == command.Name.Value))
                    {
                        guildCommands.Remove(guildCommands.First(t => t.Name == command.Name.Value));
                    }
                    await DiscordClient.Rest.CreateGuildCommand(commandBuilder.Build(), discordClientGuild.Id);
                }

                foreach (var guildCommand in guildCommands)
                {
                    await guildCommand.DeleteAsync();
                }
            }
        }
        catch (HttpException exception)
        {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            _logger.LogError(exception, json);
        }
    }

    private async Task SlashCommandExecuted(SocketSlashCommand arg)
    {
        var command = arg.Data.Name;
        var subCommand = arg.Data.Options.FirstOrDefault()?.Name;
        var type = _slashCommandProcessors.FirstOrDefault(t => t.IsSameCommand(command));
        if (type == null)
        {
            await arg.RespondAsync($"No command found for {command}... good job you found a bug in discord.", ephemeral: true);
            return;
        }
        var channels = type.GetCustomAttributes<AllowedChannelAttribute>().Select(t => t.ChannelId).ToArray();
        if (channels.Any() && !channels.Contains(arg.Channel.Id) && arg.ChannelId != ulong.Parse(_environmentContainer.Get("DISCORD_ADMIN_CHANNEL")))
        {
            await arg.RespondAsync($"This command can only be used in the following channels: {string.Join(", ", channels.Select(t => $"<#{t}>"))}", ephemeral: true);
            return;
        }

        object instance;
        try
        {
            instance = ActivatorUtilities.CreateInstance(_provider, type, arg, _gameClient);
        }
        catch
        {
            instance = ActivatorUtilities.CreateInstance(_provider, type, arg);
        }
        var method = type.GetMethods().FirstOrDefault(t => t.IsSameCommand(subCommand!));
        if (method == null)
        {
            await arg.RespondAsync($"No sub command found for {subCommand}... good job you found a bug in discord.", ephemeral: true);
            return;
        }
        var responseType = method.GetCustomAttribute<ResponseTypeAttribute>() ?? new ResponseTypeAttribute();
        var args = new List<object>();
        try
        {
            await arg.RespondAsync("Thinking...", isTTS: responseType.IsTts, ephemeral: responseType.IsEphemeral);
            var parameters = method.GetParameters();
            var paramsOptions = arg.Data.Options.First().Options;
            foreach (var parameter in parameters)
            {
                foreach (var paramOption in paramsOptions)
                {
                    if (parameter.IsSameCommand(paramOption.Name))
                    {
                        var typeSafe = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                        args.Add((typeSafe switch
                        {
                            { IsEnum: true } => Enum.GetValues(typeSafe).GetValue(Convert.ToInt32(paramOption.Value)),
                            _ when typeSafe == typeof(string) => paramOption.Value,
                            _ when typeSafe == typeof(bool) => paramOption.Value,
                            _ when typeSafe == typeof(int) => Convert.ToInt32(paramOption.Value),
                            _ when typeSafe == typeof(ulong) => Convert.ToUInt64(paramOption.Value),
                            _ when typeSafe == typeof(long) => paramOption.Value,
                            _ when typeSafe == typeof(uint) => Convert.ToUInt32(paramOption.Value),
                            _ when typeSafe == typeof(short) => Convert.ToInt16(paramOption.Value),
                            _ when typeSafe == typeof(ushort) => Convert.ToUInt16(paramOption.Value),
                            _ when typeSafe == typeof(byte) => Convert.ToByte(paramOption.Value),
                            _ when typeSafe == typeof(sbyte) => Convert.ToSByte(paramOption.Value),
                            _ when typeSafe == typeof(double) => double.Parse(paramOption.Value.ToString()!),
                            _ when typeSafe == typeof(float) => float.Parse(paramOption.Value.ToString()!),
                            _ when typeSafe == typeof(decimal) => decimal.Parse(paramOption.Value.ToString()!),
                            _ when typeSafe == typeof(DateTime) => DateTime.Parse((string)paramOption.Value),
                            _ when typeSafe == typeof(DateTimeOffset) => DateTimeOffset.Parse((string)paramOption.Value),
                            _ when typeSafe == typeof(TimeSpan) => TimeSpan.Parse((string)paramOption.Value),
                            _ when typeSafe == typeof(SocketRole) => paramOption.Value,
                            _ when typeSafe == typeof(SocketUser) => paramOption.Value,
                            _ when typeSafe == typeof(SocketChannel) => paramOption.Value,
                            _ when typeSafe == typeof(Attachment) => paramOption.Value,
                            _ when typeSafe == typeof(Guid) => Guid.Parse((string)paramOption.Value),
                            _ => throw new ArgumentOutOfRangeException(nameof(typeSafe), typeSafe, $"Could not match type with {typeSafe.Name}")
                        })!);
                    }
                }
            }
            while (args.Count != parameters.Length)
                args.Add(null!);

            if (method.ReturnType == typeof(Task))
                await ((Task?)method.Invoke(instance, args.ToArray()))!;
            else
                method.Invoke(instance, args.ToArray());
        }
        catch (Exception e)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            _logger.LogError(e, $"SlashCommandExecuted failed while executing: `/{command} {subCommand}` with args: {string.Join(", ", args.Where(t => t is not null).Select(t => t.ToString()))}");
        }
    }
}
