using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using NemosMagicMod.Spells;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using static Spell;

//Enhanced TreeSpirit with dual axes at Master tier

public class TreeSpirit : Spell, IRenderable
{
    private const string BuffId = "NemosMagicMod_TreeSpirit";
    private Texture2D axeTexture;
    private Texture2D buffIconTexture;
    private Farmer owner;
    private bool subscribed = false;

    private readonly float moveSpeed = 256f;

    private float spellTimer = 0f;
    private float spellDuration = 20f; // Base duration - will be modified by tier

    protected override SpellbookTier MinimumTier => SpellbookTier.Apprentice;

    // Tier-based duration multipliers
    private readonly Dictionary<SpellbookTier, float> durationMultipliers = new()
    {
        { SpellbookTier.Novice, 1.0f },     // 20 seconds base
        { SpellbookTier.Apprentice, 1.5f }, // 30 seconds
        { SpellbookTier.Adept, 2.0f },      // 40 seconds
        { SpellbookTier.Master, 2.5f }      // 50 seconds
    };

    public bool IsActive { get; private set; }

    // Axe data structure
    private class AxeInstance
    {
        public Vector2 Position;
        public Vector2? CurrentTargetTile;
        public float ChopTimer;
        public float SwingAngle;
        public int SwingDirection = 1;
        public bool IsReturning;

        public AxeInstance(Vector2 startPosition)
        {
            Position = startPosition;
            CurrentTargetTile = null;
            ChopTimer = 0f;
            SwingAngle = 0f;
            SwingDirection = 1;
            IsReturning = false;
        }
    }

    private List<AxeInstance> axes = new List<AxeInstance>();
    private readonly float chopInterval = 0.8f;
    private readonly float swingSpeed = 5f;
    private readonly float maxSwingAngle = 0.5f;

    private int axeCount = 1; // Will be set based on tier

    public TreeSpirit()
        : base("spirit_tree", "Tree Spirit",
              "Summons magical axe(s) that chop trees. Duration increases with spellbook tier. Master tier spawns TWO axes!",
              30, 25, false,
              "assets/TreeSpiritAxe.png")
    {
        try
        {
            axeTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/TreeSpiritAxe.png");
            buffIconTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/TreeSpiritBuffIcon.png");
            iconTexture = axeTexture;

            ModEntry.Instance.Monitor.Log($"TreeSpirit textures loaded - Axe: {axeTexture?.Width}x{axeTexture?.Height}, BuffIcon: {buffIconTexture?.Width}x{buffIconTexture?.Height}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            ModEntry.Instance.Monitor.Log($"Failed to load TreeSpirit textures: {ex.Message}", LogLevel.Error);
            // Fallback to axe texture for both if buff icon fails to load
            buffIconTexture = axeTexture;
        }
    }

    public void Unsubscribe()
    {
        if (!subscribed) return;

        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribed = false;

        // Clear axes
        axes.Clear();

        // Remove buff when spell ends
        if (owner != null && owner.buffs.IsApplied(BuffId))
        {
            owner.buffs.Remove(BuffId);
        }
    }

    /// <summary>
    /// Calculates the spell duration based on the current spellbook tier
    /// </summary>
    private float GetTierAdjustedDuration(Farmer who)
    {
        var currentTier = GetCurrentSpellbookTier(who);
        var multiplier = durationMultipliers.GetValueOrDefault(currentTier, 1.0f);
        return 20f * multiplier; // Base 20 seconds * tier multiplier
    }

    /// <summary>
    /// Gets the number of axes to spawn based on tier
    /// </summary>
    private int GetAxeCount(SpellbookTier tier)
    {
        return tier == SpellbookTier.Master ? 2 : 1;
    }

    private void ApplyTreeSpiritBuff(Farmer who, float duration)
    {
        // Remove existing Tree Spirit buff if present
        if (who.buffs.IsApplied(BuffId))
            who.buffs.Remove(BuffId);

        // Convert spell duration to milliseconds
        int durationMs = (int)(duration * 1000);

        var currentTier = GetCurrentSpellbookTier(who);
        var axeCountText = axeCount > 1 ? $"{axeCount} axes" : "an axe";

        var buff = new Buff(
            id: BuffId,
            displayName: "Tree Spirit",
            iconTexture: buffIconTexture,
            iconSheetIndex: 0,
            duration: durationMs,
            effects: new BuffEffects(),
            description: $"{axeCountText} chopping trees for you! ({currentTier} tier)"
        );

        who.buffs.Apply(buff);
        ModEntry.Instance.Monitor.Log($"Tree Spirit buff applied for {duration} seconds ({currentTier} tier, {axeCount} axe(s))", LogLevel.Info);
    }

    public override void Cast(Farmer who)
    {
        if (!CanCast(who))
            return;

        base.Cast(who);

        // Set duration and axe count based on current spellbook tier
        var currentTier = GetCurrentSpellbookTier(who);
        spellDuration = GetTierAdjustedDuration(who);
        axeCount = GetAxeCount(currentTier);

        // Show tier-specific message
        var durationSeconds = (int)spellDuration;
        var axeText = axeCount > 1 ? $"{axeCount} Tree Spirits" : "Tree Spirit";
        Game1.addHUDMessage(new HUDMessage($"{axeText} summoned for {durationSeconds}s ({currentTier} tier)", 2));

        // --- Delay the summoning of the magical axe(s) ---
        DelayedAction.functionAfterDelay(() =>
        {
            owner = who;

            // Clean up any existing Tree Spirit subscription first
            if (subscribed)
            {
                Unsubscribe();
            }

            // Initialize new Tree Spirit state
            IsActive = true;
            spellTimer = 0f;

            // Create axe instances based on tier
            axes.Clear();
            for (int i = 0; i < axeCount; i++)
            {
                Vector2 startOffset = axeCount > 1
                    ? new Vector2((i - 0.5f) * 48f, -64f) // Spread them out for dual axes
                    : new Vector2(0, -64f); // Single axe centered

                var axeInstance = new AxeInstance(who.Position + startOffset);
                axes.Add(axeInstance);
            }

            // Apply the buff
            ApplyTreeSpiritBuff(who, spellDuration);

            // Subscribe to events
            ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            subscribed = true;

            Game1.playSound("axe");

        }, 1000); // 1-second delay
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!IsActive)
        {
            Unsubscribe();
            return;
        }

        float deltaSeconds = 1f / 60f;
        spellTimer += deltaSeconds;

        if (spellTimer >= spellDuration)
        {
            IsActive = false;
            Unsubscribe();

            // Show completion message with tier info
            if (owner != null)
            {
                var currentTier = GetCurrentSpellbookTier(owner);
                var axeText = axeCount > 1 ? "Tree Spirits" : "Tree Spirit";
                Game1.addHUDMessage(new HUDMessage($"{axeText} dismissed ({currentTier} tier)", 1));
            }
            return;
        }

        // Update each axe
        for (int i = 0; i < axes.Count; i++)
        {
            UpdateAxe(axes[i], deltaSeconds);
        }
    }

    private void UpdateAxe(AxeInstance axe, float deltaSeconds)
    {
        // Acquire nearest tree if no target (that's not already being targeted by another axe)
        if (axe.CurrentTargetTile == null)
        {
            axe.CurrentTargetTile = FindNearestTreeForAxe(axe);
            axe.ChopTimer = 0f;
        }

        if (axe.CurrentTargetTile != null)
        {
            Vector2 targetWorld = axe.CurrentTargetTile.Value * Game1.tileSize + new Vector2(Game1.tileSize / 2, -32f);
            Vector2 direction = targetWorld - axe.Position;

            const float arrivalThreshold = 8f; // pixels
            if (direction.LengthSquared() > arrivalThreshold * arrivalThreshold)
            {
                direction.Normalize();
                axe.Position += direction * moveSpeed * deltaSeconds;
            }
            else
            {
                axe.Position = targetWorld;
                axe.ChopTimer += deltaSeconds;
                if (axe.ChopTimer >= chopInterval)
                {
                    DoAxeSwingAt(axe.CurrentTargetTile.Value);
                    axe.ChopTimer = 0f;
                }

                // Swing animation
                axe.SwingAngle += axe.SwingDirection * swingSpeed * deltaSeconds;
                if (axe.SwingAngle > maxSwingAngle)
                {
                    axe.SwingAngle = maxSwingAngle;
                    axe.SwingDirection = -1;
                }
                if (axe.SwingAngle < -maxSwingAngle)
                {
                    axe.SwingAngle = -maxSwingAngle;
                    axe.SwingDirection = 1;
                }

                // If tree destroyed
                if (!Game1.currentLocation.terrainFeatures.ContainsKey(axe.CurrentTargetTile.Value))
                {
                    axe.CurrentTargetTile = null;
                    axe.SwingAngle = 0f;
                }
            }

            axe.IsReturning = false;
        }
        else
        {
            axe.IsReturning = true;
        }

        // Return to player if no target
        if (axe.IsReturning && owner != null)
        {
            // Determine target position based on axe index
            int axeIndex = axes.IndexOf(axe);
            Vector2 targetPosition;

            if (axeCount > 1)
            {
                // For dual axes, mirror positions with double distance
                Vector2 baseOffset = new Vector2(0, -72f); // Original follow position
                if (axeIndex == 0)
                {
                    // First axe stays in original position
                    targetPosition = owner.Position + baseOffset;
                }
                else
                {
                    // Second axe mirrors to the opposite side
                    Vector2 mirroredOffset = new Vector2(-baseOffset.X, baseOffset.Y); // Mirror X, keep Y
                    // Add horizontal separation
                    mirroredOffset.X += 72f; // Move to the right side
                    targetPosition = owner.Position + mirroredOffset;
                }
            }
            else
            {
                // Single axe hovers above player
                targetPosition = owner.Position + new Vector2(0, -72f);
            }

            Vector2 direction = targetPosition - axe.Position;
            const float returnThreshold = 4f;

            if (direction.LengthSquared() > returnThreshold * returnThreshold)
            {
                direction.Normalize();
                axe.Position += direction * moveSpeed * deltaSeconds;
            }
        }
    }

    private Vector2? FindNearestTreeForAxe(AxeInstance requestingAxe)
    {
        if (Game1.currentLocation == null) return null;

        Vector2 axeTile = new((int)Math.Floor(requestingAxe.Position.X / Game1.tileSize),
                              (int)Math.Floor(requestingAxe.Position.Y / Game1.tileSize));

        double closestDist = double.MaxValue;
        Vector2? closestTile = null;

        // Get list of tiles already being targeted by other axes
        var targetedTiles = new HashSet<Vector2>();
        foreach (var otherAxe in axes)
        {
            if (otherAxe != requestingAxe && otherAxe.CurrentTargetTile.HasValue)
            {
                targetedTiles.Add(otherAxe.CurrentTargetTile.Value);
            }
        }

        foreach (var pair in Game1.currentLocation.terrainFeatures.Pairs)
        {
            if (pair.Value is Tree tree && tree.growthStage.Value >= 5 && !targetedTiles.Contains(pair.Key))
            {
                double dist = Vector2.DistanceSquared(pair.Key, axeTile);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestTile = pair.Key;
                }
            }
        }

        return closestTile;
    }

    private void DoAxeSwingAt(Vector2 tile)
    {
        if (owner == null) return;

        // SOLUTION 1: Temporarily move farmer to tree location for particle spawning
        Vector2 originalPosition = owner.Position;
        Vector2 treeWorldPos = tile * Game1.tileSize + new Vector2(Game1.tileSize / 2, Game1.tileSize / 2);

        // Move farmer to tree temporarily (particles will spawn here)
        owner.Position = treeWorldPos;

        Axe axe = new();
        float oldStamina = owner.stamina;

        axe.DoFunction(Game1.currentLocation, (int)treeWorldPos.X, (int)treeWorldPos.Y, 1, owner);

        // Restore farmer position and stamina
        owner.Position = originalPosition;
        owner.stamina = oldStamina;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!IsActive) return;

        SpriteBatch spriteBatch = e.SpriteBatch;

        // Render each axe behind the farmer
        foreach (var axe in axes)
        {
            Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, axe.Position);

            spriteBatch.Draw(
                axeTexture,
                screenPos,
                null,
                Color.White,
                axe.SwingAngle,
                new Vector2(axeTexture.Width / 2, axeTexture.Height / 2),
                2f,
                SpriteEffects.None,
                0.3f // Render behind farmer but ahead of background
            );
        }
    }
}