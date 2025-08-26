using MagicSkill;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using System;
using static SpaceCore.Skills;

namespace NemosMagicMod
{
    public class ModEntry : Mod
    {
        public static ModEntry Instance { get; private set; } = null!;

        private ManaBar manaBar = null!;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            manaBar = new ManaBar(() => 50, () => 100, 10, 10);
            manaBar.SubscribeToEvents(helper);

            // Load the spellbook icon texture once on mod entry
            Spellbook.LoadIcon(helper);

            // Register Magic skill (uncommented as requested)
            SkillRegistrar.Register(new Magic_Skill(), Monitor);

            // Hook SaveLoaded event so we can add the spellbook when a save is loaded
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Input.ButtonPressed += OnButtonPressed;


            Monitor.Log("Mod loaded!", LogLevel.Info);
        }

        // This runs after a save is loaded, player is guaranteed to exist
        private void OnSaveLoaded(object? sender, StardewModdingAPI.Events.SaveLoadedEventArgs e)
        {
            var player = Game1.player;

            if (player == null)
            {
                Monitor.Log("Player not found on save loaded, skipping spellbook add.", LogLevel.Warn);
                return;
            }

            // Avoid adding duplicates
            bool hasSpellbook = false;
            foreach (var item in player.Items)
            {
                if (item is Spellbook)
                {
                    hasSpellbook = true;
                    break;
                }
            }

            if (!hasSpellbook)
            {
                player.addItemToInventory(new Spellbook());
                Monitor.Log("Added Spellbook to player's inventory.", LogLevel.Info);
            }
            else
            {
                Monitor.Log("Player already has a Spellbook, not adding.", LogLevel.Trace);
            }
        }
        private void OnButtonPressed(object? sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree) // only allow when player isn't in a cutscene or menu
                return;

            // Use the number 9 key
            if (e.Button == SButton.D9)
            {
                Game1.activeClickableMenu = new SpellSelectionMenu(this.Helper);
            }
        }

    }
}

public static class SkillRegistrar
{
    public static void Register(Skill skill, IMonitor monitor)
    {
        Skills.RegisterSkill(skill);

        string queryKey = "PLAYER_" + skill.Id.ToUpper() + "_LEVEL";

        try
        {
            GameStateQuery.Register(queryKey, (args, ctx) =>
            {
                return GameStateQuery.Helpers.PlayerSkillLevelImpl(args, ctx.Player, f => f.GetCustomSkillLevel(skill));
            });

            monitor.Log($"GameStateQuery '{queryKey}' registered.", LogLevel.Info);
        }
        catch (InvalidOperationException)
        {
            monitor.Log($"GameStateQuery '{queryKey}' already registered, skipping.", LogLevel.Trace);
        }

        monitor.Log($"Skill '{skill.Id}' registered using SpaceCore API.", LogLevel.Info);
    }

}
