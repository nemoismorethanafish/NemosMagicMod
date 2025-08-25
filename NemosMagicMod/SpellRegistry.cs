using System.Collections.Generic;
using NemosMagicMod.Spells;

public static class SpellRegistry
{
    public static readonly WindSpirit WindSpirit = new WindSpirit();
    public static readonly Heal Heal = new Heal();
    public static readonly Fireball Fireball = new Fireball();

    // Add this list so the menu can render all spells
    public static readonly List<Spell> Spells = new()
    {
        WindSpirit,
        Heal,
        Fireball
    };

    public static Spell SelectedSpell = WindSpirit;
}
