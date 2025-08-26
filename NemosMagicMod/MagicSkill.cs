using Microsoft.Xna.Framework;
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
                100, 300, 600, 1000, 1500, 2200, 3000, 4000, 5000, 6000
            };

            // Assign both the main icon and skill page icon
            Texture2D icon = NemosMagicMod.ModEntry.MagicSkillIcon;
            this.Icon = icon;
            this.SkillsPageIcon = icon;
        }

        public override string GetName() => "Magic";

        public override List<string> GetExtraLevelUpInfo(int level)
        {
            return new()
            {
                $"Magic bonus: {5 * level}"
            };
        }

        public override string GetSkillPageHoverText(int level)
        {
            return $"Level {level} Magic bonus: {5 * level}";
        }
    }
}
