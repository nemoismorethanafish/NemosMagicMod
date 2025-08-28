using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using static Spell;

public class TreeSpirit : Spell, IRenderable
{
    private Texture2D axeTexture;

    private bool subscribed = false;
    private float spellTimer = 0f;
    private readonly float spellDuration = 10f;

    private Vector2 axePosition;
    private Vector2 axeVelocity;
    private readonly float moveSpeed = 128f;

    public bool IsActive { get; private set; }

    // Seeking & chopping
    private Vector2? currentTargetTile = null;
    private float chopTimer = 0f;
    private readonly float chopInterval = 0.5f;

    // Swing animation
    private float swingAngle = 0f;
    private float swingSpeed = 5f;
    private int swingDirection = 1;
    private float maxSwingAngle = 0.5f;

    public TreeSpirit()
        : base("spirit_tree", "Spirit Tree",
              "Summons a magical axe that chops in a straight line.",
              30, 50)
    {
        axeTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/TreeSpiritAxe.png");
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

        foreach (var spell in ModEntry.ActiveSpells)
        {
            if (spell is IRenderable renderable)
                renderable.Unsubscribe();

            spell.IsActive = false;
        }

        IsActive = true;
        spellTimer = 0f;

        axePosition = who.Position + new Vector2(0, -64f);

        if (!subscribed)
        {
            ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            subscribed = true;
        }

        Game1.playSound("axe");
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

        // Find nearest tree if no target
        if (currentTargetTile == null)
        {
            currentTargetTile = FindNearestTree();
            chopTimer = 0f;
        }

        if (currentTargetTile != null)
        {
            Vector2 targetWorld = currentTargetTile.Value * Game1.tileSize + new Vector2(Game1.tileSize / 2);
            Vector2 direction = targetWorld - axePosition;

            if (direction.LengthSquared() > 4f)
            {
                direction.Normalize();
                axePosition += direction * moveSpeed * deltaSeconds;
            }
            else
            {
                // At tree → chop with interval
                chopTimer += deltaSeconds;
                if (chopTimer >= chopInterval)
                {
                    ChopTreeAt(currentTargetTile.Value);
                    chopTimer = 0f;
                }

                // Animate swing
                swingAngle += swingDirection * swingSpeed * deltaSeconds;
                if (swingAngle > maxSwingAngle)
                {
                    swingAngle = maxSwingAngle;
                    swingDirection = -1;
                }
                else if (swingAngle < -maxSwingAngle)
                {
                    swingAngle = -maxSwingAngle;
                    swingDirection = 1;
                }

                // If tree destroyed, clear target
                if (!Game1.currentLocation.terrainFeatures.ContainsKey(currentTargetTile.Value))
                {
                    currentTargetTile = null;
                    swingAngle = 0f;
                }
            }
        }

        if (spellTimer >= spellDuration)
        {
            IsActive = false;
            Unsubscribe();
        }
    }

    private Vector2? FindNearestTree()
    {
        if (Game1.currentLocation == null) return null;

        Vector2 axeTile = new((int)Math.Floor(axePosition.X / Game1.tileSize),
                              (int)Math.Floor(axePosition.Y / Game1.tileSize));

        double closestDist = double.MaxValue;
        Vector2? closestTile = null;

        foreach (var pair in Game1.currentLocation.terrainFeatures.Pairs)
        {
            if (pair.Value is Tree tree && tree.growthStage.Value >= 5)
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

    private void ChopTreeAt(Vector2 tile)
    {
        if (Game1.currentLocation == null) return;

        if (Game1.currentLocation.terrainFeatures.TryGetValue(tile, out var feature)
            && feature is Tree tree)
        {
            try
            {
                tree.shake(tile, tree.growthStage.Value >= 5);
                tree.health.Value -= 20;

                if (tree.health.Value <= 0)
                {
                    Game1.currentLocation.terrainFeatures.Remove(tile);
                    Game1.playSound("treethud");

                    int woodAmount = 0;
                    int woodIndex = 388;

                    switch (tree.treeType.Value)
                    {
                        case Tree.bushyTree:
                        case Tree.leafyTree:
                        case Tree.pineTree:
                            woodAmount = Game1.random.Next(5, 11);
                            break;
                        case Tree.palmTree:
                            woodAmount = Game1.random.Next(3, 8);
                            break;
                        case Tree.mushroomTree:
                            woodIndex = 420;
                            woodAmount = Game1.random.Next(2, 5);
                            break;
                        case Tree.mahoganyTree:
                            woodIndex = 709;
                            woodAmount = Game1.random.Next(8, 12);
                            break;
                    }

                    if (woodAmount > 0)
                    {
                        Game1.createMultipleObjectDebris(
                            $"(O){woodIndex}",
                            (int)tile.X,
                            (int)tile.Y,
                            woodAmount,
                            Game1.currentLocation
                        );
                    }

                    // Sap
                    Game1.createDebris(92, (int)tile.X, (int)tile.Y, Game1.random.Next(1, 4), Game1.currentLocation);

                    // Seeds (25% chance)
                    int seedIndex = -1;
                    switch (tree.treeType.Value)
                    {
                        case Tree.leafyTree: seedIndex = 309; break;
                        case Tree.bushyTree: seedIndex = 310; break;
                        case Tree.pineTree: seedIndex = 311; break;
                        case Tree.mahoganyTree: seedIndex = 292; break;
                    }

                    if (seedIndex != -1 && Game1.random.NextDouble() < 0.25)
                    {
                        Game1.createDebris(seedIndex, (int)tile.X, (int)tile.Y, 1, Game1.currentLocation);
                    }
                }
                else
                {
                    Game1.playSound("axchop");
                }
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.Log($"Failed to chop tree at {tile}: {ex}", StardewModdingAPI.LogLevel.Warn);
            }
        }
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!IsActive) return;

        SpriteBatch spriteBatch = e.SpriteBatch;
        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, axePosition);

        spriteBatch.Draw(
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
