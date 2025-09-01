using MagicSkill;
using SpaceCore;
using StardewValley;
using System;
using System.Linq;

namespace NemosMagicMod
{
    public static class ManaManager
    {
        private static int _currentMana = 100;
        private static int _maxMana = 100;

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

        // Add this method to recalculate mana based on current state
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
    }
}