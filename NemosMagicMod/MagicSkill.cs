using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod.Spells;
using SpaceCore;
using StardewValley;
using System.Collections.Generic;
using System.Linq;
using static SpaceCore.Skills;

namespace MagicSkill
{
    public class Magic_Skill : Skill
    {
        public Magic_Skill()
            : base("nemosmagicmod.Magic")
        {
            // Experience per level
            this.ExperienceCurve = new int[]
            {
                100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000
            };
            // Icons
            Texture2D icon = NemosMagicMod.ModEntry.MagicSkillIcon;
            this.Icon = icon;
            this.SkillsPageIcon = icon;
            // -----------------------------
            // Level 5 professions
            // -----------------------------
            var manaDork = new Level5Professions.ManaDork(this);
            var battleMage = new Level5Professions.BattleMage(this);
            this.ProfessionsForLevels.Add(new ProfessionPair(5, manaDork, battleMage, null));
            this.Professions.Add(manaDork);
            this.Professions.Add(battleMage);
            // -----------------------------
            // Level 10 professions
            // -----------------------------
            var manaRegen = new Level10Professions.ManaRegeneration(this);
            var warWizard = new Level10Professions.WarWizard(this);
            var bonusDaily = new Level10Professions.BonusDaily(this);
            // ArcaneMaster → ManaRegeneration or BonusDaily
            this.ProfessionsForLevels.Add(new ProfessionPair(10, manaRegen, bonusDaily, manaDork));
            // BattleMage → WarWizard or BonusDaily
            this.ProfessionsForLevels.Add(new ProfessionPair(10, warWizard, bonusDaily, battleMage));
            // Add to Professions list
            this.Professions.Add(manaRegen);
            this.Professions.Add(warWizard);
            this.Professions.Add(bonusDaily);
        }

        public override string GetName() => "Magic";

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            var info = new List<string> { $"Mana Increased" };

            // Add spell unlock information for the level-up menu
            var unlockedSpells = GetSpellsUnlockedAtLevel(level);
            if (unlockedSpells.Any())
            {
                info.Add(""); // Empty line for spacing
                info.Add("Spells Unlocked:");
                foreach (var spell in unlockedSpells)
                {
                    info.Add($"• {spell.Name}");
                }
            }

            return info;
        }

        public override string GetSkillPageHoverText(int level) =>
            $"You're a hairy wizard!";

        /// <summary>
        /// Get all spells that are unlocked at a specific level
        /// </summary>
        private List<Spell> GetSpellsUnlockedAtLevel(int level)
        {
            var spells = new List<Spell>();

            // Check profession-specific unlocks for level 1
            if (level == 1)
            {
                try
                {
                    int battleMageID = GetProfessionId("BattleMage");
                    bool hasBattleMage = battleMageID != -1 && Game1.player.professions.Contains(battleMageID);

                    if (hasBattleMage)
                    {
                        spells.Add(SpellRegistry.FireballCantrip);
                    }
                    else
                    {
                        spells.Add(SpellRegistry.Fireball);
                    }
                }
                catch
                {
                    // Fallback to regular fireball if profession check fails
                    spells.Add(SpellRegistry.Fireball);
                }
            }

            // Regular level unlocks
            switch (level)
            {
                case 2:
                    spells.Add(SpellRegistry.Heal);
                    break;
                case 3:
                    spells.Add(SpellRegistry.TreeSpirit);
                    spells.Add(SpellRegistry.EarthSpirit);
                    break;
                case 4:
                    spells.Add(SpellRegistry.SeaSpirit);
                    break;
                case 6:
                    spells.Add(SpellRegistry.HomeWarp);
                    break;
                case 7:
                    spells.Add(SpellRegistry.TimeWarp);
                    break;
                case 9:
                    spells.Add(SpellRegistry.FertilitySpirit);
                    break;
            }

            return spells;
        }

        private int GetProfessionId(string profession)
        {
            try
            {
                return Skills.GetSkill("nemosmagicmod.Magic").Professions
                             .Single(p => p.Id == profession)
                             .GetVanillaId();
            }
            catch
            {
                return -1;
            }
        }
    }
}