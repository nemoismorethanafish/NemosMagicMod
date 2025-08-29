using HarmonyLib;
using MagicSkill;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod.Spells;
using SpaceCore;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private bool queuedWizardUpgrade = false;
        private Spellbook? queuedSpellbook = null;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            // LevelUpChanges, mana bar, etc.
            new LevelUpChanges(helper, Monitor);
            ManaManager.SetMaxMana(100);
            ManaManager.Refill();

            manaBar = new ManaBar(
                () => ManaManager.CurrentMana,
                () => ManaManager.MaxMana,
                10,
                10
            );
            manaBar.SubscribeToEvents(helper);

            Spellbook.LoadIcon(helper);
            MagicSkillIcon = Helper.ModContent.Load<Texture2D>("assets/magic-icon-smol.png");

            // === Event subscriptions ===
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Display.MenuChanged += OnMenuChanged;

            Monitor.Log("Mod loaded!", LogLevel.Info);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            // Spell selection menu key
            if (e.Button == SButton.D9)
                Game1.activeClickableMenu = new SpellSelectionMenu(Helper, Monitor);
        }
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is DialogueBox dialogue
                && Game1.currentSpeaker != null
                && Game1.currentSpeaker.Name == "Wizard")
            {
                Spellbook? spellbook = Game1.player.Items.OfType<Spellbook>().FirstOrDefault();
                if (spellbook != null)
                {
                    // Queue the upgrade, but do not open yet
                    queuedWizardUpgrade = true;
                    queuedSpellbook = spellbook;
                }
            }
        }
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            spaceCoreApi = Helper.ModRegistry.GetApi<ISpaceCoreApi>("spacechase0.SpaceCore");
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
            Helper.Events.GameLoop.UpdateTicked += GiveSpellbookOnceSafely;
            Helper.Events.GameLoop.UpdateTicked += RunUpdateMagicLevelOnce;
        }

        private void GiveSpellbookOnceSafely(object? sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= GiveSpellbookOnceSafely;

            var player = Game1.player;
            if (player == null) return;

            if (!PlayerHasSpellbookAnywhere(player))
            {
                player.addItemToInventory(new Spellbook());
                Monitor.Log("Added Spellbook to player's inventory.", LogLevel.Info);
            }
        }

        private bool PlayerHasSpellbookAnywhere(Farmer player)
        {
            if (player.Items.Any(item => item is Spellbook)) return true;

            foreach (GameLocation location in Game1.locations)
            {
                if (LocationHasSpellbook(location)) return true;

                foreach (var building in location.buildings)
                {
                    var interior = building.indoors.Value;
                    if (interior != null && LocationHasSpellbook(interior)) return true;
                }
            }

            var farmhouse = Game1.getLocationFromName("FarmHouse") as StardewValley.Locations.FarmHouse;
            if (farmhouse != null)
            {
                foreach (var obj in farmhouse.Objects.Values)
                {
                    if (obj is Chest chest && chest.fridge.Value && chest.Items.Any(i => i is Spellbook))
                        return true;
                }
            }

            return false;
        }

        private bool LocationHasSpellbook(GameLocation location)
        {
            foreach (var obj in location.Objects.Values)
            {
                if (obj is Chest chest && chest.Items.Any(i => i is Spellbook))
                    return true;
            }
            return false;
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            Helper.Data.WriteSaveData("player-save-data", SaveData);
        }

        private void RunUpdateMagicLevelOnce(object? sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= RunUpdateMagicLevelOnce;
            UpdateMagicLevel();
        }

        private void UpdateMagicLevel()
        {
            if (Game1.player == null) return;

            try
            {
                MagicLevel = Skills.GetSkillLevel(Game1.player, SkillID);
                Monitor.Log($"Magic Level updated: {MagicLevel}", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error updating MagicLevel: {ex}", LogLevel.Error);
            }
        }

        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // Update active spells
            for (int i = ActiveSpells.Count - 1; i >= 0; i--)
            {
                Spell spell = ActiveSpells[i];
                spell.Update(Game1.currentGameTime, Game1.player);
                if (!spell.IsActive)
                    ActiveSpells.RemoveAt(i);
            }

            // Handle queued wizard upgrade dialogue
            if (queuedWizardUpgrade)
            {
                if (!(Game1.activeClickableMenu is DialogueBox)) // Dialogue finished
                {
                    if (queuedSpellbook != null)
                        SpellbookUpgradeSystem.OfferWizardUpgrade(Game1.player, queuedSpellbook, Monitor);

                    queuedWizardUpgrade = false;
                    queuedSpellbook = null;
                }
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
                    GameStateQuery.Helpers.PlayerSkillLevelImpl(args, ctx.Player, f => f.GetCustomSkillLevel(skill))
                );
            }
            catch (InvalidOperationException) { }

            monitor.Log($"Skill '{skill.Id}' registered using SpaceCore API.", LogLevel.Info);
        }
    }
}
