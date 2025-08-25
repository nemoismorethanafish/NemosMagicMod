using SpaceCore;
using StardewModdingAPI;
using static SpaceCore.Skills;

namespace MagicSkillDefinition
{
    public class MagicSkill : Skill
    {
        public static MagicSkill Instance { get; private set; } = null!;

        public const string MagicSkillId = "NemosMagicMod.MagicSkill";

        public MagicSkill(IModHelper helper) : base(MagicSkillId)
        {
            Instance = this; // <-- Set static instance here
        }

        public int GetVanillaSkillIndex()
        {
            return 9;
        }


        // Your existing overrides and methods here...

        public override string GetName()
        {
            return "Magic";
        }
    }
}
