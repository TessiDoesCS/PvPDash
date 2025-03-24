namespace Celeste.Mod.PvPDash;

[SettingName("pvpdash_title")]
public class PvPDashModuleSettings : EverestModuleSettings
{
    [SettingName("pvpdash_enable")]
    public bool EnablePvPDash { get; set; }
    [SettingName("pvpdash_keep_spinners")]
    public bool KeepDashSpinnersOnGhostDeath { get; set; }

    [SettingIgnore]
    public int Team { get; private set; }

    [SettingRange(0, 999)]
    [SettingName("pvpdash_team")]
    [SettingSubText("pvpdash_team_subtext")]
    public int YourTeam
    {
        get { return Team; }
        set
        {
            Team = value;
            if (IO.CNetComm.Instance == null || !IO.CNetComm.Instance.IsConnected) { return; }
            IO.CNetComm.Instance?.Send(new Data.TeamUpdate());
        }
    }

    [SettingName("pvpdash_self")]
    [SettingSubText("pvpdash_self_subtext")]
    public bool SpawnEnemieSpinnersYourself { get; set; }

    [SettingName("pvpdash_no_mod_enemies")]
    public bool MakePlayersWithoutTheModEnemies { get; set; }

    public TextMenu.Button ManualTeamUpdate { get; private set; } = null;

    public void CreateManualTeamUpdateEntry(TextMenu menu, bool inGame)
    {
        TextMenu.Button item = new TextMenu.Button("pvpdash_teamupdate".DialogClean());
        item.Pressed(() =>
        {
            if (IO.CNetComm.Instance == null || !IO.CNetComm.Instance.IsConnected) { return; }
            IO.CNetComm.Instance.Send(new Data.TeamUpdateRequest());
        });
        menu.Add(item);
        item.AddDescription(menu, "pvpdash_teamupdate_subtext".DialogClean());
    }
}
