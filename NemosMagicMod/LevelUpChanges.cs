using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace NemosMagicMod
{
    internal class LevelUpChanges
    {
        private readonly IMonitor Monitor;

        public LevelUpChanges(IModHelper helper, IMonitor monitor)
        {
            Monitor = monitor;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (ModEntry.MagicLevel >= 2)
            {
                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains("nemo.WaterSpirit"))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add("Water Spirit");
                    Monitor.Log("Water Spirit spell unlocked on day start!", LogLevel.Info);
                }
            }
        }
    }
}
