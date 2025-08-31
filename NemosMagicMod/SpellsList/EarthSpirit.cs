using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using NemosMagicMod.Spells;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using static Spell;

//Pickaxe got stuck hovering on a rock. Need to adjust 

public class EarthSpirit : Spell, IRenderable
{
    private Texture2D pickaxeTexture;
    private Pickaxe pickaxe;

    private bool subscribed = false;
    private Vector2 toolPosition;
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

    private Vector2? currentTargetTile = null;
    private float mineTimer = 0f;
    private readonly float mineInterval = 0.8f;

    private float swingAngle = 0f;
    private float swingSpeed = 5f;
    private int swingDirection = 1;
    private float maxSwingAngle = 0.5f;

    private bool isReturning = false;
    private Farmer owner;

    public EarthSpirit()
        : base("nemo.EarthSpirit", "Earth Spirit",
              "Summons a magical pickaxe that mines rocks. Duration increases with spellbook tier.",
              30, 25)
    {
        pickaxeTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/EarthSpiritPickaxe.png");
        pickaxe = new Pickaxe(); // vanilla tool instance
    }

    public void Unsubscribe()
    {
        if (!subscribed) return;

        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribed = false;
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

    public override void Cast(Farmer who)
    {
        // --- Minimum spellbook tier check ---
        if (!HasSufficientSpellbookTier(who))
        {
            string requiredTierName = MinimumTier.ToString();
            Game1.showRedMessage($"Requires {requiredTierName} spellbook or higher!");
            return;
        }

        if (!ManaManager.HasEnoughMana(ManaCost))
        {
            Game1.showRedMessage("Not enough mana!");
            return;
        }

        // Set duration based on current spellbook tier
        spellDuration = GetTierAdjustedDuration(who);

        // Show tier-specific message
        var currentTier = GetCurrentSpellbookTier(who);
        var durationSeconds = (int)spellDuration;
        Game1.addHUDMessage(new HUDMessage($"Earth Spirit summoned for {durationSeconds}s ({currentTier} tier)", 2));

        // Immediate base.Cast: mana, XP, spell activation
        base.Cast(who);

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
            currentTargetTile = null;
            isReturning = false;
            toolPosition = who.Position + new Vector2(0, -64f);

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
                Game1.addHUDMessage(new HUDMessage($"Earth Spirit dismissed ({currentTier} tier)", 1));
            }
            return;
        }

        // Acquire nearest rock if no target
        if (currentTargetTile == null)
        {
            currentTargetTile = FindNearestRock();
            mineTimer = 0f;
        }

        if (currentTargetTile != null)
        {
            Vector2 targetWorld = currentTargetTile.Value * Game1.tileSize + new Vector2(Game1.tileSize / 2, -hoverHeight);
            Vector2 direction = targetWorld - toolPosition;

            const float arrivalThreshold = 8f; // pixels
            if (direction.LengthSquared() > arrivalThreshold * arrivalThreshold)
            {
                direction.Normalize();
                toolPosition += direction * moveSpeed * deltaSeconds;
            }
            else
            {
                toolPosition = targetWorld;
                mineTimer += deltaSeconds;
                if (mineTimer >= mineInterval)
                {
                    MineRockAt(currentTargetTile.Value);
                    mineTimer = 0f;
                }

                // Swing animation
                swingAngle += swingDirection * swingSpeed * deltaSeconds;
                if (swingAngle > maxSwingAngle) { swingAngle = maxSwingAngle; swingDirection = -1; }
                if (swingAngle < -maxSwingAngle) { swingAngle = -maxSwingAngle; swingDirection = 1; }

                if (!Game1.currentLocation.objects.ContainsKey(currentTargetTile.Value))
                {
                    currentTargetTile = null;
                    swingAngle = 0f;
                }
            }

            isReturning = false;
        }
        else
        {
            isReturning = true;
        }

        // Return to player if no target
        if (isReturning && owner != null)
        {
            Vector2 direction = owner.Position - toolPosition;
            if (direction.LengthSquared() > 4f)
            {
                direction.Normalize();
                toolPosition += direction * moveSpeed * deltaSeconds;
            }
        }
    }

    private Vector2? FindNearestRock()
    {
        if (Game1.currentLocation == null) return null;

        Vector2 toolTile = new((int)Math.Floor(toolPosition.X / Game1.tileSize),
                               (int)Math.Floor(toolPosition.Y / Game1.tileSize));

        double closestDist = double.MaxValue;
        Vector2? closestTile = null;

        foreach (var pair in Game1.currentLocation.objects.Pairs)
        {
            if (IsMineableRock(pair.Value))
            {
                double dist = Vector2.DistanceSquared(pair.Key, toolTile);
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
        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, toolPosition);

        spriteBatch.Draw(
            pickaxeTexture,
            screenPos,
            null,
            Color.White,
            swingAngle,
            new Vector2(pickaxeTexture.Width / 2, pickaxeTexture.Height / 2),
            2f,
            SpriteEffects.None,
            1f
        );
    }
}