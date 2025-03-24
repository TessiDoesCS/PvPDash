using System;
using Celeste.Mod.PvPDash.Hooks;

namespace Celeste.Mod.PvPDash;

public class PvPDashModule : EverestModule
{
    public static PvPDashModule Instance { get; private set; }

    public override Type SettingsType => typeof(PvPDashModuleSettings);
    public static PvPDashModuleSettings Settings => (PvPDashModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(PvPDashModuleSession);
    public static PvPDashModuleSession Session => (PvPDashModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(PvPDashModuleSaveData);
    public static PvPDashModuleSaveData SaveData => (PvPDashModuleSaveData)Instance._SaveData;

    private IO.CNetComm Comm;
    public PvPDashModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(PvPDashModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(PvPDashModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        Celeste.Instance.Components.Add(Comm = new IO.CNetComm(Celeste.Instance));
        PlayerUpdateHook.load();
    }

    public override void Unload()
    {
        Celeste.Instance.Components.Remove(Comm);
        PlayerUpdateHook.unload();
    }
}
