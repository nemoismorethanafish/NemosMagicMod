using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;

namespace NemosMagicMod.Spells
{
    public class TimeWarp : Spell
    {
        private const int RewindAmount = 100; // 10 minutes
        private const int EarliestTime = 600; // 6:00 AM
        private const int MinimumCastTime = 700; // must be at least 7:00 AM
        private const string LastCastKey = "NemosMagicMod.TimeWarp.LastCastDay";

        public TimeWarp()
            : base(
                  id: "nemo.TimeWarp",
                  name: "Time Warp",
                  description: "Bend time backwards by 1 hour, never earlier than 6:00 AM. Can only be cast once per day.",
                  manaCost: 15,
                  experienceGained: 25,
                  isActive: false)
        {
        }

        public override void Cast(Farmer who)
        {
            // Generate unique day ID
            int todayId = Game1.year * 1000 + Game1.currentSeason.GetHashCode() * 100 + Game1.dayOfMonth;

            // Check once-per-day
            if (who.modData.TryGetValue(LastCastKey, out string lastDayStr) && int.TryParse(lastDayStr, out int lastDay))
            {
                if (lastDay == todayId)
                {
                    Game1.showRedMessage("Time Warp can only be cast once per day!");
                    return; // do NOT spend mana
                }
            }

            // Check mana
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            if (Game1.timeOfDay < MinimumCastTime)
            {
                Game1.showRedMessage("It's too early to warp time!");
                return;
            }

            // Spend mana and grant XP
            ManaManager.SpendMana(ManaCost);
            base.GrantExperience(who);

            // Rewind time
            Game1.timeOfDay -= RewindAmount;
            if (Game1.timeOfDay < EarliestTime)
                Game1.timeOfDay = EarliestTime;

            // Flash + sound
            Game1.flashAlpha = 1f;
            Game1.playSound("wand");
            Game1.activeClickableMenu = null;

            // Spawn swirl particles
            AddSwirlEffect(who);

            // Record last cast day
            who.modData[LastCastKey] = todayId.ToString();
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
