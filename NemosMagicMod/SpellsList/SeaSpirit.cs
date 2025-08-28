using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Spell;

namespace NemosMagicMod.Spells
{
    public class SeaSpirit : Spell, IRenderable
    {
        private const float BubbleDuration = 30f; // increased duration
        private const int BubbleRadius = 5;
        private float bubbleTimer = 0f;
        private List<Vector2> bubbleTiles = new();
        private Texture2D bubbleTexture;
        private bool subscribedDraw = false;

        public IReadOnlyList<Vector2> BubbleTiles => bubbleTiles;
        public bool IsActive => bubbleTimer > 0f && bubbleTiles.Count > 0;

        public SeaSpirit()
            : base("nemo.SeaSpirit", "Sea Spirit", "Summons magical bubbles that increase fishing bite rates nearby.", 25)
        {
            bubbleTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/bubbles.png");
        }

        protected override bool FreezePlayerDuringCast => false;

        public override void Cast(Farmer who)
        {
            base.Cast(who);
            bubbleTiles.Clear();
            GameLocation location = who.currentLocation;

            Vector2 playerTile = new Vector2((int)(who.Position.X / 64f), (int)(who.Position.Y / 64f));

            for (int x = -BubbleRadius; x <= BubbleRadius; x++)
            {
                for (int y = -BubbleRadius; y <= BubbleRadius; y++)
                {
                    Vector2 tile = playerTile + new Vector2(x, y);
                    if (location.isTileOnMap(tile) && location.isOpenWater(tile))
                        bubbleTiles.Add(tile);
                }
            }

            bubbleTimer = BubbleDuration;
            SubscribeDraw();

            Game1.showGlobalMessage($"Sea Spirit activated! Bubbles will boost fishing for {BubbleDuration} seconds.");
        }

        public override void Update(GameTime gameTime, Farmer who)
        {
            if (bubbleTimer > 0f)
            {
                bubbleTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (bubbleTimer <= 0f)
                    Unsubscribe();
            }
        }

        private void SubscribeDraw()
        {
            if (!subscribedDraw)
            {
                ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
                subscribedDraw = true;
            }
        }

        public void Unsubscribe()
        {
            if (subscribedDraw)
            {
                ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
                subscribedDraw = false;
                bubbleTiles.Clear();
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!IsActive) return;

            foreach (var tile in bubbleTiles)
            {
                Vector2 pixelPos = Game1.GlobalToLocal(Game1.viewport, tile * 64f);
                e.SpriteBatch.Draw(bubbleTexture, pixelPos, null, Color.White * 0.8f, 0f, Vector2.Zero, 1f, SpriteEffects.None, 1f);
            }
        }

        /// <summary>
        /// Bite rate multiplier for a cast tile.
        /// </summary>
        public float GetFishingBiteMultiplier(Vector2 castTile)
        {
            return (IsActive && bubbleTiles.Contains(castTile)) ? 3f : 1f; // +200%
        }
    }

    public static class LocationExtensions
    {
        public static bool isOpenWater(this GameLocation loc, Vector2 tile)
        {
            if (!loc.isTileOnMap(tile)) return false;
            if (loc.terrainFeatures.TryGetValue(tile, out TerrainFeature feature))
                if (feature is Flooring) return false;

            return loc.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null;
        }
    }

    public static class SeaSpiritFishingIntegration
    {
        private static readonly string SeaSpiritKey = "NemosMagicMod.SeaSpiritApplied";

        public static void RegisterEvents(IModHelper helper)
        {
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private static void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            Farmer who = Game1.player;
            if (who == null || !who.UsingTool || !(who.CurrentTool is FishingRod rod))
                return;

            // Only while waiting for a bite
            if (rod.inUse() && !rod.isFishing && rod.isCasting)
            {
                // Grab the field for bite timer
                var field = typeof(FishingRod).GetField("timeUntilFishingBite",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (field == null) return;

                int currentTimer = (int)field.GetValue(rod);

                // Where the bobber actually landed
                Vector2 bobberTile = rod.bobber.Value / 64f;

                if (SpellRegistry.SeaSpirit is SeaSpirit sea && sea.IsActive && sea.BubbleTiles.Contains(bobberTile))
                {
                    // Make fish bite 3x faster (reduce timer by 66%)
                    int boostedTimer = Math.Max(500, (int)(currentTimer / 3f));

                    if (boostedTimer < currentTimer)
                        field.SetValue(rod, boostedTimer);
                }
            }
        }
    }
}
