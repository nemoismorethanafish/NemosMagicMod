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

            Texture2D icon = NemosMagicMod.ModEntry.MagicSkillIcon;
            this.Icon = icon;
            this.SkillsPageIcon = icon;

            // Create the professions
            var prof1 = new CantripFocusProfession(this);
            var prof2 = new ArcaneSurgeProfession(this);

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

    public class CantripFocusProfession : Skill.Profession
    {
        public CantripFocusProfession(Skill parentSkill)
            : base(parentSkill, "CantripFocus") { }

        public override string GetName() => "Cantrip Focus";
        public override string GetDescription() => "Choose this profession for flavor only (does nothing).";

        public override void DoImmediateProfessionPerk()
        {
            // No functionality for now
        }
    }

    public class ArcaneSurgeProfession : Skill.Profession
    {
        public ArcaneSurgeProfession(Skill parentSkill)
            : base(parentSkill, "ArcaneSurge") { }

        public override string GetName() => "Arcane Surge";
        public override string GetDescription() => "Choose this profession for flavor only (does nothing).";

        public override void DoImmediateProfessionPerk()
        {
            // No functionality for now
        }
    }
}
