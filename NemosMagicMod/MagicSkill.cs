using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using System.Collections.Generic;
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

        public override List<string> GetExtraLevelUpInfo(int level) =>
            new() { $"Magic bonus: {5 * level}" };

        public override string GetSkillPageHoverText(int level) =>
            $"Level {level} Magic bonus: {5 * level}";
    }
}
