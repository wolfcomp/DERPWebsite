using System.Text;
using NetStone;
using NetStone.Search.Character;

LodestoneClient _lodestone = await LodestoneClient.GetClientAsync();

var query = await _lodestone.SearchCharacter(new CharacterSearchQuery
{ CharacterName = "Lexina Hildr", World = "Seraph" });
if (query is not { HasResults: true })
{
    return;
}

if (query.Results.Count() > 1)
{
    return;
}

var character = await query.Results.First().GetCharacter();

if (character is null)
{
    return;
}

var characterCJInfo = await _lodestone.GetCharacterClassJob(query.Results.First().Id!);

if (characterCJInfo is null)
{
    return;
}