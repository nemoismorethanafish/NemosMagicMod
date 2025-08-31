using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;

namespace NemosMagicMod.Spells
{
    public class TimeWarp : Spell
    {
        private const int EarliestTime = 600; // 6:00 AM
        private const int MinimumCastTime = 700; // must be at least 7:00 AM
        private const string LastCastKey = "NemosMagicMod.TimeWarp.LastCastDay";

        // Tier-based rewind amounts in minutes (converted to game time units)
        private readonly Dictionary<SpellbookTier, int> tierRewindAmounts = new()
        {
            { SpellbookTier.Novice, 100 },     // 10 minutes
            { SpellbookTier.Apprentice, 300 }, // 30 minutes
            { SpellbookTier.Adept, 600 },      // 1 hour
            { SpellbookTier.Master, 1200 }     // 2 hours
        };

        public TimeWarp()
            : base(
                  id: "nemo.TimeWarp",
                  name: "Time Warp",
                  description: "Bend time backwards. Can only be cast once per day.",
                  manaCost: 15,
                  experienceGained: 25,
                  isActive: false)
        {
        }

        public override void Cast(Farmer who)
        {
            // --- Minimum spellbook tier check (optional) ---
            if (!HasSufficientSpellbookTier(who))
            {
                Game1.showRedMessage($"Requires {MinimumTier} spellbook or higher!");
                return;
            }

            // Generate unique day ID
            int todayId = Game1.year * 1000 + Game1.currentSeason.GetHashCode() * 100 + Game1.dayOfMonth;

            // --- Once-per-day check ---
            if (who.modData.TryGetValue(LastCastKey, out string lastDayStr) && int.TryParse(lastDayStr, out int lastDay))
            {
                if (lastDay == todayId)
                {
                    Game1.showRedMessage("Time Warp can only be cast once per day!");
                    return;
                }
            }

            // --- Not Enough Mana check ---
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            // --- Too early check ---
            if (Game1.timeOfDay < MinimumCastTime)
            {
                Game1.showRedMessage("It's too early to warp time!");
                return;
            }

            // --- Spend mana and grant XP immediately ---
            ManaManager.SpendMana(ManaCost);
            base.GrantExperience(who);

            // Determine the rewind amount based on the player's spellbook tier
            SpellbookTier tier = GetCurrentSpellbookTier(who);
            int rewindAmount = tierRewindAmounts.GetValueOrDefault(tier, 100); // default 10 min

            // --- Delay the actual time rewind and effects by 1 second ---
            DelayedAction.functionAfterDelay(() =>
            {
                // Rewind time
                Game1.timeOfDay -= rewindAmount;
                if (Game1.timeOfDay < EarliestTime)
                    Game1.timeOfDay = EarliestTime;

                // Flash + sound
                Game1.flashAlpha = 1f;
                Game1.playSound("wand");
                Game1.activeClickableMenu = null;

                // Swirl particle effects
                AddSwirlEffect(who);

                // Record last cast day
                who.modData[LastCastKey] = todayId.ToString();

            }, 1000); // 1 second delay
        }

        private void AddSwirlEffect(Farmer who)
        {
            Vector2 position = who.Position;
            int particleCount = 12;

            for (int i = 0; i < particleCount; i++)
            {
                double angle = i * (Math.PI * 2 / particleCount);
                Vector2 offset = new Vector2(
                    (float)Math.Cos(angle) * 64f,
                    (float)Math.Sin(angle) * 64f
                );

                TemporaryAnimatedSprite swirl = new TemporaryAnimatedSprite(
                    textureName: "TileSheets\\animations",
                    sourceRect: new Rectangle(0, 0, 64, 64),
                    animationInterval: 100f,
                    animationLength: 4,
                    numberOfLoops: 1,
                    position: position + offset,
                    flicker: false,
                    flipped: false
                )
                {
                    motion = -offset / 32f, // move inward
                    acceleration = Vector2.Zero,
                    scale = 0.5f,
                    alphaFade = 0.02f,
                    color = Color.LightBlue,
                    layerDepth = 1f,
                    rotationChange = 0.1f
                };

                Game1.currentLocation.temporarySprites.Add(swirl);
            }
        }
    }
}
