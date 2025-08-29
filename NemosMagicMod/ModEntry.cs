using HarmonyLib;
using MagicSkill;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod.Spells;
using SpaceCore;
using SpaceShared.APIs;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using static SpaceCore.Skills;
using System.Reflection;

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

            new LevelUpChanges(helper, Monitor);

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
            // Delay until the next tick to ensure all locations/buildings are loaded
            Helper.Events.GameLoop.UpdateTicked += GiveSpellbookOnceSafely;

            // Schedule UpdateMagicLevel to run on next tick as well
            Helper.Events.GameLoop.UpdateTicked += RunUpdateMagicLevelOnce;
        }

        private void GiveSpellbookOnceSafely(object? sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= GiveSpellbookOnceSafely;

            var player = Game1.player;
            if (player == null)
                return;

            if (!PlayerHasSpellbookAnywhere(player))
            {
                player.addItemToInventory(new Spellbook());
                Monitor.Log("Added Spellbook to player's inventory.", LogLevel.Info);
            }
        }

        private bool PlayerHasSpellbookAnywhere(Farmer player)
        {
            // 1️⃣ Check inventory
            if (player.Items.Any(item => item is Spellbook))
            {
                Monitor.Log("📖 Spellbook found in player inventory.", LogLevel.Debug);
                return true;
            }

            // 2️⃣ Check all objects in all locations (including outdoor chests)
            foreach (GameLocation location in Game1.locations)
            {
                if (LocationHasSpellbook(location))
                {
                    Monitor.Log($"📖 Spellbook found in location: {location.Name}.", LogLevel.Debug);
                    return true;
                }

                // Check building interiors (barns, coops, sheds, etc.)
                foreach (var building in location.buildings)
                {
                    var interior = building.indoors.Value;
                    if (interior != null && LocationHasSpellbook(interior))
                    {
                        Monitor.Log($"📖 Spellbook found inside building: {building.buildingType.Value} ({location.Name}).", LogLevel.Debug);
                        return true;
                    }
                }
            }

            // 3️⃣ Check farmhouse fridge explicitly
            var farmhouse = Game1.getLocationFromName("FarmHouse") as StardewValley.Locations.FarmHouse;
            if (farmhouse != null)
            {
                foreach (var obj in farmhouse.Objects.Values)
                {
                    if (obj is Chest chest && chest.fridge.Value)
                    {
                        if (chest.Items != null && chest.Items.Any(i => i is Spellbook))
                        {
                            Monitor.Log("📖 Spellbook found in farmhouse fridge.", LogLevel.Debug);
                            return true;
                        }
                    }
                }
            }

            Monitor.Log("❌ No Spellbook found anywhere.", LogLevel.Debug);
            return false;
        }

        private bool LocationHasSpellbook(GameLocation location)
        {
            foreach (var obj in location.Objects.Values)
            {
                if (obj is Chest chest)
                {
                    if (chest.Items != null && chest.Items.Any(i => i is Spellbook))
                        return true;
                }
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
                Game1.activeClickableMenu = new SpellSelectionMenu(this.Helper, this.Monitor);
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
