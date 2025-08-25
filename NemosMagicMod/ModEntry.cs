using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using System.Linq;
using static SpaceCore.Skills;

namespace NemosMagicMod
{
    internal sealed class ModEntry : Mod
    {
        public static MagicSkill? MagicSkillInstance { get; private set; }
        public static ModEntry Instance { get; private set; } = null!;

        private int previousMagicSkillLevel = -1;
        private ManaManager? manaManager;
        private int spellCooldownTicks = 0;
        private const int SpellCooldownDurationTicks = 20;

        public override void Entry(IModHelper helper)
        {
            Instance = this;

            // Initialize MagicSkill
            MagicSkillInstance = new MagicSkill(helper);

            // Register skill with SpaceCore
            SpaceCore.Skills.RegisterSkill(MagicSkillInstance);

            this.Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            this.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;

            Monitor.Log("MagicSkill successfully registered!", LogLevel.Info);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (Game1.player == null || MagicSkillInstance == null)
            {
                Monitor.Log("Game1.player or MagicSkillInstance is null, skipping skill level initialization.", LogLevel.Warn);
                return;
            }

            // Ensure skill is registered by checking if it's in SpaceCore's skill list
            if (SpaceCore.Skills.GetSkill(MagicSkill.MagicSkillId) == null)
            {
                Monitor.Log("MagicSkill not registered, re-registering it now.", LogLevel.Info);
                SpaceCore.Skills.RegisterSkill(MagicSkillInstance);
            }

            // Fetch the skill level once the game is fully loaded
            previousMagicSkillLevel = SpaceCore.Skills.GetSkillLevel(Game1.player, MagicSkill.MagicSkillId);

            if (previousMagicSkillLevel != -1)
            {
                Monitor.Log($"Previous Magic Skill Level: {previousMagicSkillLevel}", LogLevel.Info);

                // Initialize manaManager only if not already initialized
                if (manaManager == null)
                {
                    manaManager = new ManaManager(Game1.player);
                    Monitor.Log("Save loaded: manaManager initialized.", LogLevel.Info);
                }

                // Give the spellbook to the player if they don't already have one
                bool hasSpellbook = Game1.player?.Items?.Any(item => item is Spellbook) ?? false;

                if (!hasSpellbook)
                {
                    Game1.player?.addItemToInventory(new Spellbook());
                    Monitor.Log("Spellbook added to player inventory.", LogLevel.Info);
                }
            }
            else
            {
                Monitor.Log("Magic skill level not found after save load, skipping initialization.", LogLevel.Warn);
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (spellCooldownTicks > 0)
                spellCooldownTicks--;
        }

        public void CastSpell(Spell spell)
        {
            // Early exit if the spell is null
            if (spell == null)
                return;

            // Check if manaManager is null before using it
            if (manaManager == null)
            {
                Monitor.Log("Cannot cast spell: manaManager not initialized!", LogLevel.Error);
                return;
            }

            // Check for spell cooldown
            if (spellCooldownTicks > 0)
            {
                Monitor.Log("Spell is on cooldown.", LogLevel.Trace);
                return;
            }

            // Check if there's enough mana
            if (!manaManager.UseMana(spell.ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            // Check if the player is null before attempting animation
            if (Game1.player == null)
            {
                Monitor.Log("Cannot cast spell: player is null!", LogLevel.Error);
                return;
            }

            AnimateFarmerCasting(Game1.player);
            spell.Cast(Game1.player);

            spellCooldownTicks = SpellCooldownDurationTicks;

            Game1.player.startGlowing(Microsoft.Xna.Framework.Color.Cyan, false, 0.1f);

            Monitor.Log($"Spell cast: {spell.GetType().Name}", LogLevel.Info);
        }

        private void AnimateFarmerCasting(Farmer player)
        {
            if (player == null)
                return;

            player.jitterStrength = 0f;

            player.FarmerSprite.setCurrentSingleFrame(94, 250, false, false);
            player.FarmerSprite.StopAnimation();
            player.FarmerSprite.animateOnce(new FarmerSprite.AnimationFrame[] {
                new FarmerSprite.AnimationFrame(94, 100),
                new FarmerSprite.AnimationFrame(95, 100),
                new FarmerSprite.AnimationFrame(96, 100),
            });
        }
    }

    public class MagicSkill : Skill
    {
        public const string MagicSkillId = "NemosMagicMod.MagicSkill";

        public MagicSkill(IModHelper helper) : base(MagicSkillId) { }

        public override string GetName() => "Magic";

        public int GetVanillaSkillIndex()
        {
            return 8; // Example index
        }
    }

    public class ManaManager
    {
        private Farmer player;
        public int CurrentMana { get; private set; } = 100;
        public int MaxMana { get; private set; } = 100;

        public ManaManager(Farmer player)
        {
            this.player = player;
        }

        // Method to use mana
        public bool UseMana(int amount)
        {
            if (CurrentMana >= amount)
            {
                CurrentMana -= amount;
                return true;
            }
            return false;
        }
    }
}
