using NemosMagicMod.Spells;
using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.Linq;
using static SpaceCore.Skills;

namespace NemosMagicMod
{
    internal class LevelUpChanges
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        private bool checkedUnlockLevel1 = false;
        private bool checkedUnlockLevel2 = false;
        private bool checkedUnlockLevel3 = false;
        private bool checkedUnlockLevel4 = false;
        private bool checkedUnlockLevel6 = false;
        private bool checkedUnlockLevel7 = false;
        private bool checkedUnlockLevel9 = false;

        public LevelUpChanges(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            // Wait until the world is ready and magic level is stable
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private int GetProfessionId(string skill, string profession)
        {
            return Skills.GetSkill(skill).Professions
                         .Single(p => p.Id == profession)
                         .GetVanillaId();
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (ModEntry.MagicLevel >= 1 && !checkedUnlockLevel1)
            {
                try
                {
                    int battleMageID = GetProfessionId(ModEntry.SkillID, "BattleMage");
                    bool hasBattleMage = battleMageID != -1 && Game1.player.professions.Contains(battleMageID);

                    if (!hasBattleMage)
                    {
                        // Unlock regular Fireball
                        var spell = SpellRegistry.Fireball;
                        if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                        {
                            SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                            Monitor.Log($"{spell.Name} spell unlocked at MagicLevel {ModEntry.MagicLevel}!", LogLevel.Info);
                        }

                        // Make sure FireballCantrip is NOT unlocked
                        SpellRegistry.PlayerData.UnlockedSpellIds.Remove(SpellRegistry.FireballCantrip.Id);
                    }
                    else
                    {
                        // Player has Battle Mage - ensure only FireballCantrip is unlocked
                        var cantripSpell = SpellRegistry.FireballCantrip;
                        if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(cantripSpell.Id))
                        {
                            SpellRegistry.PlayerData.UnlockedSpellIds.Add(cantripSpell.Id);
                            Monitor.Log($"{cantripSpell.Name} spell unlocked via Battle Mage profession!", LogLevel.Info);
                        }

                        // Make sure regular Fireball is NOT unlocked
                        SpellRegistry.PlayerData.UnlockedSpellIds.Remove(SpellRegistry.Fireball.Id);
                    }
                }
                catch (System.Exception ex)
                {
                    Monitor.Log($"Error in level 1 spell unlock: {ex.Message}", LogLevel.Error);
                }

                checkedUnlockLevel1 = true;
            }
            if (ModEntry.MagicLevel >= 2 && !checkedUnlockLevel2)
            {
                var spell = SpellRegistry.WaterSpirit;

                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                    Monitor.Log($"{spell.Name} spell unlocked at MagicLevel {ModEntry.MagicLevel}!", LogLevel.Info);
                }

                checkedUnlockLevel2 = true;
            }

            if (ModEntry.MagicLevel >= 3 && !checkedUnlockLevel3)
            {
                var spell = SpellRegistry.TreeSpirit;
                var spell2 = SpellRegistry.EarthSpirit;

                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                    Monitor.Log($"{spell.Name} spell unlocked at MagicLevel {ModEntry.MagicLevel}!", LogLevel.Info);
                }

                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell2.Id))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell2.Id);
                    Monitor.Log($"{spell2.Name} spell unlocked at MagicLevel {ModEntry.MagicLevel}!", LogLevel.Info);
                }

                checkedUnlockLevel3 = true;
            }

            if (ModEntry.MagicLevel >= 4 && !checkedUnlockLevel4)
            {
                var spell = SpellRegistry.SeaSpirit;

                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                    Monitor.Log($"{spell.Name} spell unlocked at MagicLevel {ModEntry.MagicLevel}!", LogLevel.Info);
                }

                checkedUnlockLevel4 = true;
            }

            if (ModEntry.MagicLevel >= 6 && !checkedUnlockLevel6)
            {
                var spell = SpellRegistry.HomeWarp;

                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                    Monitor.Log($"{spell.Name} spell unlocked at MagicLevel {ModEntry.MagicLevel}!", LogLevel.Info);
                }

                checkedUnlockLevel6 = true;
            }

            if (ModEntry.MagicLevel >= 7 && !checkedUnlockLevel7)
            {
                var spell = SpellRegistry.TimeWarp;

                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                    Monitor.Log($"{spell.Name} spell unlocked at MagicLevel {ModEntry.MagicLevel}!", LogLevel.Info);
                }

                checkedUnlockLevel7 = true;
            }

            if (ModEntry.MagicLevel >= 9 && !checkedUnlockLevel9)
            {
                var spell = SpellRegistry.FertilitySpirit;

                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                    Monitor.Log($"{spell.Name} spell unlocked at MagicLevel {ModEntry.MagicLevel}!", LogLevel.Info);
                }

                checkedUnlockLevel9 = true;
            }
        }
    }
}