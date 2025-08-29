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
        private const float BubbleDuration = 30f; // duration in seconds
        private const int BubbleRadius = 5;       // radius in tiles
        private float bubbleTimer = 0f;
        private List<Vector2> bubbleTiles = new();
        private Texture2D bubbleTexture;
        private bool subscribedDraw = false;
        private bool boostAppliedThisCast = false;
        public static bool Active { get; private set; } = false;


        public IReadOnlyList<Vector2> BubbleTiles => bubbleTiles;
        public bool IsActive => bubbleTimer > 0f;

        public SeaSpirit()
            : base("nemo.SeaSpirit", "Sea Spirit", "Summons magical bubbles that increase fishing bite rates.", 25)
        {
            bubbleTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/bubbles.png");
            ApplyHarmonyPatches();
        }

        protected override bool FreezePlayerDuringCast => false;

        public override void Cast(Farmer who)
        {
            base.Cast(who);
            bubbleTiles.Clear();
            boostAppliedThisCast = false;

            GameLocation location = who.currentLocation;
            Vector2 playerTile = new((int)(who.Position.X / Game1.tileSize), (int)(who.Position.Y / Game1.tileSize));

            // Fill bubble tiles (visual only)
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

            Game1.showGlobalMessage($"Sea Spirit activated! Fishing bite rate increased for {BubbleDuration} seconds.");
            ModEntry.Instance.Monitor.Log("Sea Spirit spell cast!", LogLevel.Info);
        }

        public override void Update(GameTime gameTime, Farmer who)
        {
            if (bubbleTimer > 0f)
            {
                bubbleTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (bubbleTimer <= 0f)
                {
                    Unsubscribe();
                    ModEntry.Instance.Monitor.Log("Sea Spirit expired.", LogLevel.Info);
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
                bubbleTiles.Clear();
            }
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!IsActive) return;

            foreach (var tile in bubbleTiles)
            {
                Vector2 pixelPos = Game1.GlobalToLocal(Game1.viewport, tile * Game1.tileSize);
                e.SpriteBatch.Draw(
                    bubbleTexture,
                    pixelPos,
                    null,
                    Color.White * 0.8f,
                    0f,
                    Vector2.Zero,
                    1f,
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

        // This postfix runs right after DoFunction, when the rod sets up fishing.
        public static void FishingRod_DoFunction_Postfix(FishingRod __instance, GameLocation location, int x, int y, int power, Farmer who)
        {
            var seaSpirit = SpellRegistry.SeaSpirit;

            if (seaSpirit.IsActive && __instance.isFishing && __instance.timeUntilFishingBite > 0)
            {
                int original = (int)__instance.timeUntilFishingBite;
                int boosted = Math.Max(500, original / 3); // 3x faster
                __instance.timeUntilFishingBite = boosted;

                ModEntry.Instance.Monitor.Log(
                    $"Sea Spirit applied! Original timer: {original}, boosted timer: {boosted}",
                    LogLevel.Debug
                );
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
