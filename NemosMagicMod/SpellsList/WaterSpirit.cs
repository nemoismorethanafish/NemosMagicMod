using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using static Spell;

public class WaterSpirit : Spell, IRenderable
{
    private Texture2D cloudTexture;
    private Texture2D splashTexture;

    private bool subscribed = false;

    private float spellTimer = 0f;
    private readonly float spellDuration = 10f;

    private float wateringTimer = 0f;
    private readonly float wateringInterval = 1f;

    public void Unsubscribe()
    {
        if (!subscribed) return;

        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribed = false;
    }

    private class WaterSplash
    {
        public Vector2 Position;
        public float Timer;

        public WaterSplash(Vector2 position, float duration)
        {
            Position = position;
            Timer = duration;
        }
    }

    private List<WaterSplash> activeSplashes = new();
    private readonly float splashDuration = 0.5f;

    public bool IsActive { get; private set; }

    public WaterSpirit()
        : base("water_spirit", "Water Spirit",
              "Summons a friendly rain cloud that waters nearby crops.",
              20, 40)
    {
        {
            cloudTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/raincloud.png");
            splashTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/WaterSplash.png");
        }
    }

    private Vector2 cloudPosition; // current world position of the cloud
    private Vector2 cloudVelocity; // movement per second
    private readonly float cloudSpeed = 64f; // pixels per second

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
        wateringTimer = 0f;
        activeSplashes.Clear();

        // Initialize cloud position above the player
        cloudPosition = who.Position + new Vector2(0, -64f);

        // Determine velocity based on player's facing direction
        switch (who.FacingDirection)
        {
            case 0: cloudVelocity = new Vector2(0, -cloudSpeed); break; // up
            case 1: cloudVelocity = new Vector2(cloudSpeed, 0); break;   // right
            case 2: cloudVelocity = new Vector2(0, cloudSpeed); break;   // down
            case 3: cloudVelocity = new Vector2(-cloudSpeed, 0); break;  // left
            default: cloudVelocity = new Vector2(0, 0); break;
        }

        if (!subscribed)
        {
            ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            subscribed = true;
        }

        Game1.playSound("wateringCan");
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

        // Move cloud
        cloudPosition += cloudVelocity * deltaSeconds;

        // Water crops every interval
        wateringTimer += deltaSeconds;
        if (wateringTimer >= wateringInterval)
        {
            WaterNearbyCrops();
            wateringTimer = 0f;
        }

        // Update splash timers
        for (int i = activeSplashes.Count - 1; i >= 0; i--)
        {
            activeSplashes[i].Timer -= deltaSeconds;
            if (activeSplashes[i].Timer <= 0f)
                activeSplashes.RemoveAt(i);
        }

        // Expire spell
        if (spellTimer >= spellDuration)
        {
            IsActive = false;
            Unsubscribe();
        }
    }

    private void WaterNearbyCrops()
    {
        if (Game1.currentLocation == null) return;

        // Convert cloud position to tile coordinates (integers!)
        int cloudTileX = (int)Math.Floor(cloudPosition.X / Game1.tileSize);
        int cloudTileY = (int)Math.Floor(cloudPosition.Y / Game1.tileSize);
        Vector2 cloudTile = new Vector2(cloudTileX, cloudTileY);

        int radius = 1;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2 tile = cloudTile + new Vector2(x, y);
                if (Game1.currentLocation.terrainFeatures.TryGetValue(tile, out var feature)
                    && feature is StardewValley.TerrainFeatures.HoeDirt dirt)
                {
                    dirt.state.Value = StardewValley.TerrainFeatures.HoeDirt.watered;

                    // Add splash
                    Vector2 splashPos = tile * Game1.tileSize + new Vector2(Game1.tileSize / 2, Game1.tileSize / 2);
                    activeSplashes.Add(new WaterSplash(splashPos, splashDuration));

                    Game1.playSound("wateringCan");
                }
            }
        }
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!IsActive) return;
        SpriteBatch spriteBatch = e.SpriteBatch;

        // Draw cloud at current position
        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, cloudPosition);
        spriteBatch.Draw(
            cloudTexture,
            screenPos,
            null,
            Color.White,
            0f,
            new Vector2(cloudTexture.Width / 2, cloudTexture.Height / 2),
            2f,
            SpriteEffects.None,
            1f
        );

        // Draw splashes
        foreach (var splash in activeSplashes)
        {
            spriteBatch.Draw(
                splashTexture,
                Game1.GlobalToLocal(Game1.viewport, splash.Position),
                null,
                Color.White,
                0f,
                new Vector2(splashTexture.Width / 2, splashTexture.Height / 2),
                Game1.pixelZoom * 0.25f,
                SpriteEffects.None,
                1f
            );
        }
    }
}