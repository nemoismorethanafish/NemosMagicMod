using MagicSkill;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Linq;

namespace NemosMagicMod
{
    public static class ManaManager
    {
        private static int _currentMana = 100;
        private static int _maxMana = 100;
        private static double _manaRegenAccumulator = 0.0;

        public static int CurrentMana => _currentMana;
        public static int MaxMana => _maxMana;

        public static bool HasEnoughMana(int amount) => _currentMana >= amount;

        public static void SpendMana(int amount)
        {
            _currentMana = Math.Max(0, _currentMana - amount);
        }

        public static void SetMaxMana(int amount)
        {
            _maxMana = amount;
            _currentMana = Math.Min(_currentMana, _maxMana);
        }

        public static void RestoreMana(int amount)
        {
            _currentMana = Math.Min(_maxMana, _currentMana + amount);
        }

        public static void Refill()
        {
            _currentMana = _maxMana;
        }

        /// <summary>
        /// Recalculates max mana based on current magic level and professions
        /// </summary>
        public static void RecalculateMaxMana()
        {
            if (Game1.player == null) return;

            int baseMana = 100;
            int totalMana = baseMana + 5 * ModEntry.MagicLevel;

            // Check for ManaDork profession
            try
            {
                int manaDorkID = Skills.GetSkill(ModEntry.SkillID).Professions
                    .Single(p => p.Id == "ManaDork")
                    .GetVanillaId();
                if (Game1.player.professions.Contains(manaDorkID))
                {
                    totalMana += 50;
                }
            }
            catch (Exception)
            {
                // Profession not found or other error - continue without bonus
            }

            SetMaxMana(totalMana);
        }

        /// <summary>
        /// Handles mana regeneration per game tick
        /// </summary>
        public static void UpdateManaRegeneration(ModConfig config, bool skillSystemReady, IMonitor monitor)
        {
            if (Game1.player == null) return;

            // Calculate base mana regeneration
            double manaPerSecond = 0.1 * ModEntry.MagicLevel + 0.1; // natural regen

            // Only try to get profession bonuses if SpaceCore is working
            if (skillSystemReady)
            {
                try
                {
                    int manaRegenId = Skills.GetSkill(ModEntry.SkillID).Professions
                        .Single(p => p.Id == "ManaRegeneration")
                        .GetVanillaId();
                    if (Game1.player.professions.Contains(manaRegenId))
                        manaPerSecond += 1.0; // extra 1 mana/sec from profession
                }
                catch (Exception ex)
                {
                    monitor.Log($"Error getting mana regen profession: {ex.Message}", LogLevel.Warn);
                }
            }

            if (config.godMode)
                manaPerSecond += 50.0;

            double manaPerTick = manaPerSecond / 60.0; // SMAPI: 60 ticks/sec
            _manaRegenAccumulator += manaPerTick;

            // Apply accumulated mana when it reaches 1 or more
            int restoreAmount = (int)Math.Floor(_manaRegenAccumulator);
            if (restoreAmount > 0)
            {
                RestoreMana(restoreAmount);
                _manaRegenAccumulator -= restoreAmount;
            }
        }

        /// <summary>
        /// Handles end-of-day mana processing (experience gain and refill)
        /// </summary>
        public static void ProcessDayEnd()
        {
            if (Game1.player == null) return;

            int leftoverMana = CurrentMana;
            int leftoverManaXP = leftoverMana / 10;

            // Add XP to custom Magic skill using SpaceCore helper
            try
            {
                Skills.AddExperience(Game1.player, ModEntry.SkillID, leftoverManaXP);
            }
            catch (Exception ex)
            {
                ModEntry.Instance?.Monitor?.Log($"Error adding mana XP: {ex.Message}", LogLevel.Error);
            }

            // Refill mana for next day
            Refill();
        }

        /// <summary>
        /// Sets up mana for day start based on current skill level and professions
        /// </summary>
        public static void SetupDayStartMana(bool skillSystemReady, IMonitor monitor)
        {
            if (Game1.player == null) return;

            if (skillSystemReady)
            {
                try
                {
                    // Update Magic level
                    ModEntry.MagicLevel = Skills.GetSkillLevel(Game1.player, ModEntry.SkillID);
                    monitor.Log($"Magic Level updated: {ModEntry.MagicLevel}", LogLevel.Debug);

                    // Recalculate max mana with skill bonuses
                    RecalculateMaxMana();
                    monitor.Log($"Max Mana after day start setup: {MaxMana}", LogLevel.Debug);
                }
                catch (Exception ex)
                {
                    monitor.Log($"Error in SetupDayStartMana: {ex.Message}", LogLevel.Error);
                    // Fallback to basic setup
                    SetBasicMana();
                }
            }
            else
            {
                SetBasicMana();
            }

            Refill();
        }

        /// <summary>
        /// Sets basic mana without skill bonuses (fallback)
        /// </summary>
        public static void SetBasicMana()
        {
            SetMaxMana(100);
        }
    }
}