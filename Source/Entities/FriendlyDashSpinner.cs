using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;
using Celeste.Mod.CelesteNet.Client.Entities;

namespace Celeste.Mod.PvPDash.Entities;

[CustomEntity("PvPDash/friendlySpinner")]
public class FriendlyDashSpinner : Entity
{

    /* this is here, so there is more consistency between the spinners spawned by the player and the ghosts */
    public static float spinnerSpawnIntervalSeconds = 0.03f;
    private static Dictionary<Ghost, float> timeScinceLastSpawnPerGhost = new Dictionary<Ghost, float>();
    private Sprite sprite = GFX.SpriteBank.Create("friendlySpinner");
    private float aliveTime = 0;
    private float goAwayAfter;
    private static List<FriendlyDashSpinner> currentFriendlySpinners = new List<FriendlyDashSpinner>();
    private bool shouldExist = true;

    public FriendlyDashSpinner(Vector2 Position, Vector2 offset, Ghost ghost, float lifeTimeSeconds = 5)
        : base(Position + offset)
    {
        sprite.Scale = new Vector2(1f, 1f);
        this.Depth = 100;
        Add(sprite);
        this.goAwayAfter = lifeTimeSeconds;
        lock (currentFriendlySpinners) { currentFriendlySpinners.Add(this); }
        /* spinner is spawned on player */
        if (ghost == null) { return; }
        if (!canSpawnSpinner(ghost)) { this.shouldExist = false; return; }
        timeScinceLastSpawnPerGhost[ghost] = 0f;
    }

    private static bool canSpawnSpinner(Ghost ghost)
    {
        return timeScinceLastSpawnPerGhost.ContainsKey(ghost) ? timeScinceLastSpawnPerGhost[ghost] >= spinnerSpawnIntervalSeconds : true;
    }

    public static void updateTimeScinceLastSpawn(float passedTime)
    {
        foreach (Ghost ghost in timeScinceLastSpawnPerGhost.Keys)
        {
            timeScinceLastSpawnPerGhost[ghost] += passedTime;
        }
    }

    public override void Added(Monocle.Scene scene)
    {
        base.Added(scene);
        if (!shouldExist) { this.RemoveSelf(); }
    }

    public override void Update()
    {
        if (aliveTime < goAwayAfter)
        {
            aliveTime += Monocle.Engine.DeltaTime;
            base.Update();
            return;
        }
        base.Update();
        lock (currentFriendlySpinners) { currentFriendlySpinners.Remove(this); }
        this.RemoveSelf();
        return;
    }

    public static void destroyAll()
    {
        lock (currentFriendlySpinners)
        {
            foreach (Monocle.Entity spinner in currentFriendlySpinners)
            {
                spinner.RemoveSelf();
            }
            currentFriendlySpinners.Clear();
        }
    }
}
