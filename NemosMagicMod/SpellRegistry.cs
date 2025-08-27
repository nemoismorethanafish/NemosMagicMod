using NemosMagicMod;
using NemosMagicMod.Spells;
using System.Collections.Generic;

public static class SpellRegistry //List of all spells
{
    public static Spell WindSpirit = new WindSpirit();
    public static Spell Heal = new Heal();
    public static Spell Fireball = new Fireball();
    public static Spell WaterSpirit = new WaterSpirit();


    // Add this list so the menu can render all spells
    public static readonly List<Spell> Spells = new()
    {
        WindSpirit,
        Heal,
        Fireball,
        WaterSpirit
    };

    public static Spell SelectedSpell = WindSpirit;

    public static class PlayerData
    {
        public static HashSet<string> UnlockedSpellIds => ModEntry.SaveData.UnlockedSpellIds;

        public static bool IsSpellUnlocked(Spell spell)
        {
            if (spell.Id == "nemo.WindSpirit" ||
                spell.Id == "nemo.Heal" ||
                spell.Id == "nemo.Fireball")
            {
                return true;
            }

            return UnlockedSpellIds.Contains(spell.Id);
        }
    }



}
