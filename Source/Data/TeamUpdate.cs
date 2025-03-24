/* thanks to the devs of the deathlink and head2head mod for making their mods open source, rly helped with this and the data :D */
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Celeste.Mod.PvPDash.IO;

namespace Celeste.Mod.PvPDash.Data;
public class TeamUpdate : DataType<TeamUpdate>
{
    public DataPlayerInfo player;
    public uint playerID;
    public int team;
    public string cNetChannel;

    static TeamUpdate()
    {
        DataID = "PvPDashTeamUpdate";
    }

    public TeamUpdate()
    {
        cNetChannel = CNetComm.Instance.CurrentChannel?.Name;
        team = PvPDashModule.Settings.Team;
        player = CelesteNet.Client.CelesteNetClientModule.Instance.Client?.PlayerInfo;
        playerID = (uint)(player?.ID);
    }

    public override MetaType[] GenerateMeta(DataContext ctx)
          => new MetaType[] {
        new MetaPlayerPrivateState(player),
          };

    public override void FixupMeta(DataContext ctx)
    {
        player = Get<MetaPlayerPrivateState>(ctx);
    }
    protected override void Read(CelesteNetBinaryReader reader)
    {
        playerID = reader.ReadUInt32();
        team = reader.ReadInt32();
        cNetChannel = reader.ReadNetString();
    }

    protected override void Write(CelesteNetBinaryWriter writer)
    {
        writer.Write(playerID);
        writer.Write(team);
        writer.WriteNetString(cNetChannel);
    }
}
