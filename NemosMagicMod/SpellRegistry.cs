using NemosMagicMod;
using NemosMagicMod.Spells;
using System.Collections.Generic;

public static class SpellRegistry // List of all spells
{
    public static Spell SelectedSpell = WindSpirit;

    public static Spell WindSpirit = new WindSpirit();
    public static Spell Heal = new Heal();
    public static Spell Fireball = new Fireball();
    public static Spell FireballCantrip { get; } = new FireballCantrip();

    public static Spell WaterSpirit = new WaterSpirit();
    public static Spell TreeSpirit { get; } = new TreeSpirit();
    public static Spell EarthSpirit { get; } = new EarthSpirit();

    public static Spell SeaSpirit { get; } = new SeaSpirit();
    public static Spell TimeWarp { get; } = new TimeWarp();
    public static Spell HomeWarp { get; } = new HomeWarp();
    public static Spell FertilitySpirit { get; } = new FertilitySpirit();
    public static Spell FireCyclone { get; } = new FireCyclone();

    // Add this list so the menu can render all spells
    public static List<Spell> Spells = new()
    {
        WindSpirit,
        Heal,
        Fireball,
        FireballCantrip,
        WaterSpirit,
        TreeSpirit,
        EarthSpirit,
        SeaSpirit,
        TimeWarp,
        HomeWarp,
        FertilitySpirit,
        FireCyclone
    };

    public static class PlayerData
    {
        public static HashSet<string> UnlockedSpellIds => ModEntry.SaveData.UnlockedSpellIds;

        // Updated to require ModConfig
        public static bool IsSpellUnlocked(Spell spell, ModConfig config)
        {
            // God Mode unlocks everything
            if (config.godMode)
                return true;

            // Always unlocked
            if (spell.Id == "nemo.WindSpirit" ||
                spell.Id == "nemo.Heal")
                return true;

            return UnlockedSpellIds.Contains(spell.Id);
        }
    }
}
