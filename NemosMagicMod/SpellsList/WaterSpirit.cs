using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;

public class WaterSpirit : Spell
{
    private Texture2D cloudTexture;
    private bool subscribed = false;
    private float wateringTimer = 0f;
    private readonly float wateringInterval = 1f; // water crops every 1 second

    private float spellDuration = 10f; // spell lasts 10 seconds
    private float spellTimer = 0f;

    // Splash tracking
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
    private readonly float splashDuration = 0.5f; // splash lasts 0.5 seconds

    public WaterSpirit()
        : base("nemo.WaterSpirit", "Water Spirit",
               "Summons a friendly rain cloud that waters nearby crops.",
               manaCost: 20, experienceGained: 40)
    {
        cloudTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/raincloud.png");
    }

    public override void Cast(Farmer who)
    {
        base.Cast(who);

        IsActive = true;
        spellTimer = 0f;

        if (!subscribed)
        {
            ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            subscribed = true;
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!IsActive)
        {
            UnsubscribeEvents();
            return;
        }

        float deltaSeconds = 1f / 60f; // UpdateTicked runs 60 times per second

        // Update spell timer
        spellTimer += deltaSeconds;
        if (spellTimer >= spellDuration)
        {
            IsActive = false;
            UnsubscribeEvents();
            return;
        }

        // Slow watering logic
        wateringTimer += deltaSeconds;
        if (wateringTimer >= wateringInterval)
        {
            WaterNearbyCrops();
            wateringTimer = 0f;
        }

        // Update splashes
        for (int i = activeSplashes.Count - 1; i >= 0; i--)
        {
            activeSplashes[i].Timer -= deltaSeconds;
            if (activeSplashes[i].Timer <= 0f)
                activeSplashes.RemoveAt(i);
        }
    }

    private void UnsubscribeEvents()
    {
        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribed = false;
    }

    private void WaterNearbyCrops()
    {
        if (Game1.currentLocation == null)
            return;

        Vector2 playerTile = Game1.player.Tile;
        int radius = 2;

        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2 tile = playerTile + new Vector2(x, y);
                if (Game1.currentLocation.terrainFeatures.TryGetValue(tile, out var feature)
                    && feature is StardewValley.TerrainFeatures.HoeDirt dirt)
                {
                    dirt.state.Value = StardewValley.TerrainFeatures.HoeDirt.watered;

                    // spawn a splash
                    Vector2 splashPos = tile * Game1.tileSize + new Vector2(Game1.tileSize / 2, Game1.tileSize / 2);
                    activeSplashes.Add(new WaterSplash(splashPos, splashDuration));
                }
            }
        }
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!IsActive || Game1.player == null)
            return;

        SpriteBatch spriteBatch = e.SpriteBatch;

        // Draw cloud above player
        float scale = 2f;
        float baseOffset = 48f;
        float floatAmplitude = 10f;
        float floatSpeed = 2f;
        float bobbing = floatAmplitude * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * floatSpeed);

        Vector2 cloudPos = Game1.GlobalToLocal(
            Game1.viewport,
            Game1.player.Position + new Vector2(0, -(baseOffset * scale + bobbing))
        );

        float cloudLayer = 1f; // on top of everything

        spriteBatch.Draw(
            cloudTexture,
            cloudPos,
            null,
            Color.White,
            0f,
            new Vector2(cloudTexture.Width / 2, cloudTexture.Height / 2),
            scale,
            SpriteEffects.None,
            cloudLayer
        );

        // Draw splashes
        foreach (var splash in activeSplashes)
        {
            Rectangle sourceRect = new Rectangle(194, 192, 16, 16); // small water splash
            spriteBatch.Draw(
                Game1.mouseCursors,
                Game1.GlobalToLocal(Game1.viewport, splash.Position),
                sourceRect,
                Color.White,
                0f,
                new Vector2(8, 8), // center
                Game1.pixelZoom, // scale
                SpriteEffects.None,
                1f // above cloud
            );
        }
    }
}
