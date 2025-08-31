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
            this.ExperienceCurve = new int[]
            {
                100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000
            };

            Texture2D icon = NemosMagicMod.ModEntry.MagicSkillIcon;
            this.Icon = icon;
            this.SkillsPageIcon = icon;

            // Create the professions from Level5Professions
            var prof1 = new Level5Professions.ArcaneMaster(this);
            var prof2 = new Level5Professions.BattleMage(this);

            // Add the profession pair at level 5
            this.ProfessionsForLevels.Add(new ProfessionPair(5, prof1, prof2, null));

            // Add the individual professions to the skill
            this.Professions.Add(prof1);
            this.Professions.Add(prof2);
        }

        public override string GetName() => "Magic";

        public override List<string> GetExtraLevelUpInfo(int level) =>
            new() { $"Magic bonus: {5 * level}" };

        public override string GetSkillPageHoverText(int level) =>
            $"Level {level} Magic bonus: {5 * level}";
    }
}
