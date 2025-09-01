using NemosMagicMod;
using NemosMagicMod.Spells;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using MagicSkill;
using SpaceCore;
using Microsoft.Xna.Framework;

public static class SpellRegistry // List of all spells
{
    private static readonly List<Spell> _activeSpells = new();

    public static Spell SelectedSpell { get; set; } = WindSpirit;
    public static Spell WindSpirit { get; } = new WindSpirit();
    public static Spell Heal { get; } = new Heal();
    public static Spell Fireball { get; } = new Fireball();
    public static Spell FireballCantrip { get; } = new FireballCantrip();
    public static Spell WaterSpirit { get; } = new WaterSpirit();
    public static Spell TreeSpirit { get; } = new TreeSpirit();
    public static Spell EarthSpirit { get; } = new EarthSpirit();
    public static Spell SeaSpirit { get; } = new SeaSpirit();
    public static Spell TimeWarp { get; } = new TimeWarp();
    public static Spell HomeWarp { get; } = new HomeWarp();
    public static Spell FertilitySpirit { get; } = new FertilitySpirit();
    public static Spell FireCyclone { get; } = new FireCyclone();

    // Add this list so the menu can render all spells
    public static List<Spell> Spells { get; } = new()
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

    public static IReadOnlyList<Spell> ActiveSpells => _activeSpells.AsReadOnly();

    /// <summary>
    /// Registers a spell as active and manages its lifecycle
    /// </summary>
    public static void RegisterActiveSpell(Spell spell)
    {
        _activeSpells.Add(spell);
        ModEntry.Instance?.Monitor?.Log($"Registered active spell: {spell.Name}", LogLevel.Trace);
    }

    /// <summary>
    /// Updates all active spells and removes inactive ones
    /// </summary>
    public static void UpdateActiveSpells(GameTime gameTime, Farmer player)
    {
        for (int i = _activeSpells.Count - 1; i >= 0; i--)
        {
            Spell spell = _activeSpells[i];
            spell.Update(gameTime, player);
            if (!spell.IsActive)
            {
                ModEntry.Instance?.Monitor?.Log($"Removing inactive spell: {spell.Name}", LogLevel.Trace);
                _activeSpells.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Casts a spell by hotkey if available
    /// </summary>
    public static bool TryHotkeyCast(Farmer player, string? hotkeyId)
    {
        if (string.IsNullOrEmpty(hotkeyId))
        {
            Game1.showRedMessage("No hotkeyed spell assigned.");
            return false;
        }

        var spell = Spells.FirstOrDefault(s => s.Id == hotkeyId);
        if (spell == null)
        {
            Game1.showRedMessage("Hotkeyed spell not found.");
            return false;
        }

        var spellbook = player.Items.OfType<Spellbook>().FirstOrDefault();
        if (spellbook == null || !player.Items.Contains(spellbook))
        {
            Game1.showRedMessage("You need the Spellbook in your inventory to cast this spell.");
            return false;
        }

        spell.Cast(player);
        Game1.playSound("coin");
        Game1.addHUDMessage(new HUDMessage($"Cast {spell.Name} via Hotkey!", HUDMessage.newQuest_type));
        return true;
    }

    /// <summary>
    /// Cleans up conflicting spells based on profession status
    /// </summary>
    public static void CleanupConflictingSpells(IMonitor monitor)
    {
        if (Game1.player == null) return;

        try
        {
            int battleMageID = Skills.GetSkill(ModEntry.SkillID).Professions
                .Single(p => p.Id == "BattleMage")
                .GetVanillaId();
            bool hasBattleMage = Game1.player.professions.Contains(battleMageID);

            var saveData = ModEntry.SaveData;
            bool hasFireball = saveData.UnlockedSpellIds.Contains(Fireball.Id);
            bool hasCantrip = saveData.UnlockedSpellIds.Contains(FireballCantrip.Id);

            if (hasFireball && hasCantrip)
            {
                monitor.Log("Found conflicting Fireball spells, cleaning up...", LogLevel.Info);

                if (hasBattleMage)
                {
                    saveData.UnlockedSpellIds.Remove(Fireball.Id);
                    monitor.Log("Removed regular Fireball (player has Battle Mage)", LogLevel.Info);
                }
                else
                {
                    saveData.UnlockedSpellIds.Remove(FireballCantrip.Id);
                    monitor.Log("Removed FireballCantrip (player doesn't have Battle Mage)", LogLevel.Info);
                }
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"Error cleaning up conflicting spells: {ex.Message}", LogLevel.Error);
        }
    }

    public static class PlayerData
    {
        public static HashSet<string> UnlockedSpellIds => ModEntry.SaveData.UnlockedSpellIds;

        /// <summary>
        /// Checks if a spell is unlocked for the current player
        /// </summary>
        public static bool IsSpellUnlocked(Spell spell, ModConfig config)
        {
            // God Mode unlocks everything
            if (config.godMode)
                return true;

            // Always unlocked spells
            if (spell.Id == "nemo.WindSpirit" || spell.Id == "nemo.WaterSpirit")
                return true;

            return UnlockedSpellIds.Contains(spell.Id);
        }
    }
}