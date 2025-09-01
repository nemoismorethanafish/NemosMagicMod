using HarmonyLib;
using MagicSkill;
using Microsoft.Xna.Framework;
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
using static Level5Professions;
using static NemosMagicMod.Spells.SpellbookUpgradeSystem;
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
        public ModConfig Config { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            // LevelUpChanges, mana bar, etc.
            new LevelUpChanges(helper, Monitor);
            ManaManager.SetMaxMana(ManaManager.MaxMana);
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

            Config = helper.ReadConfig<ModConfig>();
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsPlayerFree)
                return;

            var player = Game1.player;

            // Spell selection menu key
            if (e.Button == Config.SpellSelectionKey)
            {
                Spellbook? spellbook = player.Items.OfType<Spellbook>().FirstOrDefault();
                if (spellbook != null)
                {
                    Game1.activeClickableMenu = new SpellSelectionMenu(Helper, Monitor, spellbook);
                }
                else
                {
                    Game1.showRedMessage("You don't have a Spellbook!");
                }
                return;
            }
        }

        private void TriggerBookAnimation(Farmer player)
        {
            if (player == null)
                return;

            // Create your custom book
            var customBook = new CustomBook();

            // Use reflection to call the readBook method for the full animation
            var method = typeof(StardewValley.Object).GetMethod(
                "readBook",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );

            if (method != null)
            {
                // Backup experience
                int[] expBackup = new int[player.experiencePoints.Count];
                player.experiencePoints.CopyTo(expBackup, 0);

                // Trigger animation
                method.Invoke(customBook, new object[] { player.currentLocation });

                // Restore experience
                for (int i = 0; i < expBackup.Length; i++)
                {
                    player.experiencePoints[i] = expBackup[i];
                }
            }
            else
            {
                Monitor.Log("Failed to find Object.readBook via reflection.", LogLevel.Warn);
            }
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
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

            var gmcm = Helper.ModRegistry.GetApi<SpaceShared.APIs.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm != null)
            {
                Config.RegisterGMCM(Helper, gmcm, ModManifest);
                Monitor.Log("GMCM options registered.", LogLevel.Info);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            SaveData = Helper.Data.ReadSaveData<PlayerSaveData>("player-save-data") ?? new PlayerSaveData();
            Monitor.Log("✅ Save data loaded.", LogLevel.Info);

            // Clean up any conflicting spells after loading
            Helper.Events.GameLoop.UpdateTicked += CleanupConflictingSpells;
        }

        private void CleanupConflictingSpells(object? sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= CleanupConflictingSpells;

            if (Game1.player == null) return;

            try
            {
                int battleMageID = GetProfessionId(SkillID, "BattleMage");
                bool hasBattleMage = Game1.player.professions.Contains(battleMageID);

                bool hasFireball = SaveData.UnlockedSpellIds.Contains(SpellRegistry.Fireball.Id);
                bool hasCantrip = SaveData.UnlockedSpellIds.Contains(SpellRegistry.FireballCantrip.Id);

                if (hasFireball && hasCantrip)
                {
                    Monitor.Log("Found conflicting Fireball spells, cleaning up...", LogLevel.Info);

                    if (hasBattleMage)
                    {
                        SaveData.UnlockedSpellIds.Remove(SpellRegistry.Fireball.Id);
                        Monitor.Log("Removed regular Fireball (player has Battle Mage)", LogLevel.Info);
                    }
                    else
                    {
                        SaveData.UnlockedSpellIds.Remove(SpellRegistry.FireballCantrip.Id);
                        Monitor.Log("Removed FireballCantrip (player doesn't have Battle Mage)", LogLevel.Info);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Monitor.Log($"Error cleaning up conflicting spells: {ex.Message}", LogLevel.Error);
            }
        }
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            // Run these once after the first UpdateTicked after day start
            Helper.Events.GameLoop.UpdateTicked += RunDayStartSetup;
        }

        private void RunDayStartSetup(object? sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= RunDayStartSetup;

            if (Game1.player == null)
                return;

            // Update Magic level
            MagicLevel = Skills.GetSkillLevel(Game1.player, SkillID);
            Monitor.Log($"Magic Level updated: {MagicLevel}", LogLevel.Debug);

            // Set max mana based on skill level
            int baseMana = 100;
            int totalMana = baseMana + 5 * MagicLevel;

            // Get the profession ID for Mana Dork
            int manaDorkID = GetProfessionId(SkillID, "ManaDork");

            // Apply bonus if player has Mana Dork
            if (Game1.player.professions.Contains(manaDorkID))
            {
                totalMana += 50;
            }

            ManaManager.SetMaxMana(totalMana);
            ManaManager.Refill();

            Monitor.Log($"Max Mana after day start setup: {ManaManager.MaxMana}", LogLevel.Debug);

            // Give spellbook if the player doesn't have one
            if (!PlayerHasSpellbookAnywhere(Game1.player))
            {
                Game1.player.addItemToInventory(new Spellbook());
                Monitor.Log("Added Spellbook to player's inventory.", LogLevel.Info);
            }
        }

        private int GetProfessionId(string skill, string profession)
        {
            return Skills.GetSkill(skill).Professions
                         .Single(p => p.Id == profession)
                         .GetVanillaId();
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


        private double manaRegenAccumulator = 0.0;

        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;

            // --- Update active spells ---
            for (int i = ActiveSpells.Count - 1; i >= 0; i--)
            {
                Spell spell = ActiveSpells[i];
                spell.Update(Game1.currentGameTime, Game1.player);
                if (!spell.IsActive)
                {
                    Monitor.Log($"Removing inactive spell: {spell.Name}", LogLevel.Trace);
                    ActiveSpells.RemoveAt(i);
                }
            }

            // --- Calculate mana regeneration ---
            double manaPerSecond = 0.1 * MagicLevel; // natural regen
            int manaRegenId = GetProfessionId(SkillID, "ManaRegeneration");
            if (Game1.player.professions.Contains(manaRegenId))
                manaPerSecond += 1.0; // extra 1 mana/sec from profession
            if (Config.godMode == true)
                manaPerSecond += 50.0;

            double manaPerTick = manaPerSecond / 60.0; // SMAPI: 60 ticks/sec
            manaRegenAccumulator += manaPerTick;

            // --- Apply accumulated mana when it reaches 1 or more ---
            int restoreAmount = (int)Math.Floor(manaRegenAccumulator);
            if (restoreAmount > 0)
            {
                ManaManager.RestoreMana(restoreAmount);
                manaRegenAccumulator -= restoreAmount;
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