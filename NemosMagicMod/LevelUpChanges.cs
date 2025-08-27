using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace NemosMagicMod
{
    internal class LevelUpChanges
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;
        private bool checkedUnlock = false;

        public LevelUpChanges(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            // Wait until the world is ready and magic level is stable
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || checkedUnlock)
                return;

            // Only proceed once magic level has stabilized
            if (ModEntry.MagicLevel >= 2)
            {
                var spell = SpellRegistry.WaterSpirit;

                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                    Monitor.Log($"✅ {spell.Name} spell unlocked at MagicLevel {ModEntry.MagicLevel}!", LogLevel.Info);
                }

                checkedUnlock = true; // Only do this once
            }

            if (ModEntry.MagicLevel >= 3)
            {
                var spell = SpellRegistry.TreeSpirit;

                if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(spell.Id))
                        {
                    SpellRegistry.PlayerData.UnlockedSpellIds.Add(spell.Id);
                    Monitor.Log("{ spell.Name} spell unlocked at MagicLevel { ModEntry.MagicLevel}!", LogLevel.Info);
                }

                checkedUnlock = true;
            }
        }

    }
}
