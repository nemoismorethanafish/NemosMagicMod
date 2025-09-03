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
        public static List<Spell> ActiveSpells => SpellRegistry.ActiveSpells.ToList();
        private bool queuedWizardUpgrade = false;
        private Spellbook? queuedSpellbook = null;
        private bool skillSystemReady = false;
        private int initializationRetries = 0;
        private const int MAX_RETRIES = 100;
        public ModConfig Config { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            new LevelUpChanges(helper, Monitor);
            ManaManager.SetMaxMana(ManaManager.MaxMana);
            ManaManager.Refill();
            manaBar = new ManaBar(
                () => ManaManager.CurrentMana,
                () => ManaManager.MaxMana,
                helper // pass the IModHelper so ManaBar can read/write its config
            );

            manaBar.SubscribeToEvents();
            Spellbook.LoadIcon(helper);
            MagicSkillIcon = Helper.ModContent.Load<Texture2D>("assets/magic-icon-smol.png");

            // === Event subscriptions ===
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.GameLoop.DayEnding += OnDayEnding;
            helper.Events.GameLoop.Saving += OnSaving;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

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

            // Hotkey spell cast - now delegated to SpellRegistry
            if (e.Button == Config.HotkeyCast)
            {
                string? hotkeyId = SaveData.HotkeyedSpellId;
                SpellRegistry.TryHotkeyCast(player, hotkeyId);
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

            Helper.Events.GameLoop.UpdateTicked += CleanupConflictingSpellsOnce;
        }
        private int GetProfessionId(string skill, string profession)
        {
            return Skills.GetSkill(skill).Professions
                         .Single(p => p.Id == profession)
                         .GetVanillaId();
        }
        private void CleanupConflictingSpellsOnce(object? sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= CleanupConflictingSpellsOnce;
            SpellRegistry.CleanupConflictingSpells(Monitor);
        }
        public void SetCustomSkillLevel(Farmer player, string skillId, int targetLevel)
        {
            var skill = Skills.GetSkill(skillId);
            if (skill == null)
            {
                Monitor.Log($"Cannot set unknown skill {skillId} - skill not registered", LogLevel.Warn);
                return;
            }

            try
            {
                // Get current level safely
                int currentLevel = 0;
                try
                {
                    currentLevel = Skills.GetSkillLevel(player, skillId);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error getting current skill level for {skillId}: {ex.Message}", LogLevel.Warn);
                    // Continue with currentLevel = 0
                }

                // Clamp level within the skill's ExperienceCurve
                targetLevel = Math.Clamp(targetLevel, 0, skill.ExperienceCurve.Length);

                // Get target XP for the level
                int targetXP = targetLevel == 0 ? 0 : skill.ExperienceCurve[targetLevel - 1];

                int currentXP = 0;
                try
                {
                    currentXP = Skills.GetExperienceFor(player, skillId);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error getting current XP for {skillId}: {ex.Message}", LogLevel.Warn);
                    // Continue with currentXP = 0
                }

                int deltaXP = targetXP - currentXP;

                if (deltaXP != 0)
                {
                    Skills.AddExperience(player, skillId, deltaXP);
                }

                Monitor.Log($"Set {skillId} level from {currentLevel} to {targetLevel} (XP={targetXP})", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error setting skill level for {skillId}: {ex.Message}", LogLevel.Error);
            }
        }
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            skillSystemReady = false;
            initializationRetries = 0;

            Helper.Events.GameLoop.UpdateTicked += CheckSpaceCoreReady;
        }
        private void RunDayStartSetup(object? sender, UpdateTickedEventArgs e)
        {
            Helper.Events.GameLoop.UpdateTicked -= RunDayStartSetup;

            if (Game1.player == null)
                return;

            var skill = Skills.GetSkill(SkillID);
            if (skill == null)
            {
                Monitor.Log("Magic skill not yet registered, deferring setup.", LogLevel.Debug);
                // Defer setup by re-subscribing to try again next tick
                Helper.Events.GameLoop.UpdateTicked += RunDayStartSetup;
                return;
            }

            try
            {
                // Update Magic level safely
                MagicLevel = 0; // Default value
                try
                {
                    MagicLevel = Skills.GetSkillLevel(Game1.player, SkillID);
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error getting Magic level during setup: {ex.Message}", LogLevel.Warn);
                }

                Monitor.Log($"Magic Level updated: {MagicLevel}", LogLevel.Debug);

                // Set max mana based on skill level
                int baseMana = 100;
                int totalMana = baseMana + 5 * MagicLevel;

                // Get the profession ID for Mana Dork safely
                try
                {
                    int manaDorkID = GetProfessionId(SkillID, "ManaDork");
                    if (Game1.player.professions.Contains(manaDorkID))
                    {
                        totalMana += 50;
                    }
                }
                catch (Exception ex)
                {
                    Monitor.Log($"Error getting ManaDork profession: {ex.Message}", LogLevel.Warn);
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
            catch (Exception ex)
            {
                Monitor.Log($"Error in RunDayStartSetup: {ex.Message}", LogLevel.Error);
            }
        }
        private void RunFullDayStartSetup()
        {
            if (Game1.player == null) return;

            try
            {
                // Delegate mana setup to ManaManager
                ManaManager.SetupDayStartMana(skillSystemReady, Monitor);

                // Give spellbook if the player doesn't have one
                if (!PlayerHasSpellbookAnywhere(Game1.player))
                {
                    Game1.player.addItemToInventory(new Spellbook());
                    Monitor.Log("Added Spellbook to player's inventory.", LogLevel.Info);
                }
            }
            catch (Exception ex)
            {
                Monitor.Log($"Error in RunFullDayStartSetup: {ex.Message}", LogLevel.Error);
                RunBasicDayStartSetup();
            }
        }
        private void RunBasicDayStartSetup()
        {
            if (Game1.player == null) return;

            // Delegate basic mana setup to ManaManager
            ManaManager.SetBasicMana();
            ManaManager.Refill();

            // Give spellbook if the player doesn't have one
            if (!PlayerHasSpellbookAnywhere(Game1.player))
            {
                Game1.player.addItemToInventory(new Spellbook());
                Monitor.Log("Added Spellbook to player's inventory.", LogLevel.Info);
            }

            Monitor.Log("Basic day start setup completed (SpaceCore unavailable)", LogLevel.Info);
        }
        private static void OnDayEnding(object sender, DayEndingEventArgs e)
        {
            ManaManager.ProcessDayEnd();
        }
        public void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || Game1.player == null)
                return;

            // Delegate active spell updates to SpellRegistry
            SpellRegistry.UpdateActiveSpells(Game1.currentGameTime, Game1.player);

            // Delegate mana regeneration to ManaManager
            ManaManager.UpdateManaRegeneration(Config, skillSystemReady, Monitor);
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
        private void CheckSpaceCoreReady(object? sender, UpdateTickedEventArgs e)
        {
            initializationRetries++;

            // Give up after too many retries
            if (initializationRetries > MAX_RETRIES)
            {
                Helper.Events.GameLoop.UpdateTicked -= CheckSpaceCoreReady;
                Monitor.Log("SpaceCore initialization timeout - running setup without skill system", LogLevel.Warn);
                RunBasicDayStartSetup();
                return;
            }

            // Check if SpaceCore skills are working
            try
            {
                var skill = Skills.GetSkill(SkillID);
                if (skill != null && Game1.player != null)
                {
                    // Try to get skill level - this will throw if SpaceCore isn't ready
                    int testLevel = Skills.GetSkillLevel(Game1.player, SkillID);

                    // If we got here, SpaceCore is working
                    Helper.Events.GameLoop.UpdateTicked -= CheckSpaceCoreReady;
                    skillSystemReady = true;
                    Monitor.Log($"SpaceCore ready after {initializationRetries} ticks", LogLevel.Debug);
                    RunFullDayStartSetup();
                }
            }
            catch (Exception ex)
            {
                // SpaceCore not ready yet, continue checking
                if (initializationRetries % 30 == 0) // Log every half second
                {
                    Monitor.Log($"Waiting for SpaceCore... (attempt {initializationRetries})", LogLevel.Debug);
                }
            }
        }
        public static void RegisterActiveSpell(Spell spell)
        {
            SpellRegistry.RegisterActiveSpell(spell);
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