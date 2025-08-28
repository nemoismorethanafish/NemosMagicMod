using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using System;
using static Spell;

public class EarthSpirit : Spell, IRenderable
{
    private Texture2D pickaxeTexture;

    private bool subscribed = false;
    private Vector2 toolPosition;
    private readonly float moveSpeed = 128f;

    private float spellTimer = 0f;
    private readonly float spellDuration = 10f;
    private readonly float hoverHeight = 32f;

    public bool IsActive { get; private set; }

    private Vector2? currentTargetTile = null;
    private float mineTimer = 0f;
    private readonly float mineInterval = 0.5f;

    private float swingAngle = 0f;
    private float swingSpeed = 5f;
    private int swingDirection = 1;
    private float maxSwingAngle = 0.5f;

    private bool isReturning = false;
    private Farmer owner;

    public EarthSpirit()
        : base("nemo.EarthSpirit", "Earth Spirit",
              "Summons a magical pickaxe that mines rocks.",
              30, 50)
    {
        pickaxeTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/EarthSpiritPickaxe.png");
    }

    public void Unsubscribe()
    {
        if (!subscribed) return;

        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribed = false;
    }

    public override void Cast(Farmer who)
    {
        if (!ManaManager.HasEnoughMana(ManaCost))
        {
            Game1.showRedMessage("Not enough mana!");
            return;
        }

        base.Cast(who);

        owner = who;

        foreach (var spell in ModEntry.ActiveSpells)
        {
            if (spell is IRenderable renderable)
                renderable.Unsubscribe();

            spell.IsActive = false;
        }

        IsActive = true;
        spellTimer = 0f;
        currentTargetTile = null;
        isReturning = false;
        toolPosition = who.Position + new Vector2(0, -64f);

        if (!subscribed)
        {
            ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            subscribed = true;
        }

        Game1.playSound("hammer");
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

            if (direction.LengthSquared() > 4f)
            {
                direction.Normalize();
                toolPosition += direction * moveSpeed * deltaSeconds;
            }
            else
            {
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

        // Follow player
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
        if (Game1.currentLocation == null) return;

        if (!Game1.currentLocation.objects.TryGetValue(tile, out var obj) || !IsMineableRock(obj))
            return;

        try
        {
            // Spawn drops manually
            int dropIndex = 0;
            int minCount = 1, maxCount = 3;

            switch (obj.Name)
            {
                case string s when s.Contains("Stone"):
                    dropIndex = 390; // stone
                    minCount = 1; maxCount = 3;
                    break;
                case string s when s.Contains("Iron Ore"):
                    dropIndex = 380; minCount = 1; maxCount = 2; break;
                case string s when s.Contains("Gold Ore"):
                    dropIndex = 384; minCount = 1; maxCount = 2; break;
                case string s when s.Contains("Geode"):
                    dropIndex = 535; minCount = 1; maxCount = 1; break;
                default:
                    dropIndex = 390; minCount = 1; maxCount = 2; break;
            }

            int amount = Game1.random.Next(minCount, maxCount + 1);
            Game1.createMultipleObjectDebris($"(O){dropIndex}", (int)tile.X, (int)tile.Y, amount, Game1.currentLocation);

            // Spawn particle chips
            SpawnStoneChips(tile);

            // Play mining sound
            Game1.playSound("hammer");

            // Remove rock from map
            Game1.currentLocation.objects.Remove(tile);
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

    private void SpawnStoneChips(Vector2 tile)
    {
        if (Game1.currentLocation == null) return;

        int count = Game1.random.Next(3, 7);
        for (int i = 0; i < count; i++)
        {
            var sprite = new TemporaryAnimatedSprite(
                10,
                tile * Game1.tileSize + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-16, 16)),
                Color.Gray,
                4,
                false,
                0.1f
            );
            sprite.scale = 1f;
            sprite.layerDepth = 1f;
            Game1.currentLocation.temporarySprites.Add(sprite);
        }
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
