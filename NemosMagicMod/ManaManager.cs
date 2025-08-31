using MagicSkill;
using SpaceCore;
using StardewValley;
using System;

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
    }
}
