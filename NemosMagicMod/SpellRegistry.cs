using System.Collections.Generic;
using NemosMagicMod.Spells;

public static class SpellRegistry //List of all spells
{
    public static Spell WindSpirit = new WindSpirit();
    public static Spell Heal = new Heal();
    public static Spell Fireball = new Fireball();

    // Add this list so the menu can render all spells
    public static readonly List<Spell> Spells = new()
    {
        WindSpirit,
        Heal,
        Fireball
    };

    public static Spell SelectedSpell = WindSpirit;

    public static class PlayerData
    {
        // Assume this keeps track of unlocked spells by their unique IDs or names
        public static HashSet<string> UnlockedSpellIds = new HashSet<string>();

        public static bool IsSpellUnlocked(Spell spell)
        {
            return spell.IsSpellUnlocked;
        }
    }

}
