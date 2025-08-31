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
using System.Reflection;
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
            { SpellbookTier.Novice, 1.0f },     // 30 seconds base
            { SpellbookTier.Apprentice, 1.5f }, // 45 seconds
            { SpellbookTier.Adept, 2.0f },      // 60 seconds
            { SpellbookTier.Master, 2.5f }      // 75 seconds
        };

        private readonly Dictionary<SpellbookTier, int> bubbleLocationCounts = new()
        {
            { SpellbookTier.Novice, 1 },      // 1 bubble location
            { SpellbookTier.Apprentice, 3 },  // 2 bubble locations
            { SpellbookTier.Adept, 6 },       // 3 bubble locations
            { SpellbookTier.Master, 9 }       // 4 bubble locations
        };

        private Texture2D bubbleTexture;
        private bool subscribedDraw = false;
        public List<Bubble> bubbles = new();
        public IReadOnlyList<Bubble> Bubbles => bubbles.AsReadOnly();

        private float bubbleTimer = 0f;
        private float currentSpellDuration = BaseBubbleDuration;

        // Override minimum tier requirement
        protected override SpellbookTier MinimumTier => SpellbookTier.Novice;

        public SeaSpirit()
            : base("nemo.SeaSpirit", "Sea Spirit", "Summons magical bubbles that increase fishing bite rates. Duration and coverage increase with spellbook tier.", 25)
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

        /// <summary>
        /// Calculates the spell duration based on the current spellbook tier
        /// </summary>
        private float GetTierAdjustedDuration(Farmer who)
        {
            var currentTier = GetCurrentSpellbookTier(who);
            var multiplier = durationMultipliers.GetValueOrDefault(currentTier, 1.0f);
            return BaseBubbleDuration * multiplier;
        }

        /// <summary>
        /// Gets the number of bubble locations based on the current spellbook tier
        /// </summary>
        private int GetTierAdjustedBubbleLocations(Farmer who)
        {
            var currentTier = GetCurrentSpellbookTier(who);
            return bubbleLocationCounts.GetValueOrDefault(currentTier, 1);
        }

        public override void Cast(Farmer who)
        {
            // Check spellbook tier requirement
            if (!HasSufficientSpellbookTier(who))
            {
                string requiredTierName = MinimumTier.ToString();
                Game1.showRedMessage($"Requires {requiredTierName} spellbook or higher!");
                return;
            }

            // --- Not Enough Mana check ---
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            // Calculate tier-based values
            var currentTier = GetCurrentSpellbookTier(who);
            currentSpellDuration = GetTierAdjustedDuration(who);
            var bubbleLocationCount = GetTierAdjustedBubbleLocations(who);

            base.Cast(who);

            // Show tier-specific message
            var durationSeconds = (int)currentSpellDuration;
            Game1.addHUDMessage(new HUDMessage($"Sea Spirit: {durationSeconds}s, {bubbleLocationCount} location(s) ({currentTier} tier)", 2));

            // Set timer to activate the spell (this makes IsActive return true)
            bubbleTimer = currentSpellDuration;
            ModEntry.RegisterActiveSpell(this);

            // --- Delay the custom bubble effects ---
            DelayedAction.functionAfterDelay(() =>
            {
                // Only reset OTHER active spells, not this one
                foreach (var spell in ModEntry.ActiveSpells.ToList())
                {
                    if (spell != this) // Don't deactivate ourselves
                    {
                        if (spell is IRenderable renderable)
                            renderable.Unsubscribe();
                        spell.IsActive = false;
                    }
                }

                // Clean up any existing SeaSpirit subscription first
                if (subscribedDraw)
                {
                    Unsubscribe();
                }

                bubbles.Clear();

                GameLocation location = who.currentLocation;
                Vector2 playerTile = new((int)(who.Position.X / Game1.tileSize), (int)(who.Position.Y / Game1.tileSize));

                // Find all valid water tiles
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

                // Select multiple bubble locations based on tier
                if (validTiles.Count > 0)
                {
                    List<Vector2> selectedTiles = new();

                    // Shuffle the valid tiles to ensure random selection
                    var shuffledTiles = validTiles.OrderBy(x => Game1.random.Next()).ToList();

                    // Select unique bubble locations up to the tier limit
                    int locationsToSelect = Math.Min(bubbleLocationCount, shuffledTiles.Count);
                    for (int i = 0; i < locationsToSelect; i++)
                    {
                        selectedTiles.Add(shuffledTiles[i]);
                    }

                    // Debug log for troubleshooting
                    ModEntry.Instance.Monitor.Log($"SeaSpirit: Selected {selectedTiles.Count} locations out of {validTiles.Count} valid tiles", LogLevel.Debug);

                    // Create bubbles for each selected location
                    int bubblesPerLocation = 8; // number of simultaneous bubbles per location
                    foreach (var tile in selectedTiles)
                    {
                        for (int i = 0; i < bubblesPerLocation; i++)
                        {
                            bubbles.Add(new Bubble(tile));
                        }
                    }

                    ModEntry.Instance.Monitor.Log($"SeaSpirit: Created {bubbles.Count} total bubbles", LogLevel.Debug);
                }

                bubbleTimer = currentSpellDuration;
                SubscribeDraw();

                Game1.showGlobalMessage($"Sea Spirit activated! Fishing bite rate increased for {(int)currentSpellDuration} seconds.");

            }, 1000); // 1-second delay
        }

        public void Update(float dt)
        {
            for (int i = 0; i < bubbles.Count; i++)
            {
                var b = bubbles[i];

                if (!b.Active)
                {
                    b.StartDelay -= dt;
                    if (b.StartDelay <= 0f)
                        b.Active = true;
                    continue;
                }

                // movement
                b.PixelPos.Y -= b.RiseSpeed * dt;
                b.PixelPos.X += b.DriftSpeed * dt;

                // bounce horizontally within tile bounds
                float maxOffset = Game1.tileSize / 2f;
                if (b.PixelPos.X > b.Tile.X * Game1.tileSize + maxOffset || b.PixelPos.X < b.Tile.X * Game1.tileSize - maxOffset)
                    b.DriftSpeed *= -1f;

                // clamp to prevent runaway drift
                b.DriftSpeed = MathHelper.Clamp(b.DriftSpeed, -20f, 20f);

                // fade out as it rises
                b.Opacity -= 0.25f * dt;

                if (b.Opacity <= 0f)
                {
                    // reset bubble like constructor
                    b.PixelPos = b.Tile * Game1.tileSize + new Vector2(Game1.random.Next(-4, 5), 0);
                    b.Opacity = 1f;
                    b.Active = false;
                    b.StartDelay = (float)Game1.random.NextDouble() * 1f;

                    // reset velocity
                    b.RiseSpeed = BubbleRiseSpeed * (0.8f + (float)Game1.random.NextDouble() * 0.4f);
                    b.DriftSpeed = ((float)Game1.random.NextDouble() - 0.5f) * 20f;
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