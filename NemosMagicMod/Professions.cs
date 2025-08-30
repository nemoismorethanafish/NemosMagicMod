using static SpaceCore.Skills;

public static class Level5Professions
{
    public class ArcaneMaster : Skill.Profession
    {
        public ArcaneMaster(Skill parentSkill)
            : base(parentSkill, "ArcaneMaster") { }

        public override string GetName() => "Arcane Master";
        public override string GetDescription() => "You no longer need to carry your spellbook.";

        public override void DoImmediateProfessionPerk()
        {
            // Placeholder for functionality: no need to carry spellbook anymore
        }
    }

    public class FireballCantrip : Skill.Profession
    {
        public FireballCantrip(Skill parentSkill)
            : base(parentSkill, "FireballCantrip") { }

        public override string GetName() => "Fireball Cantrip";
        public override string GetDescription() => "Your Fireball spell is replaced with one that costs 0 mana and awards 0 XP.";

        public override void DoImmediateProfessionPerk()
        {
            // Placeholder for functionality: Replace fireball spell
        }
    }
}
