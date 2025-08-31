using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using System.Threading;
using static SpaceCore.Skills;

public static class Level5Professions
{
    public class ArcaneMaster : Skill.Profession
    {
        public ArcaneMaster(Skill parentSkill)
            : base(parentSkill, "ArcaneMaster") { }

        public override string GetName() => "Arcane Master";
        public override string GetDescription() => "+50 to your maximum Mana.";

        public override void DoImmediateProfessionPerk()
        {
            // Placeholder for functionality: 
        }
    }

    public class BattleMage : Skill.Profession
    {
        public BattleMage(Skill parentSkill)
            : base(parentSkill, "BattleMage") { }

        public override string GetName() => "Battle Mage";
        public override string GetDescription() => "Your Fireball spell is replaced with one that costs 0 mana and awards 0 XP.";

        public override void DoImmediateProfessionPerk()
        {
            // Lock original Fireball
            SpellRegistry.PlayerData.UnlockedSpellIds.Remove(SpellRegistry.Fireball.Id);

            // Unlock FireballCantrip
            if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(SpellRegistry.FireballCantrip.Id))
            {
                SpellRegistry.PlayerData.UnlockedSpellIds.Add(SpellRegistry.FireballCantrip.Id);
            }
        }
    }
}
