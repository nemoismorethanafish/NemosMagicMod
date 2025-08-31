using NemosMagicMod;
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
public static class Level10Professions
{
    public class ManaRegeneration : Skill.Profession
    {
        public ManaRegeneration(Skill parentSkill)
            : base(parentSkill, "ManaRegeneration") { }

        public override string GetName() => "Mana Regeneration";
        public override string GetDescription() => "Regenerate 1 Mana per second.";

        public override void DoImmediateProfessionPerk()
        {
            // Functionality to be implemented later
        }
    }

    public class WarWizard : Skill.Profession
    {
        public WarWizard(Skill parentSkill)
            : base(parentSkill, "WarWizard") { }

        public override string GetName() => "War Wizard";
        public override string GetDescription() => "Grants a unique, powerful spell.";

        public override void DoImmediateProfessionPerk()
        {
            if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(SpellRegistry.FireCyclone.Id))
            {
                SpellRegistry.PlayerData.UnlockedSpellIds.Add(SpellRegistry.FireCyclone.Id);
                ModEntry.Instance.Monitor.Log("Unlocked Fire Cyclone spell via War Wizard profession!", LogLevel.Info);
            }
        }
    }

    public class BonusDaily : Skill.Profession
    {
        public BonusDaily(Skill parentSkill)
            : base(parentSkill, "BonusDaily") { }

        public override string GetName() => "Bonus Daily";
        public override string GetDescription() => "Allows you to ignore the once-per-day restriction… once per day.";

        public override void DoImmediateProfessionPerk()
        {
            // Functionality to be implemented later
        }
    }
}
