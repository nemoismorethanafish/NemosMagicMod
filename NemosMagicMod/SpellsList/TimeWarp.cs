using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewValley;
using StardewValley.BellsAndWhistles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NemosMagicMod.Spells
{
    public class TimeWarp : Spell
    {
        private const int EarliestTime = 600; // 6:00 AM
        private const int MinimumCastTime = 800; // must be at least 8:00 AM
        private const string LastCastKey = "NemosMagicMod.TimeWarp.LastCastDay";
        protected override SpellbookTier MinimumTier => SpellbookTier.Adept;

        private Texture2D timeTexture;


        private int GetProfessionId(string skillId, string professionId)
        {
            return Skills.GetSkill(skillId)
                         .Professions
                         .Single(p => p.Id == professionId)
                         .GetVanillaId();
        }


        // Tier-based rewind amounts in minutes (converted to game time units)
        private readonly Dictionary<SpellbookTier, int> tierRewindAmounts = new()
        {
            { SpellbookTier.Novice, 200 },     // 10 minutes
            { SpellbookTier.Apprentice, 200 }, // 30 minutes
            { SpellbookTier.Adept, 200 },      // 1 hour
            { SpellbookTier.Master, 300 }     // 2 hours
        };

        public TimeWarp()
            : base(
                  id: "nemo.TimeWarp",
                  name: "Time Warp",
                  description: "Bend time backwards. Can only be cast once per day.",
                  manaCost: 100,
                  experienceGained: 50,
                  isActive: false,
                  "assets/TimeSpiral.png")
        {
            timeTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/TimeSpiral.png");
            iconTexture = timeTexture;
        }

        public override void Cast(Farmer who)
        {
            if (!CanCast(who))
                return;

            // --- Generate unique day ID ---
            int todayId = Game1.year * 1000 + Game1.currentSeason.GetHashCode() * 100 + Game1.dayOfMonth;

            // --- Check normal cast ---
            bool normalUsedToday = who.modData.TryGetValue(LastCastKey, out string lastDayStr)
                                   && int.TryParse(lastDayStr, out int lastDay)
                                   && lastDay == todayId;

            // --- Check Bonus Daily cast ---
            int bonusDailyId = GetProfessionId(SkillID, "BonusDaily");
            bool hasBonusDaily = Game1.player.professions.Contains(bonusDailyId);
            bool bonusUsedToday = who.modData.TryGetValue("NemosMagicMod.TimeWarp.BonusDailyUsed", out string usedStr)
                                  && int.TryParse(usedStr, out int usedDay)
                                  && usedDay == todayId;

            // --- Block if both normal and bonus are spent ---
            if (normalUsedToday && (!hasBonusDaily || bonusUsedToday))
            {
                Game1.showRedMessage("Time Warp can only be cast once per day!");
                return;
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

            // --- Base cast (spends mana, triggers standard effects) ---
            base.Cast(who);

            // --- Determine the rewind amount based on the player's spellbook tier ---
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

                // --- Record cast usage ---
                if (!normalUsedToday)
                {
                    who.modData[LastCastKey] = todayId.ToString(); // spend normal cast
                }
                else if (hasBonusDaily && !bonusUsedToday)
                {
                    who.modData["NemosMagicMod.TimeWarp.BonusDailyUsed"] = todayId.ToString(); // spend bonus cast
                }

            }, 1000);
        }
    }
}
