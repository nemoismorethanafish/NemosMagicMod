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

    public override void Cast(Farmer who)
    {
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

        if (!subscribed)
        {
            ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            subscribed = true;
        }

        Game1.playSound("rain");
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

                    // Add splash
                    Vector2 splashPos = tile * Game1.tileSize + new Vector2(Game1.tileSize / 2, Game1.tileSize / 2);
                    activeSplashes.Add(new WaterSplash(splashPos, splashDuration));
                }
            }
        }
    }

    public void Unsubscribe()
    {
        if (!subscribed) return;

        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribed = false;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!IsActive || Game1.player == null) return;

        SpriteBatch spriteBatch = e.SpriteBatch;

        // Floating cloud animation
        float floatAmplitude = 10f;
        float floatSpeed = 2f;
        float bobbing = floatAmplitude * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * floatSpeed);

        float baseOffset = 48f;
        float scale = 2f;

        Vector2 worldPos = Game1.player.Position + new Vector2(0, -(baseOffset * scale + bobbing));
        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, worldPos);

        spriteBatch.Draw(
            cloudTexture,
            screenPos,
            null,
            Color.White,
            0f,
            new Vector2(cloudTexture.Width / 2, cloudTexture.Height / 2),
            scale,
            SpriteEffects.None,
            1f
        );

        // Draw splashes
        foreach (var splash in activeSplashes)
        {
            spriteBatch.Draw(
                splashTexture,
                Game1.GlobalToLocal(Game1.viewport, splash.Position),
                null, // use the entire texture
                Color.White,
                0f,
                new Vector2(splashTexture.Width / 2, splashTexture.Height / 2), // center
                Game1.pixelZoom * 0.25f, // scale down to 1/4
                SpriteEffects.None,
                1f
            );
        }
    }
}
