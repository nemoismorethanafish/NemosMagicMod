using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Spell;

namespace NemosMagicMod.Spells
{
    public class SeaSpirit : Spell, IRenderable
    {
        private const float BubbleDuration = 30f;
        private const int BubbleRadius = 5;
        private const float BubbleRiseSpeed = 60f;
        private const float BubbleFadeSpeed = 0.6f;

        private Texture2D bubbleTexture;
        private bool subscribedDraw = false;
        public List<Bubble> bubbles = new();
        public IReadOnlyList<Bubble> Bubbles => bubbles.AsReadOnly();

        private float bubbleTimer = 0f;

        public SeaSpirit()
            : base("nemo.SeaSpirit", "Sea Spirit", "Summons magical bubbles that increase fishing bite rates.", 25)
        {
            bubbleTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/bubbles.png");
            ApplyHarmonyPatches();
        }

        public bool IsActive => bubbleTimer > 0f;

        protected override bool FreezePlayerDuringCast => false;

        public class Bubble
        {
            public Vector2 Tile;
            public Vector2 PixelPos;
            public float Opacity;
            public float XOffset;
            public float RiseSpeed;
            public float DriftSpeed;
            public float StartDelay;
            public bool Active;
            public float Scale;
            public float ScaleDirection;

            public Bubble(Vector2 tile)
            {
                Tile = tile;
                XOffset = Game1.random.Next(-4, 5);
                PixelPos = tile * Game1.tileSize + new Vector2(XOffset, 0);
                Opacity = 0f;
                RiseSpeed = BubbleRiseSpeed * (0.8f + (float)Game1.random.NextDouble() * 0.4f);
                DriftSpeed = ((float)Game1.random.NextDouble() - 0.5f) * 20f; // ±10 pixels/sec
                StartDelay = (float)Game1.random.NextDouble() * 1f;
                Active = false;
                Scale = 0.8f + (float)Game1.random.NextDouble() * 0.4f;
                ScaleDirection = 1f;
            }
        }

        public override void Cast(Farmer who)
        {
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            base.Cast(who);
            bubbles.Clear();

            GameLocation location = who.currentLocation;
            Vector2 playerTile = new((int)(who.Position.X / Game1.tileSize), (int)(who.Position.Y / Game1.tileSize));

            List<Vector2> validTiles = new();
            for (int x = -BubbleRadius; x <= BubbleRadius; x++)
            {
                for (int y = -BubbleRadius; y <= BubbleRadius; y++)
                {
                    Vector2 tile = playerTile + new Vector2(x, y);
                    if (location.isTileOnMap(tile) && location.isOpenWater(tile))
                        validTiles.Add(tile);
                }
            }

            int numBubbles = 8; // number of simultaneous bubbles
            if (validTiles.Count > 0)
            {
                Vector2 chosenTile = validTiles[Game1.random.Next(validTiles.Count)];
                for (int i = 0; i < numBubbles; i++)
                    bubbles.Add(new Bubble(chosenTile));
            }

            bubbleTimer = BubbleDuration;
            SubscribeDraw();

            Game1.showGlobalMessage($"Sea Spirit activated! Fishing bite rate increased for {BubbleDuration} seconds.");
        }

        public override void Update(GameTime gameTime, Farmer who)
        {
            if (bubbleTimer > 0f)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                bubbleTimer -= dt;

                foreach (var b in bubbles)
                {
                    if (!b.Active)
                    {
                        b.StartDelay -= dt;
                        if (b.StartDelay <= 0f)
                            b.Active = true;
                        else
                            continue;
                    }

                    b.PixelPos.Y -= b.RiseSpeed * dt;
                    b.PixelPos.X += b.DriftSpeed * dt;

                    float maxOffset = Game1.tileSize / 2f;
                    if (b.PixelPos.X > b.Tile.X * Game1.tileSize + maxOffset || b.PixelPos.X < b.Tile.X * Game1.tileSize - maxOffset)
                        b.DriftSpeed *= -1f;

                    b.Opacity -= BubbleFadeSpeed * dt;

                    if (b.Opacity <= 0f)
                    {
                        b.PixelPos = b.Tile * Game1.tileSize + new Vector2(Game1.random.Next(-4, 5), 0);
                        b.Opacity = 1f;
                        b.Active = false;
                        b.StartDelay = (float)Game1.random.NextDouble() * 1f;
                    }
                }

                if (bubbleTimer <= 0f)
                    Unsubscribe();
            }

            HandleFishingBiteRate();
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
                bubbles.Clear();
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!IsActive) return;

            foreach (var b in bubbles)
            {
                if (!b.Active) continue;

                e.SpriteBatch.Draw(
                    bubbleTexture,
                    Game1.GlobalToLocal(Game1.viewport, b.PixelPos),
                    null,
                    Color.White * b.Opacity,
                    0f,
                    Vector2.Zero,
                    b.Scale,
                    SpriteEffects.None,
                    1f
                );
            }
        }

        private void ApplyHarmonyPatches()
        {
            var harmony = new Harmony("nemo.SeaSpiritFishingPatch");
            harmony.Patch(
                original: AccessTools.Method(typeof(FishingRod), nameof(FishingRod.DoFunction)),
                postfix: new HarmonyMethod(typeof(SeaSpirit), nameof(FishingRod_DoFunction_Postfix))
            );
        }

        public static void FishingRod_DoFunction_Postfix(FishingRod __instance, GameLocation location, int x, int y, int power, Farmer who)
        {
            var seaSpirit = (SeaSpirit)SpellRegistry.SeaSpirit;

            if (seaSpirit.IsActive && __instance.isFishing && __instance.timeUntilFishingBite > 0)
            {
                Vector2 fishingTile = new Vector2(x / Game1.tileSize, y / Game1.tileSize);
                bool nearBubble = false;

                foreach (var b in seaSpirit.Bubbles)
                {
                    if (!b.Active) continue;
                    if (Vector2.Distance(fishingTile, b.Tile) <= 1f)
                    {
                        nearBubble = true;
                        break;
                    }
                }

                if (nearBubble)
                {
                    int original = (int)__instance.timeUntilFishingBite;
                    int boosted = Math.Max(500, original / 3);
                    __instance.timeUntilFishingBite = boosted;

                    ModEntry.Instance.Monitor.Log(
                        $"Sea Spirit applied! Original timer: {original}, boosted timer: {boosted}",
                        LogLevel.Debug
                    );
                }
            }
        }

        private void HandleFishingBiteRate()
        {
            // Optional: any additional per-frame fishing logic can go here
        }
    }

    public static class LocationExtensions
    {
        public static bool isOpenWater(this GameLocation loc, Vector2 tile)
        {
            if (!loc.isTileOnMap(tile)) return false;
            if (loc.terrainFeatures.TryGetValue(tile, out var feature))
                if (feature is Flooring) return false;

            return loc.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") != null;
        }
    }
}
