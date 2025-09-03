using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buffs;
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
        private const string BuffId = "NemosMagicMod_SeaSpirit"; // <-- Add this
        private Texture2D buffIconTexture;


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
            { SpellbookTier.Apprentice, 3 },
            { SpellbookTier.Adept, 6 },
            { SpellbookTier.Master, 9 }
        };

        private Texture2D bubbleTexture;
        private bool subscribedDraw = false;
        public List<Bubble> bubbles = new();
        public IReadOnlyList<Bubble> Bubbles => bubbles.AsReadOnly();

        private float bubbleTimer = 0f;
        private float currentSpellDuration = BaseBubbleDuration;

        // Override minimum tier requirement
        protected override SpellbookTier MinimumTier => SpellbookTier.Apprentice;

        // Extra fishing luck multiplier at Master tier
        private readonly Dictionary<SpellbookTier, double> fishingLuckMultipliers = new()
{
    { SpellbookTier.Novice, 1.0 },
    { SpellbookTier.Apprentice, 1.0 },
    { SpellbookTier.Adept, 1.0 },
    { SpellbookTier.Master, 1.5 } // 50% better luck near bubbles
};

        private void ApplySeaSpiritBuff(Farmer who)
        {
            if (who.buffs.IsApplied(BuffId))
                who.buffs.Remove(BuffId);

            int durationMs = (int)(bubbleTimer * 1000);
            var tier = GetCurrentSpellbookTier(who);

            var buff = new Buff(
                id: BuffId,
                displayName: "Sea Spirit",
                iconTexture: buffIconTexture,
                iconSheetIndex: 0,
                duration: durationMs,
                effects: new BuffEffects(), // Visual only
                description: $"Magical bubbles increase fishing bite rates! ({tier} tier)"
            );

            who.buffs.Apply(buff);
            ModEntry.Instance.Monitor.Log($"Sea Spirit buff applied for {bubbleTimer:F1}s ({tier} tier)", LogLevel.Info);
        }

        private void ApplyBuffRemoval(Farmer who)
        {
            if (who.buffs.IsApplied(BuffId))
                who.buffs.Remove(BuffId);

            Unsubscribe();

            // Reset bubbles
            bubbles.Clear();
            bubbleTimer = 0f;
        }




        public SeaSpirit()
            : base("nemo.SeaSpirit", "Sea Spirit", "Summons magical bubbles that increase fishing bite rates.", 30, 25, false, "assets/BubblesBuffIcon.png")
        {
            try
            {
                bubbleTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/bubbles.png");
                buffIconTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/BubblesBuffIcon.png");
                iconTexture = bubbleTexture;
            }
            catch (Exception ex)
            {
                ModEntry.Instance.Monitor.Log($"Failed to load SeaSpirit textures: {ex}", LogLevel.Error);
                buffIconTexture = bubbleTexture;
            }

            // Only apply patches once using a static flag
            ApplyHarmonyPatchesOnce();
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
            public float Scale;
            public float ScaleDirection;

            public Bubble(Vector2 tile)
            {
                Tile = tile;
                InitializeValues();
            }

            private void InitializeValues()
            {
                // Fully random initial values
                XOffset = Game1.random.Next(-4, 5);
                PixelPos = Tile * Game1.tileSize + new Vector2(XOffset, 0);
                Opacity = 1f;
                RiseSpeed = SeaSpirit.BubbleRiseSpeed * (0.8f + (float)Game1.random.NextDouble() * 0.4f);
                DriftSpeed = ((float)Game1.random.NextDouble() - 0.5f) * 20f;
                Scale = 0.8f + (float)Game1.random.NextDouble() * 0.4f;
                ScaleDirection = 1f;
            }

            public void ResetToFreshValues(bool keepRiseSpeed = true)
            {
                // Store the original rise speed before resetting anything
                float preservedRiseSpeed = RiseSpeed;

                XOffset = Game1.random.Next(-4, 5);
                PixelPos = Tile * Game1.tileSize + new Vector2(XOffset, 0);
                Opacity = 1f;
                DriftSpeed = ((float)Game1.random.NextDouble() - 0.5f) * 20f;
                Scale = 0.8f + (float)Game1.random.NextDouble() * 0.4f;
                ScaleDirection = 1f;

                // Only regenerate RiseSpeed if we're not keeping it
                if (keepRiseSpeed)
                {
                    RiseSpeed = preservedRiseSpeed;
                }
                else
                {
                    RiseSpeed = SeaSpirit.BubbleRiseSpeed * (0.8f + (float)Game1.random.NextDouble() * 0.4f);
                }
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

            int preCastDirection = who.FacingDirection; // capture before base.Cast
            base.Cast(who);

            // ✅ remove any existing SeaSpirit instances
            var existingSeaSpirits = ModEntry.ActiveSpells.OfType<SeaSpirit>().ToList();
            foreach (var existingSpirit in existingSeaSpirits)
            {
                existingSpirit.Unsubscribe();
                SpellRegistry.UnregisterActiveSpell(existingSpirit);
            }

            // remove old buff if any
            if (who.buffs.IsApplied(BuffId))
                who.buffs.Remove(BuffId);

            // setup spell state
            var currentTier = GetCurrentSpellbookTier(who);
            int bubbleCount = GetTierAdjustedBubbleLocations(who);
            float duration = GetTierAdjustedDuration(who);

            // ✅ reset duration if already active
            currentSpellDuration = duration;
            bubbleTimer = currentSpellDuration;

            string message = existingSeaSpirits.Any()
                ? $"Sea Spirit recast: {(int)currentSpellDuration}s, {bubbleCount} location(s) ({currentTier} tier)"
                : $"Sea Spirit: {(int)currentSpellDuration}s, {bubbleCount} location(s) ({currentTier} tier)";

            Game1.addHUDMessage(new HUDMessage(message, 2));

            // register new instance
            ModEntry.RegisterActiveSpell(this);

            // apply buff
            ApplySeaSpiritBuff(who);

            // spawn bubbles
            StartSpellEffects(who, currentTier, bubbleCount, preCastDirection);
        }
        public void AddBubbleLocations(Farmer who, SpellbookTier tier, int count)
        {
            int facingDirection = who.FacingDirection; // capture it

            var newTiles = PickBubbleTiles(who, count, facingDirection);

            foreach (var tile in newTiles)
            {
                if (!bubbles.Any(b => b.Tile == tile))
                    SpawnBubble(tile);
            }
        }

        private List<Vector2> PickBubbleTiles(Farmer who, int count, int facingDirection)
        {
            var results = new List<Vector2>();
            var location = who.currentLocation;
            Vector2 center = who.Tile;
            var currentTier = GetCurrentSpellbookTier(who);

            // Tier-based distance preferences within the 6x6 front area
            var tierSettings = new Dictionary<SpellbookTier, (int minDistance, float farBias)>
    {
        { SpellbookTier.Novice, (0, 0.0f) },      // No minimum distance, no bias
        { SpellbookTier.Apprentice, (1, 0.3f) },  // 1+ tiles away, mild far bias
        { SpellbookTier.Adept, (2, 0.6f) },       // 2+ tiles away, stronger far bias  
        { SpellbookTier.Master, (3, 0.8f) }       // 3+ tiles away, strong far bias
    };

            var (minDistance, farBias) = tierSettings[currentTier];

            var candidateTiles = GetFrontArea(who, 6, facingDirection);

            int tries = 0;
            int maxTries = Math.Min(candidateTiles.Count * 10, 500);

            while (results.Count < count && tries < maxTries)
            {
                tries++;

                // Pick a random tile from the front area
                Vector2 tile = candidateTiles[Game1.random.Next(candidateTiles.Count)];

                // Skip if already selected or not water
                if (results.Contains(tile) || !location.isOpenWater(tile))
                    continue;

                float distance = Vector2.Distance(center, tile);

                // Enforce minimum distance for higher tiers
                if (distance < minDistance)
                    continue;

                // Apply distance-based selection bias within the front area
                float selectionChance = 1.0f;

                if (farBias > 0f)
                {
                    // Calculate distance-based probability (max distance in 6x6 area is ~4.24)
                    float maxPossibleDistance = 4.24f; // Approximate diagonal of 6x6 area
                    float normalizedDistance = Math.Min(distance / maxPossibleDistance, 1f);

                    // Higher tier = higher chance to select tiles that are further away
                    selectionChance = (1f - farBias) + (farBias * normalizedDistance);
                }

                bool shouldSelect = Game1.random.NextDouble() < selectionChance;

                if (shouldSelect)
                    results.Add(tile);
            }

            // Fallback: if we still don't have enough bubbles, fill with any valid water tiles from front area
            if (results.Count < count)
            {
                foreach (var tile in candidateTiles.OrderBy(x => Game1.random.Next()))
                {
                    if (results.Count >= count) break;
                    if (!results.Contains(tile) && location.isOpenWater(tile))
                        results.Add(tile);
                }
            }

            // Final fallback to prevent empty results
            if (results.Count == 0)
                results.Add(center);

            ModEntry.Instance.Monitor.Log(
                $"Picked {results.Count}/{count} bubble tiles for {currentTier} tier " +
                $"from 6x6 front area (min distance: {minDistance}, bias: {farBias:P0}) " +
                $"facing: {DirectionName(facingDirection)}",
                LogLevel.Debug
            );


            return results;
        }

        private List<Vector2> GetFrontArea(Farmer who, int size, int facingDirection)
        {
            var tiles = new List<Vector2>();
            Vector2 center = who.Tile;

            // how far forward and sideways the grid should extend
            int halfWidth = size / 2;

            ModEntry.Instance.Monitor.Log(
                $"DEBUG GetFrontArea: Farmer at {center}, UsingDirection={facingDirection} ({DirectionName(facingDirection)})",
                LogLevel.Info
            );

            switch (facingDirection)
            {
                case 0: // Up
                    for (int forward = 1; forward <= size; forward++)
                    {
                        for (int sideways = -halfWidth; sideways <= halfWidth; sideways++)
                        {
                            tiles.Add(new Vector2(center.X + sideways, center.Y - forward));
                        }
                    }
                    ModEntry.Instance.Monitor.Log("Using UP 6x6 grid", LogLevel.Info);
                    break;

                case 1: // Right
                    for (int forward = 1; forward <= size; forward++)
                    {
                        for (int sideways = -halfWidth; sideways <= halfWidth; sideways++)
                        {
                            tiles.Add(new Vector2(center.X + forward, center.Y + sideways));
                        }
                    }
                    ModEntry.Instance.Monitor.Log("Using RIGHT 6x6 grid", LogLevel.Info);
                    break;

                case 2: // Down
                    for (int forward = 1; forward <= size; forward++)
                    {
                        for (int sideways = -halfWidth; sideways <= halfWidth; sideways++)
                        {
                            tiles.Add(new Vector2(center.X + sideways, center.Y + forward));
                        }
                    }
                    ModEntry.Instance.Monitor.Log("Using DOWN 6x6 grid", LogLevel.Info);
                    break;

                case 3: // Left
                    for (int forward = 1; forward <= size; forward++)
                    {
                        for (int sideways = -halfWidth; sideways <= halfWidth; sideways++)
                        {
                            tiles.Add(new Vector2(center.X - forward, center.Y + sideways));
                        }
                    }
                    ModEntry.Instance.Monitor.Log("Using LEFT 6x6 grid", LogLevel.Info);
                    break;
            }

            return tiles;
        }
        private Vector2 GetFacingDirection(Farmer who)
        {
            // Convert Stardew Valley's facing direction to a Vector2
            return who.FacingDirection switch
            {
                0 => new Vector2(0, -1), // Up
                1 => new Vector2(1, 0),  // Right  
                2 => new Vector2(0, 1),  // Down
                3 => new Vector2(-1, 0), // Left
                _ => new Vector2(0, 1)   // Default to down
            };
        }

        // Also update the GetFacingDirectionName method to use the same logic
        private string DirectionName(int direction) => direction switch
        {
            0 => "Up",
            1 => "Right",
            2 => "Down",
            3 => "Left",
            _ => "Unknown"
        };
        private void SpawnBubble(Vector2 tile)
        {
            var bubble = new Bubble(tile);
            bubbles.Add(bubble);
        }

        private void StartSpellEffects(Farmer who, SpellbookTier tier, int bubbleCount, int facingDirection)
        {
            bubbleTimer = currentSpellDuration;

            var tiles = PickBubbleTiles(who, bubbleCount, facingDirection);
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
                    // Move bubbles
                    b.PixelPos.Y -= b.RiseSpeed * dt;
                    b.PixelPos.X += b.DriftSpeed * dt;

                    // Bounce horizontally within a tile
                    float maxOffset = Game1.tileSize / 2f;
                    if (b.PixelPos.X > b.Tile.X * Game1.tileSize + maxOffset ||
                        b.PixelPos.X < b.Tile.X * Game1.tileSize - maxOffset)
                    {
                        b.DriftSpeed = -b.DriftSpeed;
                    }

                    // Fade out
                    b.Opacity -= BubbleFadeSpeed * dt;

                    // Respawn when fully faded - use fresh values, don't preserve speed
                    if (b.Opacity <= 0f)
                        b.ResetToFreshValues(keepRiseSpeed: false);
                }

                if (bubbleTimer <= 0f)
                {
                    var currentTier = GetCurrentSpellbookTier(who);
                    Game1.addHUDMessage(new HUDMessage($"Sea Spirit faded ({currentTier} tier)", 1));
                    Unsubscribe();
                    ApplyBuffRemoval(who);
                }
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
                bubbles.Clear();
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!IsActive) return;

            foreach (var b in bubbles)
            {
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

        private static bool harmonyPatchesApplied = false;
        private static readonly object harmonyLock = new object();

        private static void ApplyHarmonyPatchesOnce()
        {
            lock (harmonyLock)
            {
                if (!harmonyPatchesApplied)
                {
                    var harmony = new Harmony("nemo.SeaSpiritFishingPatch");

                    // Patch for bite speed boost
                    harmony.Patch(
                        original: AccessTools.Method(typeof(FishingRod), nameof(FishingRod.DoFunction)),
                        postfix: new HarmonyMethod(typeof(SeaSpirit), nameof(FishingRod_DoFunction_Postfix))
                    );

                    // Patch for rare fish / luck near bubbles
                    harmony.Patch(
                        original: AccessTools.Method(typeof(FishingRod), nameof(FishingRod.pullFishFromWater)),
                        prefix: new HarmonyMethod(typeof(SeaSpirit), nameof(PullFishFromWater_Postfix))
                    );

                    harmonyPatchesApplied = true;
                    ModEntry.Instance.Monitor.Log("SeaSpirit Harmony patches applied", LogLevel.Debug);
                }
            }
        }


        // In SeaSpirit class:
        private static readonly List<Vector2> RecentBubbleBites = new();

        public static void FishingRod_DoFunction_Postfix(FishingRod __instance, GameLocation location, int x, int y, int power, Farmer who)
        {
            var seaSpirit = ModEntry.ActiveSpells.OfType<SeaSpirit>().FirstOrDefault();
            if (seaSpirit == null || !seaSpirit.IsActive || !__instance.isFishing || __instance.timeUntilFishingBite <= 0)
                return;

            Vector2 fishingTile = new Vector2(x / Game1.tileSize, y / Game1.tileSize);
            bool nearBubble = seaSpirit.Bubbles.Any(b => Vector2.Distance(fishingTile, b.Tile) <= 1f);

            if (nearBubble)
            {
                float original = __instance.timeUntilFishingBite; // keep as float
                int boosted = Math.Max(500, (int)(original / 3f)); // cast to int
                __instance.timeUntilFishingBite = boosted;

                // Store the tile for later luck check
                RecentBubbleBites.Add(fishingTile);

                ModEntry.Instance.Monitor.Log($"Sea Spirit applied! Original timer: {original}, boosted timer: {boosted}", LogLevel.Debug);
            }

        }

        public static void PullFishFromWater_Postfix(FishingRod __instance, ref int fishQuality)
        {
            if (!(__instance.lastUser is Farmer who))
                return;

            var seaSpirit = ModEntry.ActiveSpells.OfType<SeaSpirit>().FirstOrDefault();
            if (seaSpirit == null || !seaSpirit.IsActive)
                return;

            // Use the bobber tile if possible
            Vector2 fishingTile = new Vector2(
                (int)(__instance.bobber.X / Game1.tileSize),
                (int)(__instance.bobber.Y / Game1.tileSize)
            );
            bool nearBubble = seaSpirit.Bubbles.Any(b => Vector2.Distance(fishingTile, b.Tile) <= 1f);

            if (nearBubble && seaSpirit.GetCurrentSpellbookTier(who) == SpellbookTier.Master)
            {
                if (fishQuality < 4)
                    fishQuality++;

                ModEntry.Instance.Monitor.Log($"Sea Spirit luck boost applied: fish quality upgraded to {fishQuality}", LogLevel.Debug);
            }
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
    



