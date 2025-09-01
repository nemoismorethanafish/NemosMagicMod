using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Collections.Generic;
using static Spell;

namespace NemosMagicMod.Spells
{
    public class WaterSpirit : Spell, IRenderable
    {
        private Texture2D cloudTexture;
        private Texture2D splashTexture;

        private bool subscribed = false;

        private float spellTimer = 0f;
        private float spellDuration = 10f; // now adjustable by tier

        private float wateringTimer = 0f;
        private readonly float wateringInterval = 1f;

        private Vector2 cloudPosition;
        private Vector2 cloudVelocity;
        private readonly float cloudSpeed = 64f;
        private readonly float cloudSeekSpeed = 64f;
        private readonly int seekRadius = 5;

        private Vector2? targetTile = null;

        private Farmer owner;

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

        // Tier-based duration multipliers
        private readonly Dictionary<SpellbookTier, float> durationMultipliers = new()
    {
        { SpellbookTier.Novice, 1.0f },
        { SpellbookTier.Apprentice, 1.5f },
        { SpellbookTier.Adept, 2.0f },
        { SpellbookTier.Master, 2.5f }
    };

        // Minimum tier for the spell
        protected override SpellbookTier MinimumTier => SpellbookTier.Novice;

        public WaterSpirit()
            : base("water_spirit", "Water Spirit",
                  "Summons a friendly rain cloud that waters nearby crops.",
                  30, 25)
        {
            cloudTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/raincloud.png");
            splashTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/WaterSplash.png");
        }

        private float GetTierAdjustedDuration(Farmer who)
        {
            var tier = GetCurrentSpellbookTier(who);
            float multiplier = durationMultipliers.GetValueOrDefault(tier, 1f);
            return 10f * multiplier; // base 10 seconds * tier multiplier
        }

        public override void Cast(Farmer who)
        {
            if (!CanCast(who))
                return;

            base.Cast(who);

            // Apply tier-based duration
            spellDuration = GetTierAdjustedDuration(who);

            // Optionally show HUD message
            Game1.addHUDMessage(new HUDMessage($"Water Spirit summoned for {(int)spellDuration}s", 2));

            DelayedAction.functionAfterDelay(() =>
            {
                owner = who;

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
                targetTile = null;

                cloudPosition = who.Position + new Vector2(0, -64f);

                switch (who.FacingDirection)
                {
                    case 0: cloudVelocity = new Vector2(0, -cloudSpeed); break;
                    case 1: cloudVelocity = new Vector2(cloudSpeed, 0); break;
                    case 2: cloudVelocity = new Vector2(0, cloudSpeed); break;
                    case 3: cloudVelocity = new Vector2(-cloudSpeed, 0); break;
                    default: cloudVelocity = Vector2.Zero; break;
                }

                if (!subscribed)
                {
                    ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
                    ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
                    subscribed = true;
                }

                Game1.playSound("wateringCan");

            }, 1000);
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

            // Seek nearest crop if no target
            if (targetTile == null)
            {
                targetTile = FindNearestCrop();
            }

            // Move cloud
            if (targetTile != null)
            {
                Vector2 targetWorld = targetTile.Value * Game1.tileSize + new Vector2(Game1.tileSize / 2, Game1.tileSize / 2);
                Vector2 direction = targetWorld - cloudPosition;

                if (direction.LengthSquared() > 4f)
                {
                    direction.Normalize();
                    cloudPosition += direction * cloudSeekSpeed * deltaSeconds;
                    cloudVelocity = direction * cloudSeekSpeed; // update velocity for scoring
                }
                else
                {
                    WaterNearbyCrops();
                    targetTile = null;
                    cloudVelocity = Vector2.Zero;
                }
            }
            else if (owner != null)
            {
                // Return to player if no target
                Vector2 direction = owner.Position - cloudPosition;
                if (direction.LengthSquared() > 4f)
                {
                    direction.Normalize();
                    cloudPosition += direction * cloudSeekSpeed * deltaSeconds;
                    cloudVelocity = direction * cloudSeekSpeed; // update velocity
                }
                else
                {
                    cloudVelocity = Vector2.Zero;
                }
            }

            // Water crops every interval (extra safety)
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

            if (spellTimer >= spellDuration)
            {
                IsActive = false;
                Unsubscribe();
            }
        }

        private Vector2? FindNearestCrop()
        {
            if (Game1.currentLocation == null) return null;

            Vector2 cloudTile = new Vector2(
                (int)Math.Floor(cloudPosition.X / Game1.tileSize),
                (int)Math.Floor(cloudPosition.Y / Game1.tileSize)
            );

            float closestScore = float.MaxValue; // lower score = more desirable
            Vector2? bestTile = null;

            foreach (var kvp in Game1.currentLocation.terrainFeatures.Pairs)
            {
                if (kvp.Value is StardewValley.TerrainFeatures.HoeDirt dirt && dirt.crop != null)
                {
                    // Skip crops that are already watered
                    if (dirt.state.Value == StardewValley.TerrainFeatures.HoeDirt.watered)
                        continue;

                    Vector2 tile = kvp.Key;
                    Vector2 toTile = tile - cloudTile;
                    float distance = toTile.Length();

                    if (distance > seekRadius)
                        continue;

                    // Score based on distance and alignment with cloud velocity
                    float directionScore = 0f;
                    if (cloudVelocity.LengthSquared() > 0f)
                    {
                        Vector2 dirNorm = Vector2.Normalize(cloudVelocity);
                        Vector2 toTileNorm = Vector2.Normalize(toTile);
                        float alignment = Vector2.Dot(dirNorm, toTileNorm); // 1 = perfectly aligned
                        directionScore = distance * (1f - alignment); // smaller = better
                    }
                    else
                    {
                        directionScore = distance;
                    }

                    if (directionScore < closestScore)
                    {
                        closestScore = directionScore;
                        bestTile = tile;
                    }
                }
            }

            return bestTile;
        }

        private void WaterNearbyCrops()
        {
            if (Game1.currentLocation == null) return;

            int cloudTileX = (int)Math.Floor(cloudPosition.X / Game1.tileSize);
            int cloudTileY = (int)Math.Floor(cloudPosition.Y / Game1.tileSize);
            Vector2 cloudTile = new Vector2(cloudTileX, cloudTileY);

            if (Game1.currentLocation.terrainFeatures.TryGetValue(cloudTile, out var feature)
                && feature is StardewValley.TerrainFeatures.HoeDirt dirt)
            {
                dirt.state.Value = StardewValley.TerrainFeatures.HoeDirt.watered;

                Vector2 splashPos = cloudTile * Game1.tileSize + new Vector2(Game1.tileSize / 2, Game1.tileSize / 2);
                activeSplashes.Add(new WaterSplash(splashPos, splashDuration));

                Game1.playSound("wateringCan");
            }
        }


        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!IsActive) return;

            SpriteBatch spriteBatch = e.SpriteBatch;

            // Draw cloud
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

        public void Unsubscribe()
        {
            if (!subscribed) return;

            ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            subscribed = false;
        }
    }
}
