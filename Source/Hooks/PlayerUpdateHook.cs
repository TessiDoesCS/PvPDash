using Celeste.Mod.CelesteNet.Client.Entities;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Celeste.Mod.PvPDash.Entities;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Celeste.Mod.PvPDash.Hooks;

public static class PlayerUpdateHook
{
    /* ConcurrentDictionary here, because idk if celestenet does multithreading stuff or smth */
    private static ConcurrentDictionary<uint, int> playerIDToTeam = new ConcurrentDictionary<uint, int>();
    /* maybe make this a setting, but cheating would be very easy that way (could make it, so only people with the same lifetime spawn spinners maybe) */
    private static float spinnerLifeTimeSeconds = 3;
    /* here, so the players spinners, where ping isnt a problem, dont overlap so much */
    private static float playerSpinnerSpawnColldown = 0;
    /* to not continuously remove the team tag in the player nametag, in case other mods ad something similar to the player tag */
    private static bool playerHasTeamTag = false;

    internal static void load()
    {
        On.Celeste.Player.Update += doModLogic;
    }

    internal static void unload()
    {
        On.Celeste.Player.Update -= doModLogic;
    }

    private static void doModLogic(On.Celeste.Player.orig_Update orig, Player self)
    {
        /* put destroyAll() before spawning, or some spinners might not be destroyed for some reason */
        if (!PvPDashModule.Settings.EnablePvPDash)
        {
            removePlayerTeamTagIfThere();
            orig(self);
            return;
        }
        UnfriendlyDashSpinner.updateTimeScinceLastSpawn(Monocle.Engine.DeltaTime);
        var ghosts = Monocle.Engine.Scene.Entities.FindAll<Ghost>();
        if (ghosts == null)
        {
            orig(self);
            return;
        }
        foreach (Ghost ghost in ghosts)
        {
            setGhostNameTag(ghost);
            destroyAllDashSpinnersOnDeath(ghost);
            addUnfriendlySpinnerOnDash(ghost);
            addFriendlySpinnersOnTeammatesDash(ghost);
        }
        setPlayerNameTag();
        deleteDuplicatePlayerNameTags();
        addSpinnerOnPlayerDash(self);
        orig(self);
    }

    private static void deleteDuplicatePlayerNameTags()
    {
        /* sometimes, if you die there will be a duplicate nametag on the player of which only one is the actual one */
        /* this looks bad, so this function removes it */
        var playerNameTags = Monocle.Engine.Scene.Entities.FindAll<GhostNameTag>();
        playerNameTags.RemoveAll((tag) => !(tag.Tracking is Player));
        playerNameTags.RemoveAll((tag) => Regex.Match(tag.Name, @"  \[\d[\d]?[\d]?\]").Success);
        if (playerNameTags.Count == 0) { return; }
        foreach (GhostNameTag extraPlayerNameTag in playerNameTags)
        {
            extraPlayerNameTag.RemoveSelf();
        }
    }

    private static void removePlayerTeamTagIfThere()
    {
        if (playerHasTeamTag && CelesteNet.Client.CelesteNetClientModule.Instance?.Context?.Main?.PlayerNameTag != null)
        {
            GhostNameTag playerNameTag = CelesteNet.Client.CelesteNetClientModule.Instance?.Context?.Main?.PlayerNameTag;
            playerNameTag.Name = Regex.Replace(playerNameTag.Name, @"  \[\d[\d]?[\d]?\]", "");
            playerHasTeamTag = false;
        }
    }

    private static void setGhostNameTag(Ghost ghost)
    {
        if (playerIDToTeam.ContainsKey(ghost.PlayerInfo.ID))
        {
            setGhostNameTagAccordingToTeam(ghost);
        }
    }

    private static void setGhostNameTagAccordingToTeam(Ghost ghost)
    {
        /* if this regex appears in a different context, this will need to be changed, but its simple and should hopefully work */
        if (Regex.Match(ghost.NameTag.Name, @"  \[\d[\d]?[\d]?\]").Success)
        {
            ghost.NameTag.Name = Regex.Replace(ghost.NameTag.Name, @"  \[\d[\d]?[\d]?\]", "  [" + playerIDToTeam.GetValueOrDefault(ghost.PlayerInfo.ID).ToString() + "]");
        }
        else
        {
            ghost.NameTag.Name += "  [" + playerIDToTeam.GetValueOrDefault(ghost.PlayerInfo.ID).ToString() + "]";
        }
    }

    private static void setPlayerNameTag()
    {
        GhostNameTag playerNameTag = CelesteNet.Client.CelesteNetClientModule.Instance?.Context?.Main?.PlayerNameTag;
        if (playerNameTag == null) { return; }
        /* if this regex appears in a different context, this will need to be changed, but its simple and should hopefully work */
        if (Regex.Match(playerNameTag.Name, @"  \[\d[\d]?[\d]?\]").Success)
        {
            playerNameTag.Name = Regex.Replace(playerNameTag.Name, @"  \[\d[\d]?[\d]?\]", "  [" + PvPDashModule.Settings.Team.ToString() + "]");
        }
        else
        {
            playerNameTag.Name += "  [" + PvPDashModule.Settings.Team.ToString() + "]";
        }
        playerHasTeamTag = true;
    }

    private static void addUnfriendlySpinnerOnDash(Ghost ghost)
    {
        int ghostsTeam = playerIDToTeam.GetValueOrDefault(ghost.PlayerInfo.ID);
        bool ghostHasTeam = playerIDToTeam.ContainsKey(ghost.PlayerInfo.ID);
        bool ghotIsOnUnfriendlyTeam = ghostsTeam != PvPDashModule.Settings.Team && ghostHasTeam ||
                                       ghostsTeam == 0 && (ghostHasTeam || PvPDashModule.Settings.MakePlayersWithoutTheModEnemies);
        if (ghost.DashDir != null && !ghost.Dead && ghotIsOnUnfriendlyTeam)
        {
            UnfriendlyDashSpinner spinner = new UnfriendlyDashSpinner(ghost.Position, true, CrystalColor.Red, ghost, lifeTimeSeconds: spinnerLifeTimeSeconds);
            Monocle.Engine.Scene.Add(spinner);
        }
    }

    private static void addFriendlySpinnersOnTeammatesDash(Ghost ghost)
    {
        bool ghostIsOnSameTeam = playerIDToTeam.GetValueOrDefault(ghost.PlayerInfo.ID) == PvPDashModule.Settings.Team && playerIDToTeam.ContainsKey(ghost.PlayerInfo.ID);
        if (ghost.DashDir != null && !ghost.Dead && ghostIsOnSameTeam && playerIDToTeam[ghost.PlayerInfo.ID] != 0)
        {
            Monocle.Engine.Scene.Add(new FriendlyDashSpinner(ghost.Position, new Vector2(0, 0), ghost, spinnerLifeTimeSeconds));
        }
    }

    private static void addSpinnerOnPlayerDash(Player self)
    {
        if (PvPDashModule.Settings.SpawnEnemieSpinnersYourself && PvPDashModule.Settings.Team == 0)
        {
            spawnUnfriendlySpinnersOnPlayerDash(self);
        }
        else
        {
            spawnFriendlySpinnersOnPlayerDash(self);
        }
        playerSpinnerSpawnColldown -= Monocle.Engine.DeltaTime;
    }

    private static void spawnUnfriendlySpinnersOnPlayerDash(Player self)
    {
        if (playerSpinnerSpawnColldown <= 0 && self.StateMachine.State == Player.StDash)
        {
            UnfriendlyDashSpinner spinner = new UnfriendlyDashSpinner(self.Position, true, CrystalColor.Red, null, lifeTimeSeconds: spinnerLifeTimeSeconds, collideCooldown: 0.3f);
            Monocle.Engine.Scene.Add(spinner);
            playerSpinnerSpawnColldown = 0.05f;
        }
    }

    private static void spawnFriendlySpinnersOnPlayerDash(Player self)
    {
        if (playerSpinnerSpawnColldown <= 0 && self.StateMachine.State == Player.StDash)
        {
            Monocle.Engine.Scene.Add(new FriendlyDashSpinner(self.Position, new Vector2(0, 0), null, spinnerLifeTimeSeconds));
            playerSpinnerSpawnColldown = 0.05f;
        }
    }

    private static void destroyAllDashSpinnersOnDeath(Ghost ghost)
    {
        if (!PvPDashModule.Settings.KeepDashSpinnersOnGhostDeath && ghost.Dead)
        {
            UnfriendlyDashSpinner.destroyAll();
            FriendlyDashSpinner.destroyAll();
        }
    }

    public static void addOrChangePlayerTeam(uint playerID, int playerTeam)
    {
        playerIDToTeam.AddOrUpdate(playerID, (a) => playerTeam, (a, b) => playerTeam);
    }
}
