using Microsoft.Xna.Framework;
using Celeste.Mod.CelesteNet.Client.Entities;
using System.Collections.Generic;

namespace Celeste.Mod.PvPDash.Entities;

public class UnfriendlyDashSpinner : CrystalStaticSpinner
{

    /* this is here, so there is more consistency between the spinners spawned by the player and the ghosts */
    public static float spinnerSpawnIntervalSeconds = 0.03f;
    private static Dictionary<Ghost, float> timeScinceLastSpawnPerGhost = new Dictionary<Ghost, float>();
    private float aliveTime = 0;
    private float lifeTimeSeconds;
    private bool destroyParticles;
    private bool shouldExist = true;
    private static List<UnfriendlyDashSpinner> currentUnfriendlySpinners = new List<UnfriendlyDashSpinner>();
    /* if the spinner spawns on the player, it shouldnt instantly kill them */
    private float collideCooldown;
    private Monocle.Collider ourCollider;

    /// <summary>
    /// the object will not be added to a scene, if a spinner has already spawned too recently(at time of calling the constructor)
    /// </summary>
    /// <param name="destroyParticles">if particles should be spawned, when the spinner is destroyed</param>
    /// <param name="ghost"> set to null to have it not be liked to a ghost and therefore not have a spawn cooldown. </param>
    /// <returns></returns>
    public UnfriendlyDashSpinner(Vector2 position, bool attachToSolid, CrystalColor color, Ghost ghost, float lifeTimeSeconds = 5, bool destroyParticles = false, float collideCooldown = 0f) : base(position, attachToSolid, color)
    {
        this.destroyParticles = destroyParticles;
        this.lifeTimeSeconds = lifeTimeSeconds;
        this.collideCooldown = collideCooldown;
        lock (currentUnfriendlySpinners) { currentUnfriendlySpinners.Add(this); }
        /* spinner is spawned on player */
        if (ghost == null) { return; }
        if (!canSpawnSpinner(ghost)) { this.shouldExist = false; return; }
        timeScinceLastSpawnPerGhost[ghost] = 0f;
    }

    public override void Added(Monocle.Scene scene)
    {
        base.Added(scene);
        ourCollider = Collider.Clone();
        if (collideCooldown > 0) { Collider = null; }
        if (!shouldExist) { this.RemoveSelf(); }
    }

    public override void Update()
    {
        if (Collider == null && collideCooldown <= 0)
        {
            Collider = ourCollider;
        }
        else
        {
            collideCooldown -= Monocle.Engine.DeltaTime;
        }

        if (aliveTime < lifeTimeSeconds)
        {
            aliveTime += Monocle.Engine.DeltaTime;
            base.Update();
            return;
        }
        base.Update();
        this.remove();
        lock (currentUnfriendlySpinners) { currentUnfriendlySpinners.Remove(this); }
        return;
    }


    /// <summary>
    /// removes all UnfriendlyDashSpinners and spawns destroy particles at spinners where it was specified when calling the constructor
    /// </summary>
    /// <returns></returns>
    public static void destroyAll()
    {
        lock (currentUnfriendlySpinners)
        {
            foreach (var spinner in currentUnfriendlySpinners)
            {
                spinner.remove();
            }
            currentUnfriendlySpinners.Clear();
        }
    }

    public static void updateTimeScinceLastSpawn(float passedTime)
    {
        foreach (Ghost ghost in timeScinceLastSpawnPerGhost.Keys)
        {
            timeScinceLastSpawnPerGhost[ghost] += passedTime;
        }
    }

    private static bool canSpawnSpinner(Ghost ghost)
    {
        return timeScinceLastSpawnPerGhost.ContainsKey(ghost) ? timeScinceLastSpawnPerGhost[ghost] >= spinnerSpawnIntervalSeconds : true;
    }

    /// <summary>
    /// removes this spinner and spawns destroy Particles, if specified at creation
    /// </summary>
    /// <returns></returns>
    public void remove()
    {
        if (destroyParticles) { this.Destroy(); }
        else { this.RemoveSelf(); }
    }
}
