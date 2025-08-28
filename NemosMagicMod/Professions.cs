using static SpaceCore.Skills;

public class CantripFocusProfession : Skill.Profession
{
    public CantripFocusProfession(Skill parentSkill)
        : base(parentSkill, "CantripFocus") { }

    public override string GetName() => "Cantrip Focus";
    public override string GetDescription() => "Replace one spell with a free Cantrip (0 mana, 0 XP).";

    public override void DoImmediateProfessionPerk()
    {
        // Assign the free cantrip here
    }
}
