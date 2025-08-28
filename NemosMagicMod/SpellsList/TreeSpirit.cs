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
    private readonly float axeSpeed = 64f;

    public bool IsActive { get; private set; }

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

        // Clear other active spells
        foreach (var spell in ModEntry.ActiveSpells)
        {
            if (spell is IRenderable renderable)
                renderable.Unsubscribe();

            spell.IsActive = false;
        }

        IsActive = true;
        spellTimer = 0f;

        // Spawn axe above the player
        axePosition = who.Position + new Vector2(0, -64f);

        // Move forward based on facing direction
        switch (who.FacingDirection)
        {
            case 0: axeVelocity = new Vector2(0, -axeSpeed); break; // up
            case 1: axeVelocity = new Vector2(axeSpeed, 0); break;   // right
            case 2: axeVelocity = new Vector2(0, axeSpeed); break;   // down
            case 3: axeVelocity = new Vector2(-axeSpeed, 0); break;  // left
            default: axeVelocity = Vector2.Zero; break;
        }

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

        // Move axe forward
        axePosition += axeVelocity * deltaSeconds;

        // Chop any nearby trees
        ChopNearbyTrees();

        // Expire spell
        if (spellTimer >= spellDuration)
        {
            IsActive = false;
            Unsubscribe();
        }
    }

    private void ChopNearbyTrees()
    {
        if (Game1.currentLocation == null) return;

        int tileX = (int)Math.Floor(axePosition.X / Game1.tileSize);
        int tileY = (int)Math.Floor(axePosition.Y / Game1.tileSize);
        Vector2 tile = new(tileX, tileY);

        if (Game1.currentLocation.terrainFeatures.TryGetValue(tile, out var feature)
            && feature is StardewValley.TerrainFeatures.Tree tree)
        {
            try
            {
                // Shake tree for visual feedback
                tree.shake(tile, tree.growthStage.Value >= 5);

                // Reduce tree health
                tree.health.Value -= 20;

                // If tree is destroyed, remove it and spawn drops
                if (tree.health.Value <= 0)
                {
                    Game1.currentLocation.terrainFeatures.Remove(tile);
                    Game1.playSound("treethud");

                    int woodAmount = 0;
                    int woodIndex = 388; // Wood

                    switch (tree.treeType.Value)
                    {
                        case Tree.bushyTree:     // Maple
                        case Tree.leafyTree:     // Oak
                        case Tree.pineTree:      // Pine
                            woodAmount = Game1.random.Next(5, 11);
                            break;

                        case Tree.palmTree:      // Palm
                            woodAmount = Game1.random.Next(3, 8);
                            break;

                        case Tree.mushroomTree:  // Mushroom tree
                            woodIndex = 420; // Red Mushroom
                            woodAmount = Game1.random.Next(2, 5);
                            break;

                        case Tree.mahoganyTree:  // Mahogany
                            woodIndex = 709; // Hardwood
                            woodAmount = Game1.random.Next(8, 12);
                            break;
                    }

                    if (woodAmount > 0)
                    {
                        Game1.createMultipleObjectDebris(
                            $"(O){woodIndex}", // qualified object ID
                            (int)tile.X,
                            (int)tile.Y,
                            woodAmount,
                            Game1.currentLocation
                        );
                    }


                    // Sap (92)
                    Game1.createDebris(
                        92,
                        (int)tile.X,
                        (int)tile.Y,
                        Game1.random.Next(1, 4),
                        Game1.currentLocation
                    );

                    // Seeds (25% chance)
                    int seedIndex = -1;
                    switch (tree.treeType.Value)
                    {
                        case Tree.leafyTree: seedIndex = 309; break; // Acorn
                        case Tree.bushyTree: seedIndex = 310; break; // Maple seed
                        case Tree.pineTree: seedIndex = 311; break; // Pine cone
                        case Tree.mahoganyTree: seedIndex = 292; break; // Mahogany seed
                    }

                    if (seedIndex != -1 && Game1.random.NextDouble() < 0.25)
                    {
                        Game1.createDebris(
                            seedIndex,
                            (int)tile.X,
                            (int)tile.Y,
                            1,
                            Game1.currentLocation
                        );
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
            0f,
            new Vector2(axeTexture.Width / 2, axeTexture.Height / 2),
            2f,
            SpriteEffects.None,
            1f
        );
    }
}
