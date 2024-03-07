using System.Text;
using Discord;
using Discord.WebSocket;
using NetStone;
using NetStone.Search.Character;
using SkiaSharp;

namespace DERPWebsite.Discord;

// [SlashCommand("character", "A collection of character commands tied to lodestone")]
// public partial class Character : ISlashCommandProcessor
// {
//     private readonly LodestoneClient _lodestone;
//     private readonly ILogger<Market> _logger;
//     private readonly SocketSlashCommand _arg;
//
//     public Character(LodestoneClient lodestone, ILogger<Market> logger, SocketSlashCommand command)
//     {
//         _lodestone = lodestone;
//         _logger = logger;
//         _arg = command;
//     }
//
//     [SlashCommand("search", "Searches for a character")]
//     public async Task Search(string name, string world = "", string datacenter = "")
//     {
//         var query = await _lodestone.SearchCharacter(new CharacterSearchQuery
//         { CharacterName = name, World = world, DataCenter = datacenter });
//         if (query is not { HasResults: true })
//         {
//             await _arg.ModifyOriginalResponseAsync(msg =>
//             {
//                 msg.Content = "No results found for query";
//             });
//             return;
//         }
//
//         if (query.Results.Count() > 1)
//         {
//             var builder = new EmbedBuilder();
//             var sb = new StringBuilder();
//             builder.WithTitle("Multiple results found");
//             foreach (var entry in query.Results)
//             {
//                 var str = $"{entry.Name} [{entry.Server}]";
//                 if (sb.Length + str.Length > 4096)
//                     break;
//                 sb.AppendLine($"{entry.Name} [{entry.Server}]");
//             }
//             builder.WithDescription(sb.ToString());
//
//             await _arg.ModifyOriginalResponseAsync(msg =>
//             {
//                 msg.Content = null;
//                 msg.Embed = builder.Build();
//             });
//             return;
//         }
//
//         var character = await query.Results.First().GetCharacter();
//         if (character is null)
//         {
//             await _arg.ModifyOriginalResponseAsync(msg =>
//             {
//                 msg.Content = "Error while fetching character";
//             });
//         }
//
//         var characterCJInfo = await character.GetClassJobInfo();
//         if (characterCJInfo is null)
//         {
//             await _arg.ModifyOriginalResponseAsync(msg =>
//             {
//                 msg.Content = "Error while fetching character class job info";
//             });
//         }
//
//     }
// }
