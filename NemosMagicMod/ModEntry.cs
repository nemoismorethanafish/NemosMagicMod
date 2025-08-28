using MagicSkill;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Linq;
using System.Collections.Generic;
using static SpaceCore.Skills;

namespace NemosMagicMod
{
    public interface ISpaceCoreApi
    {
        void RegisterSerializerType(Type type);
    }

    public class ModEntry : Mod
    {
        private ISpaceCoreApi? spaceCoreApi;

        public const string SkillID = "nemosmagicmod.Magic";
        public static ModEntry Instance { get; private set; } = null!;
        public static Texture2D MagicSkillIcon { get; private set; } = null!;
        public static int MagicLevel = 0;

        private ManaBar manaBar = null!;

        public static PlayerSaveData SaveData = new();

        // === Active Spell Tracking ===
        public static readonly List<Spell> ActiveSpells = new();

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            // Setup mana bar etc.
            ManaManager.SetMaxMana(100);
            ManaManager.Refill();

            manaBar = new ManaBar(
                () => ManaManager.CurrentMana,
                () => ManaManager.MaxMana,
                10,
                10);
            manaBar.SubscribeToEvents(helper);

            Spellbook.LoadIcon(helper);
            MagicSkillIcon = Helper.ModContent.Load<Texture2D>("assets/magic-icon-smol.png");

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            Monitor.Log("Mod loaded!", LogLevel.Info);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var spaceCoreApi = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
            if (spaceCoreApi == null)
            {
                Monitor.Log("Could not get SpaceCore API. Is it installed correctly?", LogLevel.Error);
                return;
            }

            spaceCoreApi.RegisterSerializerType(typeof(Spellbook));
            Monitor.Log("Registered Spellbook with SpaceCore serializer.", LogLevel.Info);

            SkillRegistrar.Register(new Magic_Skill(), Monitor);
            Monitor.Log("Registered Magic skill during GameLaunched.", LogLevel.Info);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            SaveData = Helper.Data.ReadSaveData<PlayerSaveData>("player-save-data") ?? new PlayerSaveData();
            Monitor.Log("✅ Save data loaded.", LogLevel.Info);
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            var player = Game1.player;
            if (player == null || player.Items == null)
                return;

            bool hasSpellbook = player.Items.Any(item => item is Spellbook);
            if (!hasSpellbook)
            {
                player.addItemToInventory(new Spellbook());
                Monitor.Log("Added Spellbook to player's inventory.", LogLevel.Info);
            }

            // Schedule UpdateMagicLevel to run on next tick to ensure SpaceCore is ready
            Helper.Events.GameLoop.UpdateTicked += RunUpdateMagicLevelOnce;
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            Helper.Data.WriteSaveData("player-save-data", SaveData);
        }

        private void RunUpdateMagicLevelOnce(object? sender, UpdateTickedEventArgs e)
        {
            // Unsubscribe immediately so it only runs once
            Helper.Events.GameLoop.UpdateTicked -= RunUpdateMagicLevelOnce;
            UpdateMagicLevel();
        }

        private void UpdateMagicLevel()
        {
            try
            {
                if (Game1.player == null)
                {
                    Monitor.Log("Player is null, cannot update MagicLevel.", LogLevel.Warn);
                    return;
                }

                MagicLevel = Skills.GetSkillLevel(Game1.player, SkillID);
                Monitor.Log($"Magic Level updated: {MagicLevel}", LogLevel.Debug);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Monitor.Log($"Caught ArgumentOutOfRangeException in UpdateMagicLevel: {ex.Message}", LogLevel.Error);
                // Could be SpaceCore skill list not ready yet, skip update this time
            }
            catch (Exception ex)
            {
                Monitor.Log($"Unexpected exception in UpdateMagicLevel: {ex}", LogLevel.Error);
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            if (e.Button == SButton.D9)
            {
                Game1.activeClickableMenu = new SpellSelectionMenu(this.Helper, this.Monitor);
            }
        }

        // === Active Spell Management ===
        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            for (int i = ActiveSpells.Count - 1; i >= 0; i--)
            {
                Spell spell = ActiveSpells[i];
                spell.Update(Game1.currentGameTime, Game1.player);

                if (!spell.IsActive)
                    ActiveSpells.RemoveAt(i);
            }
        }

        public static void RegisterActiveSpell(Spell spell)
        {
            ActiveSpells.Add(spell);
            Instance.Monitor.Log($"Registered active spell: {spell.Name}", LogLevel.Trace);
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
}
