using NemosMagicMod;
using NemosMagicMod.Spells;
using System.Collections.Generic;

public static class SpellRegistry //List of all spells
{
    public static Spell SelectedSpell = WindSpirit;

    public static Spell WindSpirit = new WindSpirit();
    public static Spell Heal = new Heal();
    public static Spell Fireball = new Fireball();
    public static Spell WaterSpirit = new WaterSpirit();
    public static Spell TreeSpirit { get; } = new TreeSpirit();
    public static Spell EarthSpirit { get; } = new EarthSpirit();



    // Add this list so the menu can render all spells
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
        public static HashSet<string> UnlockedSpellIds => ModEntry.SaveData.UnlockedSpellIds;

        public static bool IsSpellUnlocked(Spell spell)
        {
            if (spell.Id == "nemo.WindSpirit" ||
                spell.Id == "nemo.Heal" ||
                spell.Id == "nemo.Fireball" ||

                spell.Id == "nemo.EarthSpirit")
            {
                return true;
            }

            return UnlockedSpellIds.Contains(spell.Id);
        }
    }



}
