using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using NemosMagicMod.Spells;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using static Spell;

//Enhanced EarthSpirit with dual pickaxes at Master tier

public class EarthSpirit : Spell, IRenderable
{
    private const string BuffId = "NemosMagicMod_EarthSpirit";
    private Texture2D pickaxeTexture;
    private Texture2D buffIconTexture;
    private Pickaxe pickaxe;

    private bool subscribed = false;
    private readonly float moveSpeed = 256f;

    private float spellTimer = 0f;
    private float spellDuration = 20f; // Base duration - will be modified by tier
    private readonly float hoverHeight = 32f;

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

    // Pickaxe data structure
    private class PickaxeInstance
    {
        public Vector2 Position;
        public Vector2? CurrentTargetTile;
        public float MineTimer;
        public float SwingAngle;
        public int SwingDirection = 1;
        public bool IsReturning;

        public PickaxeInstance(Vector2 startPosition)
        {
            Position = startPosition;
            CurrentTargetTile = null;
            MineTimer = 0f;
            SwingAngle = 0f;
            SwingDirection = 1;
            IsReturning = false;
        }
    }

    private List<PickaxeInstance> pickaxes = new List<PickaxeInstance>();
    private readonly float mineInterval = 0.8f;
    private readonly float swingSpeed = 5f;
    private readonly float maxSwingAngle = 0.5f;

    private Farmer owner;
    private int pickaxeCount = 1; // Will be set based on tier

    public EarthSpirit()
        : base(
            "nemo.EarthSpirit",
            "Earth Spirit",
            "Summons magical pickaxe that mine rocks. Duration increases with spellbook tier.",
            30,
            25,
            false,
            "assets/EarthSpiritPickaxe.png")
    {
        try
        {
            pickaxeTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/EarthSpiritPickaxe.png");
            buffIconTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/EarthSpiritPickaxeBuffIcon.png");
            iconTexture = pickaxeTexture;

            ModEntry.Instance.Monitor.Log($"EarthSpirit textures loaded - Pickaxe: {pickaxeTexture?.Width}x{pickaxeTexture?.Height}, BuffIcon: {buffIconTexture?.Width}x{buffIconTexture?.Height}", LogLevel.Info);
        }
        catch (Exception ex)
        {
            ModEntry.Instance.Monitor.Log($"Failed to load EarthSpirit textures: {ex.Message}", LogLevel.Error);
            // Fallback to pickaxe texture for both if buff icon fails to load
            buffIconTexture = pickaxeTexture;
        }
    }

    public void Unsubscribe()
    {
        if (!subscribed) return;

        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribed = false;

        // Clear pickaxes
        pickaxes.Clear();

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
    /// Gets the number of pickaxes to spawn based on tier
    /// </summary>
    private int GetPickaxeCount(SpellbookTier tier)
    {
        return tier == SpellbookTier.Master ? 2 : 1;
    }

    private void ApplyEarthSpiritBuff(Farmer who, float duration)
    {
        // Remove existing Earth Spirit buff if present
        if (who.buffs.IsApplied(BuffId))
            who.buffs.Remove(BuffId);

        // Convert spell duration to milliseconds
        int durationMs = (int)(duration * 1000);

        var currentTier = GetCurrentSpellbookTier(who);
        var pickaxeCountText = pickaxeCount > 1 ? $"{pickaxeCount} pickaxes" : "a pickaxe";

        var buff = new Buff(
            id: BuffId,
            displayName: "Earth Spirit",
            iconTexture: buffIconTexture,
            iconSheetIndex: 0,
            duration: durationMs,
            effects: new BuffEffects(), // No stat effects, just visual indicator
            description: $"{pickaxeCountText} mining rocks for you! ({currentTier} tier)"
        );

        who.buffs.Apply(buff);
        ModEntry.Instance.Monitor.Log($"Earth Spirit buff applied for {duration} seconds ({currentTier} tier, {pickaxeCount} pickaxe(s))", LogLevel.Info);
    }

    public override void Cast(Farmer who)
    {
        if (!CanCast(who))
            return;

        base.Cast(who);

        // Set duration and pickaxe count based on current spellbook tier
        var currentTier = GetCurrentSpellbookTier(who);
        spellDuration = GetTierAdjustedDuration(who);
        pickaxeCount = GetPickaxeCount(currentTier);

        // Show tier-specific message
        var durationSeconds = (int)spellDuration;
        var pickaxeText = pickaxeCount > 1 ? $"{pickaxeCount} Earth Spirits" : "Earth Spirit";
        Game1.addHUDMessage(new HUDMessage($"{pickaxeText} summoned for {durationSeconds}s ({currentTier} tier)", 2));

        // Delay only the EarthSpirit visual/effects
        DelayedAction.functionAfterDelay(() =>
        {
            owner = who;

            // Clean up any existing Earth Spirit subscription first
            if (subscribed)
            {
                Unsubscribe();
            }

            // Initialize new Earth Spirit state
            IsActive = true;
            spellTimer = 0f;

            // Create pickaxe instances based on tier
            pickaxes.Clear();
            for (int i = 0; i < pickaxeCount; i++)
            {
                Vector2 startOffset = pickaxeCount > 1
                    ? new Vector2((i - 0.5f) * 48f, -64f) // Spread them out for dual pickaxes
                    : new Vector2(0, -64f); // Single pickaxe centered

                var pickaxeInstance = new PickaxeInstance(who.Position + startOffset);
                pickaxes.Add(pickaxeInstance);
            }

            // Apply the buff
            ApplyEarthSpiritBuff(who, spellDuration);

            // Subscribe to events
            ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            subscribed = true;

            Game1.playSound("hammer");

        }, 1000); // 1 second delay before EarthSpirit visual/effects
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
                var pickaxeText = pickaxeCount > 1 ? "Earth Spirits" : "Earth Spirit";
                Game1.addHUDMessage(new HUDMessage($"{pickaxeText} dismissed ({currentTier} tier)", 1));
            }
            return;
        }

        // Update each pickaxe
        for (int i = 0; i < pickaxes.Count; i++)
        {
            UpdatePickaxe(pickaxes[i], deltaSeconds);
        }
    }

    private void UpdatePickaxe(PickaxeInstance pickaxe, float deltaSeconds)
    {
        // Acquire nearest rock if no target (that's not already being targeted by another pickaxe)
        if (pickaxe.CurrentTargetTile == null)
        {
            pickaxe.CurrentTargetTile = FindNearestRockForPickaxe(pickaxe);
            pickaxe.MineTimer = 0f;
        }

        if (pickaxe.CurrentTargetTile != null)
        {
            Vector2 targetWorld = pickaxe.CurrentTargetTile.Value * Game1.tileSize + new Vector2(Game1.tileSize / 2, -hoverHeight);
            Vector2 direction = targetWorld - pickaxe.Position;

            const float arrivalThreshold = 8f; // pixels
            if (direction.LengthSquared() > arrivalThreshold * arrivalThreshold)
            {
                direction.Normalize();
                pickaxe.Position += direction * moveSpeed * deltaSeconds;
            }
            else
            {
                pickaxe.Position = targetWorld;
                pickaxe.MineTimer += deltaSeconds;
                if (pickaxe.MineTimer >= mineInterval)
                {
                    MineRockAt(pickaxe.CurrentTargetTile.Value);
                    pickaxe.MineTimer = 0f;
                }

                // Swing animation
                pickaxe.SwingAngle += pickaxe.SwingDirection * swingSpeed * deltaSeconds;
                if (pickaxe.SwingAngle > maxSwingAngle)
                {
                    pickaxe.SwingAngle = maxSwingAngle;
                    pickaxe.SwingDirection = -1;
                }
                if (pickaxe.SwingAngle < -maxSwingAngle)
                {
                    pickaxe.SwingAngle = -maxSwingAngle;
                    pickaxe.SwingDirection = 1;
                }

                if (!Game1.currentLocation.objects.ContainsKey(pickaxe.CurrentTargetTile.Value))
                {
                    pickaxe.CurrentTargetTile = null;
                    pickaxe.SwingAngle = 0f;
                }
            }

            pickaxe.IsReturning = false;
        }
        else
        {
            pickaxe.IsReturning = true;
        }

        // Return to player if no target
        if (pickaxe.IsReturning && owner != null)
        {
            // Determine target position based on pickaxe index
            int pickaxeIndex = pickaxes.IndexOf(pickaxe);
            Vector2 targetPosition;

            if (pickaxeCount > 1)
            {
                // For dual pickaxes, mirror positions
                Vector2 baseOffset = new Vector2(0, -72f); // Original follow position
                if (pickaxeIndex == 0)
                {
                    // First pickaxe stays in original position
                    targetPosition = owner.Position + baseOffset;
                }
                else
                {
                    // Second pickaxe mirrors to the opposite side
                    Vector2 mirroredOffset = new Vector2(-baseOffset.X, baseOffset.Y); // Mirror X, keep Y
                    // Add some horizontal separation so they don't overlap
                    mirroredOffset.X += 72f; // Move to the right side
                    targetPosition = owner.Position + mirroredOffset;
                }
            }
            else
            {
                // Single pickaxe hovers above player
                targetPosition = owner.Position + new Vector2(0, -72f);
            }

            Vector2 direction = targetPosition - pickaxe.Position;
            if (direction.LengthSquared() > 4f)
            {
                direction.Normalize();
                pickaxe.Position += direction * moveSpeed * deltaSeconds;
            }
        }
    }

    private Vector2? FindNearestRockForPickaxe(PickaxeInstance requestingPickaxe)
    {
        if (Game1.currentLocation == null) return null;

        Vector2 pickaxeTile = new((int)Math.Floor(requestingPickaxe.Position.X / Game1.tileSize),
                                  (int)Math.Floor(requestingPickaxe.Position.Y / Game1.tileSize));

        double closestDist = double.MaxValue;
        Vector2? closestTile = null;

        // Get list of tiles already being targeted by other pickaxes
        var targetedTiles = new HashSet<Vector2>();
        foreach (var otherPickaxe in pickaxes)
        {
            if (otherPickaxe != requestingPickaxe && otherPickaxe.CurrentTargetTile.HasValue)
            {
                targetedTiles.Add(otherPickaxe.CurrentTargetTile.Value);
            }
        }

        foreach (var pair in Game1.currentLocation.objects.Pairs)
        {
            if (IsMineableRock(pair.Value) && !targetedTiles.Contains(pair.Key))
            {
                double dist = Vector2.DistanceSquared(pair.Key, pickaxeTile);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestTile = pair.Key;
                }
            }
        }

        return closestTile;
    }

    private void MineRockAt(Vector2 tile)
    {
        if (Game1.currentLocation == null || owner == null) return;

        if (!Game1.currentLocation.objects.TryGetValue(tile, out var obj) || !IsMineableRock(obj))
            return;

        try
        {
            float oldStamina = owner.stamina;

            // Create a pickaxe instance if we don't have one
            if (pickaxe == null)
            {
                pickaxe = new Pickaxe();
                pickaxe.UpgradeLevel = 4; // Set to max level (Iridium)
            }

            // Use the pickaxe's DoFunction like a normal tool swing
            pickaxe.DoFunction(Game1.currentLocation, (int)tile.X * Game1.tileSize, (int)tile.Y * Game1.tileSize, 1, owner);

            // Prevent stamina drain
            owner.stamina = oldStamina;

            Game1.playSound("hammer");
            Game1.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(12, tile * Game1.tileSize, Color.Gray));
        }
        catch (Exception ex)
        {
            ModEntry.Instance.Monitor.Log($"Failed to mine rock at {tile}: {ex}", StardewModdingAPI.LogLevel.Warn);
        }
    }

    private bool IsMineableRock(StardewValley.Object obj)
    {
        return obj != null && (obj.Name.Contains("Stone") || obj.Name.Contains("Ore") || obj.Name.Contains("Geode"));
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!IsActive) return;

        SpriteBatch spriteBatch = e.SpriteBatch;

        // Render each pickaxe
        foreach (var pickaxe in pickaxes)
        {
            Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, pickaxe.Position);

            spriteBatch.Draw(
                pickaxeTexture,
                screenPos,
                null,
                Color.White,
                pickaxe.SwingAngle,
                new Vector2(pickaxeTexture.Width / 2, pickaxeTexture.Height / 2),
                2f,
                SpriteEffects.None,
                0.001f
            );
        }
    }
}