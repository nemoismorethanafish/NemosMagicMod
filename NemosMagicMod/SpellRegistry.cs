using NemosMagicMod;
using NemosMagicMod.Spells;
using System.Collections.Generic;

public static class SpellRegistry
{
    public static Spell SelectedSpell { get; set; } = WindSpirit; // ← add this

    public static Spell WindSpirit = new WindSpirit();
    public static Spell Heal = new Heal();
    public static Spell Fireball = new Fireball();
    public static Spell WaterSpirit = new WaterSpirit();
    public static Spell TreeSpirit { get; } = new TreeSpirit();
    public static Spell EarthSpirit { get; } = new EarthSpirit();

    public static List<Spell> Spells = new()
    {
        WindSpirit,
        Heal,
        Fireball,
        WaterSpirit,
        TreeSpirit,
        EarthSpirit
    };

    public static class PlayerData
    {
        public static bool IsSpellUnlocked(Spell spell)
        {
            // Always unlocked spells
            if (spell.Id == "nemo.WindSpirit" ||
                spell.Id == "nemo.Heal" ||
                spell.Id == "nemo.Fireball")
                return true;

            int level = ModEntry.MagicLevel;

            return (spell == WaterSpirit && level >= 2)
                || (spell == TreeSpirit && level >= 3)
                || (spell == EarthSpirit && level >= 4);
        }
    }
}
