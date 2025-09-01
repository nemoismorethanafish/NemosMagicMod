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
using System.Linq;
using static Spell;

namespace NemosMagicMod.Spells
{
    public class SeaSpirit : Spell, IRenderable
    {
        private const float BaseBubbleDuration = 30f;
        private const int BubbleRadius = 5;
        private const float BubbleRiseSpeed = 60f;
        private const float BubbleFadeSpeed = 0.6f;

        // Tier-based scaling factors
        private readonly Dictionary<SpellbookTier, float> durationMultipliers = new()
        {
            { SpellbookTier.Novice, 1.0f },
            { SpellbookTier.Apprentice, 1.5f },
            { SpellbookTier.Adept, 2.0f },
            { SpellbookTier.Master, 2.5f }
        };

        private readonly Dictionary<SpellbookTier, int> bubbleLocationCounts = new()
        {
            { SpellbookTier.Novice, 1 },
            { SpellbookTier.Apprentice, 2 },
            { SpellbookTier.Adept, 3 },
            { SpellbookTier.Master, 4 }
        };

        private Texture2D bubbleTexture;
        private bool subscribedDraw = false;
        public List<Bubble> bubbles = new();
        public IReadOnlyList<Bubble> Bubbles => bubbles.AsReadOnly();

        private float bubbleTimer = 0f;
        private float currentSpellDuration = BaseBubbleDuration;

        // Override minimum tier requirement
        protected override SpellbookTier MinimumTier => SpellbookTier.Apprentice;

        public SeaSpirit()
            : base("nemo.SeaSpirit", "Sea Spirit", "Summons magical bubbles that increase fishing bite rates.", 30, 25, false, "assets/bubbles.png")
        {
            bubbleTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/bubbles.png");
            ApplyHarmonyPatches();
            iconTexture = bubbleTexture;
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
                DriftSpeed = ((float)Game1.random.NextDouble() - 0.5f) * 20f;
                StartDelay = (float)Game1.random.NextDouble() * 1f;
                Active = false;
                Scale = 0.8f + (float)Game1.random.NextDouble() * 0.4f;
                ScaleDirection = 1f;
            }

            public void ResetToFreshValues()
            {
                XOffset = Game1.random.Next(-4, 5);
                PixelPos = Tile * Game1.tileSize + new Vector2(XOffset, 0);
                Opacity = 1f;
                Active = false;
                StartDelay = (float)Game1.random.NextDouble() * 1f;

                RiseSpeed = BubbleRiseSpeed * (0.8f + (float)Game1.random.NextDouble() * 0.4f);
                DriftSpeed = ((float)Game1.random.NextDouble() - 0.5f) * 20f;
                Scale = 0.8f + (float)Game1.random.NextDouble() * 0.4f;
                ScaleDirection = 1f;
            }
        }

        private float GetTierAdjustedDuration(Farmer who)
        {
            var currentTier = GetCurrentSpellbookTier(who);
            var multiplier = durationMultipliers.GetValueOrDefault(currentTier, 1.0f);
            return BaseBubbleDuration * multiplier;
        }

        private int GetTierAdjustedBubbleLocations(Farmer who)
        {
            var currentTier = GetCurrentSpellbookTier(who);
            return bubbleLocationCounts.GetValueOrDefault(currentTier, 1);
        }

        public override void Cast(Farmer who)
        {
            if (!CanCast(who))
                return;

            base.Cast(who);

            var existingSeaSpirit = ModEntry.ActiveSpells.OfType<SeaSpirit>().FirstOrDefault();
            var currentTier = GetCurrentSpellbookTier(who);
            var extraBubbleCount = GetTierAdjustedBubbleLocations(who);
            var extraDuration = GetTierAdjustedDuration(who);

            if (existingSeaSpirit != null)
            {
                existingSeaSpirit.AddBubbleLocations(who, currentTier, extraBubbleCount);
                existingSeaSpirit.ExtendDuration(extraDuration);

                Game1.addHUDMessage(new HUDMessage(
                    $"Sea Spirit refreshed: +{extraBubbleCount} bubbles, +{(int)extraDuration}s duration!", 2));
            }
            else
            {
                currentSpellDuration = extraDuration;
                bubbleTimer = currentSpellDuration;

                var durationSeconds = (int)currentSpellDuration;
                Game1.addHUDMessage(new HUDMessage(
                    $"Sea Spirit: {durationSeconds}s, {extraBubbleCount} location(s) ({currentTier} tier)", 2));

                ModEntry.RegisterActiveSpell(this);
                StartSpellEffects(who, currentTier, extraBubbleCount);
            }
        }

        public void AddBubbleLocations(Farmer who, SpellbookTier tier, int count)
        {
            var newTiles = PickBubbleTiles(who, count);
            foreach (var tile in newTiles)
                SpawnBubble(tile);
        }

        public void ExtendDuration(float extraSeconds)
        {
            bubbleTimer += extraSeconds;
            currentSpellDuration += extraSeconds;
        }

        private List<Vector2> PickBubbleTiles(Farmer who, int count)
        {
            var results = new List<Vector2>();
            var location = who.currentLocation;
            Vector2 center = who.Tile;

            int tries = 0;
            while (results.Count < count && tries < 100)
            {
                tries++;
                int dx = Game1.random.Next(-BubbleRadius, BubbleRadius + 1);
                int dy = Game1.random.Next(-BubbleRadius, BubbleRadius + 1);
                Vector2 tile = center + new Vector2(dx, dy);

                if (location.isOpenWater(tile) && !results.Contains(tile))
                    results.Add(tile);
            }

            return results;
        }

        private void SpawnBubble(Vector2 tile)
        {
            var bubble = new Bubble(tile);
            bubbles.Add(bubble);
        }

        private void StartSpellEffects(Farmer who, SpellbookTier tier, int bubbleCount)
        {
            bubbleTimer = currentSpellDuration;

            var tiles = PickBubbleTiles(who, bubbleCount);
            foreach (var tile in tiles)
                SpawnBubble(tile);

            SubscribeDraw();
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
                    if (b.PixelPos.X > b.Tile.X * Game1.tileSize + maxOffset ||
                        b.PixelPos.X < b.Tile.X * Game1.tileSize - maxOffset)
                    {
                        b.DriftSpeed = -b.DriftSpeed;
                    }

                    b.Opacity -= BubbleFadeSpeed * dt;

                    if (b.Opacity <= 0f)
                        b.ResetToFreshValues();
                }

                if (bubbleTimer <= 0f)
                {
                    var currentTier = GetCurrentSpellbookTier(who);
                    Game1.addHUDMessage(new HUDMessage($"Sea Spirit faded ({currentTier} tier)", 1));
                    Unsubscribe();
                }
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
