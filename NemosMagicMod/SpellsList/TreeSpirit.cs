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
using static Spell;

//Fixed: Particles now spawn at tree location or not at all

public class TreeSpirit : Spell, IRenderable
{
    private const string BuffId = "NemosMagicMod_TreeSpirit";
    private Texture2D axeTexture;
    private Texture2D buffIconTexture;
    private Farmer owner;
    private bool subscribed = false;

    private Vector2 axePosition;
    private readonly float moveSpeed = 256f;

    private float spellTimer = 0f;
    private readonly float spellDuration = 20f;

    private Vector2? currentTargetTile = null;
    private float chopTimer = 0f;
    private readonly float chopInterval = 0.8f;

    private float swingAngle = 0f;
    private float swingSpeed = 5f;
    private int swingDirection = 1;
    private float maxSwingAngle = 0.5f;

    private bool isReturning = false;

    public bool IsActive { get; private set; }

    protected override SpellbookTier MinimumTier => SpellbookTier.Apprentice;

    public TreeSpirit()
        : base("spirit_tree", "Tree Spirit",
              "Summons a magical axe that chops trees.",
              30, 25, false,
              "assets/TreeSpiritAxe.png")
    {
        axeTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/TreeSpiritAxe.png");
        buffIconTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/TreeSpiritBuffIcon.png");
        iconTexture = axeTexture;
    }

    public void Unsubscribe()
    {
        if (!subscribed) return;

        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribed = false;

        // Remove buff when spell ends
        if (owner != null && owner.buffs.IsApplied(BuffId))
        {
            owner.buffs.Remove(BuffId);
        }
    }

    public override void Cast(Farmer who)
    {
        if (!CanCast(who))
            return;

        base.Cast(who);

        // --- Delay the summoning of the magical axe ---
        DelayedAction.functionAfterDelay(() =>
        {
            owner = who;
            IsActive = true;
            spellTimer = 0f;
            currentTargetTile = null;
            axePosition = who.Position + new Vector2(0, -64f);
            isReturning = false;

            // Apply the buff
            ApplyTreeSpiritBuff(who);

            if (!subscribed)
            {
                ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
                ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                subscribed = true;
            }

            Game1.playSound("axe");

        }, 1000); // 1-second delay
    }

    private void ApplyTreeSpiritBuff(Farmer who)
    {
        // Remove existing Tree Spirit buff if present
        if (who.buffs.IsApplied(BuffId))
            who.buffs.Remove(BuffId);

        // Convert spell duration to milliseconds
        int durationMs = (int)(spellDuration * 1000);

        var buff = new Buff(
            id: BuffId,
            displayName: "Tree Spirit",
            iconTexture: buffIconTexture,
            iconSheetIndex: 0,
            duration: durationMs,
            effects: new BuffEffects(),
            description: "A magical axe is chopping trees for you!"
        );

        who.buffs.Apply(buff);
        ModEntry.Instance.Monitor.Log($"Tree Spirit buff applied for {spellDuration} seconds", LogLevel.Info);
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!IsActive)
        {
            Unsubscribe();
            return;
        }

        float deltaSeconds = 1f / 60f;

        // Lifetime
        spellTimer += deltaSeconds;
        if (spellTimer >= spellDuration)
        {
            IsActive = false;
            Unsubscribe();
            return;
        }

        // Acquire a tree target
        if (currentTargetTile == null)
        {
            currentTargetTile = FindNearestTree();
            chopTimer = 0f;
        }

        if (currentTargetTile == null)
        {
            currentTargetTile = FindNearestTree();
            chopTimer = 0f;

            if (currentTargetTile == null)
                isReturning = true;
            else
                isReturning = false;
        }

        if (currentTargetTile != null)
        {
            Vector2 targetWorld = currentTargetTile.Value * Game1.tileSize + new Vector2(Game1.tileSize / 2, -32f);
            Vector2 direction = targetWorld - axePosition;

            if (direction.LengthSquared() > 8f)
            {
                direction.Normalize();
                axePosition += direction * moveSpeed * deltaSeconds;
            }
            else
            {
                chopTimer += deltaSeconds;
                if (chopTimer >= chopInterval)
                {
                    DoAxeSwingAt(currentTargetTile.Value);
                    chopTimer = 0f;
                }

                // swing animation
                swingAngle += swingDirection * swingSpeed * deltaSeconds;
                if (swingAngle > maxSwingAngle) { swingAngle = maxSwingAngle; swingDirection = -1; }
                if (swingAngle < -maxSwingAngle) { swingAngle = -maxSwingAngle; swingDirection = 1; }

                // if tree destroyed
                if (!Game1.currentLocation.terrainFeatures.ContainsKey(currentTargetTile.Value))
                {
                    currentTargetTile = null;
                    swingAngle = 0f;
                }
            }
        }
        if (isReturning && owner != null)
        {
            Vector2 direction = owner.Position - axePosition;
            const float returnThreshold = 4f;

            if (direction.LengthSquared() > returnThreshold * returnThreshold)
            {
                direction.Normalize();
                axePosition += direction * moveSpeed * deltaSeconds;
            }
            else
            {
                isReturning = false;
            }
        }
    }

    private Vector2? FindNearestTree()
    {
        if (Game1.currentLocation == null) return null;

        Vector2 axeTile = new((int)(axePosition.X / Game1.tileSize), (int)(axePosition.Y / Game1.tileSize));
        double closest = double.MaxValue;
        Vector2? closestTile = null;

        foreach (var pair in Game1.currentLocation.terrainFeatures.Pairs)
        {
            if (pair.Value is Tree tree && tree.growthStage.Value >= 5)
            {
                double dist = Vector2.DistanceSquared(pair.Key, axeTile);
                if (dist < closest)
                {
                    closest = dist;
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

        SpriteBatch b = e.SpriteBatch;
        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, axePosition);

        b.Draw(
            axeTexture,
            screenPos,
            null,
            Color.White,
            swingAngle,
            new Vector2(axeTexture.Width / 2, axeTexture.Height / 2),
            2f,
            SpriteEffects.None,
            1f
        );
    }
}