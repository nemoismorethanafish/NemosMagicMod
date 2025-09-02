using NemosMagicMod.Spells;
using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;
using System.Collections.Generic;
using static SpaceCore.Skills;

namespace NemosMagicMod
{
    internal class LevelUpChanges
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        private int lastCheckedLevel = 0;
        private HashSet<string> pendingSpellUnlocks = new HashSet<string>();

        public LevelUpChanges(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            // Subscribe to level-up events
            helper.Events.Player.LevelChanged += OnLevelChanged;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnLevelChanged(object? sender, LevelChangedEventArgs e)
        {
            // Check if Magic skill leveled up
            if (e.Skill.ToString() == "nemosmagicmod.Magic")
            {
                // Queue spell unlocks for this level
                var spellsToUnlock = GetSpellsForLevel(e.NewLevel);
                foreach (var spell in spellsToUnlock)
                {
                    pendingSpellUnlocks.Add(spell.Id);
                }

                Monitor.Log($"Magic skill leveled up to {e.NewLevel}! Queued {spellsToUnlock.Count} spells for unlock.", LogLevel.Info);
            }
        }

        private int GetProfessionId(string skill, string profession)
        {
            try
            {
                return Skills.GetSkill(skill).Professions
                             .Single(p => p.Id == profession)
                             .GetVanillaId();
            }
            catch
            {
                return -1;
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // Process any pending spell unlocks
            if (pendingSpellUnlocks.Count > 0)
            {
                foreach (var spellId in pendingSpellUnlocks.ToList())
                {
                    if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spellId))
                    {
                        SpellRegistry.PlayerData.UnlockedSpellIds.Add(spellId);
                        Monitor.Log($"Spell with ID {spellId} unlocked!", LogLevel.Info);
                    }
                    pendingSpellUnlocks.Remove(spellId);
                }
            }

            // Check for level changes and unlock spells accordingly
            if (ModEntry.MagicLevel > lastCheckedLevel)
            {
                for (int level = lastCheckedLevel + 1; level <= ModEntry.MagicLevel; level++)
                {
                    UnlockSpellsForLevel(level);
                }
                lastCheckedLevel = ModEntry.MagicLevel;
            }
        }

        private void UnlockSpellsForLevel(int level)
        {
            var spells = GetSpellsForLevel(level);

            foreach (var spell in spells)
            {
                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                    Monitor.Log($"{spell.Name} spell unlocked at Magic Level {level}!", LogLevel.Info);
                }
            }
        }

        private List<Spell> GetSpellsForLevel(int level)
        {
            var spells = new List<Spell>();

            try
            {
                // Level 1 - Fireball (depends on profession)
                if (level == 1)
                {
                    int battleMageID = GetProfessionId(ModEntry.SkillID, "BattleMage");
                    bool hasBattleMage = battleMageID != -1 && Game1.player.professions.Contains(battleMageID);

                    if (hasBattleMage)
                    {
                        spells.Add(SpellRegistry.FireballCantrip);
                        // Remove regular Fireball if it was unlocked
                        SpellRegistry.PlayerData.UnlockedSpellIds.Remove(SpellRegistry.Fireball.Id);
                    }
                    else
                    {
                        spells.Add(SpellRegistry.Fireball);
                        // Remove FireballCantrip if it was unlocked
                        SpellRegistry.PlayerData.UnlockedSpellIds.Remove(SpellRegistry.FireballCantrip.Id);
                    }
                }
                // Level 2 - Heal
                else if (level == 2)
                {
                    spells.Add(SpellRegistry.Heal);
                }
                // Level 3 - TreeSpirit and EarthSpirit
                else if (level == 3)
                {
                    spells.Add(SpellRegistry.TreeSpirit);
                    spells.Add(SpellRegistry.EarthSpirit);
                }
                // Level 4 - SeaSpirit
                else if (level == 4)
                {
                    spells.Add(SpellRegistry.SeaSpirit);
                }
                // Level 6 - HomeWarp
                else if (level == 6)
                {
                    spells.Add(SpellRegistry.HomeWarp);
                }
                // Level 7 - TimeWarp
                else if (level == 7)
                {
                    spells.Add(SpellRegistry.TimeWarp);
                }
                // Level 9 - FertilitySpirit
                else if (level == 9)
                {
                    spells.Add(SpellRegistry.FertilitySpirit);
                }
            }
            catch (System.Exception ex)
            {
                Monitor.Log($"Error getting spells for level {level}: {ex.Message}", LogLevel.Error);
            }

            return spells;
        }
    }
}