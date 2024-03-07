using DERPWebsite.Services;
using Lumina.Data;
using Lumina.Excel.GeneratedSheets;
using NetStone.GameData;

namespace DERPWebsite.Lodestone;

public class LodestoneGameClient : IGameDataProvider
{
    private readonly GameClient _gameClient;

    public LodestoneGameClient(GameClient gameClient)
    {
        _gameClient = gameClient;
    }

    public NamedGameData? GetItem(string name)
    {
        var rowId = GetItemRowId(name);
        if (rowId == 0)
            return null;
        var (en, de, fr, ja) = GetItemRow(rowId);
        return new NamedGameData
        {
            Info = new GameDataInfo
            {
                Key = rowId,
                Name = name
            },
            Name = new LanguageStrings
            {
                De = de.Name,
                En = en.Name,
                Fr = fr.Name,
                Ja = ja.Name
            }
        };
    }

    private uint GetItemRowId(string name)
    {
        var en = _gameClient.GetSheet<Item>()!;
        var row = en.FirstOrDefault(x => x.Name.ToString().Equals(name, StringComparison.InvariantCultureIgnoreCase));
        return row?.RowId ?? 0;
    }

    private (Item en, Item de, Item fr, Item ja) GetItemRow(uint rowId)
    {
        var en = _gameClient.GetSheet<Item>(Language.English)!.GetRow(rowId)!;
        var de = _gameClient.GetSheet<Item>(Language.German)!.GetRow(rowId)!;
        var fr = _gameClient.GetSheet<Item>(Language.French)!.GetRow(rowId)!;
        var ja = _gameClient.GetSheet<Item>(Language.Japanese)!.GetRow(rowId)!;
        return (en, de, fr, ja);
    }
}
