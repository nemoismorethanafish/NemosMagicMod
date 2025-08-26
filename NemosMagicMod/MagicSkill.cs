using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using System.Collections.Generic;
using static SpaceCore.Skills;

namespace MagicSkill
{
    public class Magic_Skill : Skill
    {
        // Constructor to initialize the skill
        public Magic_Skill()
            : base("nemosmagicmod.Magic") // Unique skill ID
        {
            // Ensure the helper and assets are loaded properly
            this.Icon = NemosMagicMod.ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/magic-icon-smol.png");

            // Set experience bar color (Purple for magic)
            this.ExperienceBarColor = new Color(80, 0, 255);

            // Define the experience curve for leveling up
            this.ExperienceCurve = new int[10] { 100, 300, 600, 1000, 1500, 2200, 3000, 4000, 5000, 6000 };
        }

        // Get the name of the skill (localized)
        public override string GetName()
        {
            return "Magic";  // Localize this if needed
        }

        // Additional info when player levels up the skill
        public override List<string> GetExtraLevelUpInfo(int level)
        {
            return new List<string>
            {
                $"Magic bonus: {5 * level}"  // Add your logic here
            };
        }

        // Hover text on the skill page
        public override string GetSkillPageHoverText(int level)
        {
            return $"Level {level} Magic bonus: {5 * level}";
        }
    }
}
