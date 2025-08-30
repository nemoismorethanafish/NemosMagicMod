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
using static NemosMagicMod.Spells.SpellbookUpgradeSystem;
using static SpaceCore.Skills;
using Microsoft.Xna.Framework;


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

            var player = Game1.player;

            if (e.Button == SButton.L) // Trigger on L
            {
                TriggerBookAnimation(Game1.player);
            }

            // Spell selection menu key
            if (e.Button == SButton.D9)
            {
                Game1.activeClickableMenu = new SpellSelectionMenu(Helper, Monitor);
                return;
            }

            // --- Debug: Open SpellbookUpgradeMenu with key 0 ---
            if (e.Button == SButton.D0)
            {
                Spellbook? spellbook = player.Items.OfType<Spellbook>().FirstOrDefault();
                if (spellbook != null)
                {
                    Game1.activeClickableMenu = new SpellbookUpgradeMenu(player, spellbook, Monitor);
                }
                else
                {
                    Game1.showRedMessage("No Spellbook found in inventory!");
                }
                return;
            }

            // --- Hardcoded right-click to trigger wizard interaction ---
            if (e.Button == SButton.MouseRight)
            {
                // Compute the tile the player is facing
                Vector2 playerTile = new Vector2((int)(player.Position.X / Game1.tileSize), (int)(player.Position.Y / Game1.tileSize));
                Vector2 facingTile = playerTile + new Vector2(
                    player.FacingDirection == 1 ? 1 : player.FacingDirection == 3 ? -1 : 0,
                    player.FacingDirection == 0 ? -1 : player.FacingDirection == 2 ? 1 : 0
                );

                // Find Wizard at that tile
                NPC wizard = Game1.currentLocation.characters
                    .FirstOrDefault(n =>
                        n.Name == "Wizard" &&
                        Vector2.Distance(
                            new Vector2((int)(n.Position.X / Game1.tileSize), (int)(n.Position.Y / Game1.tileSize)),
                            facingTile
                        ) < 1f
                    );

                if (wizard != null)
                {
                    Spellbook? spellbook = player.Items.OfType<Spellbook>().FirstOrDefault();
                    if (spellbook != null)
                        SpellbookUpgradeSystem.OfferWizardUpgrade(player, spellbook, Monitor);
                    else
                        Game1.showRedMessage("You don't have a Spellbook!");
                }
            }
        }

        private void TriggerBookAnimation(Farmer player)
        {
            if (player == null)
                return;

            // Create a temporary Price Catalogue object
            var tempBook = new StardewValley.Object("104", 1);

            // Use reflection to call internal readBook method
            var method = typeof(StardewValley.Object).GetMethod(
                "readBook",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
            );

            if (method != null)
            {
                // Pass the player's current location instead of the player
                method.Invoke(tempBook, new object[] { player.currentLocation });
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

