using NemosMagicMod;
using NemosMagicMod.Spells;
using StardewModdingAPI;
using StardewValley;

public class ModConfig
{
    public SButton SpellSelectionKey { get; set; } = SButton.Q;
    public SButton HotkeyCast { get; set; } = SButton.Z;
    public bool godMode { get; set; } = false;
    public bool OverrideMagicLevel { get; set; } = false;
    public int MagicLevel { get; set; } = 0;
    public bool GiveMasterSpellbook { get; set; } = false;



    public void RegisterGMCM(IModHelper helper, SpaceShared.APIs.IGenericModConfigMenuApi gmcm, IManifest manifest)
    {
        gmcm.Register(
            mod: manifest,
            reset: () =>
            {
                SpellSelectionKey = SButton.D9;
                HotkeyCast = SButton.Z;
                godMode = false;
                OverrideMagicLevel = false;
                MagicLevel = 0;
            },
            save: () =>
            {
                helper.WriteConfig(this);

                 if (OverrideMagicLevel)
                        ApplyMagicLevelChange();

                 if (GiveMasterSpellbook)
                    {
                     TryGiveMasterSpellbook();
                     GiveMasterSpellbook = false; // reset so it doesn’t keep spawning
                        helper.WriteConfig(this);
                    }
}

        );

        // Spell selection menu key
        gmcm.AddKeybind(
            mod: manifest,
            name: () => "Spell Selection Key",
            tooltip: () => "The key to open the spell selection menu.",
            getValue: () => SpellSelectionKey,
            setValue: val => SpellSelectionKey = val
        );

        // Hotkey cast
        gmcm.AddKeybind(
            mod: manifest,
            name: () => "Hotkey Cast",
            tooltip: () => "The key to instantly cast your hotkeyed spell.",
            getValue: () => HotkeyCast,
            setValue: val => HotkeyCast = val
        );

        // God mode
        gmcm.AddBoolOption(
            mod: manifest,
            name: () => "God Mode",
            tooltip: () => "Infinite Mana, no level reqs, etc.",
            getValue: () => godMode,
            setValue: val => godMode = val
        );

        // Override magic level toggle
        gmcm.AddBoolOption(
            mod: manifest,
            name: () => "Override Magic Level",
            tooltip: () => "If enabled, the Magic Level below will be forced when you save config.",
            getValue: () => OverrideMagicLevel,
            setValue: val => OverrideMagicLevel = val
        );

        // Magic level number
        gmcm.AddNumberOption(
            mod: manifest,
            name: () => "Magic Level",
            tooltip: () => "Target Magic Level (0 = natural progression). Only applied if override is enabled.",
            getValue: () => MagicLevel,
            setValue: val => MagicLevel = val,
            min: 0,
            max: 10
        );

        // Give Master Spellbook
        gmcm.AddBoolOption(
            mod: manifest,
            name: () => "Give Master Spellbook",
            tooltip: () => "If enabled, a Master tier spellbook will be added to your inventory when you save the config.",
            getValue: () => GiveMasterSpellbook,
            setValue: val => GiveMasterSpellbook = val
        );

    }

    private static readonly int[] MagicXpPerLevel =
    {
        100, 380, 770, 1300, 2150, 3300, 4800, 6900, 10000, 15000
    };

    private static int TotalXpForLevel(int level)
    {
        int total = 0;
        for (int i = 0; i < level && i < MagicXpPerLevel.Length; i++)
            total += MagicXpPerLevel[i];
        return total;
    }

    private void ApplyMagicLevelChange()
    {
        if (!Context.IsWorldReady || Game1.player is null)
            return;

        try
        {
            const string skillId = "nemosmagicmod.Magic";

            int currentLevel = SpaceCore.Skills.GetSkillLevel(Game1.player, skillId);
            int targetLevel = MagicLevel;

            int currentXp = TotalXpForLevel(currentLevel);
            int targetXp = TotalXpForLevel(targetLevel);
            int delta = targetXp - currentXp;

            if (delta != 0)
            {
                SpaceCore.Skills.AddExperience(Game1.player, skillId, delta);
            }

            NemosMagicMod.ModEntry.MagicLevel = targetLevel;

            ManaManager.RecalculateMaxMana();

            Game1.addHUDMessage(new HUDMessage($"Magic Level set to {targetLevel}", HUDMessage.achievement_type));
            NemosMagicMod.ModEntry.Instance?.Monitor?.Log($"Magic Level set to {targetLevel} via config override", LogLevel.Info);
        }
        catch (System.Exception ex)
        {
            NemosMagicMod.ModEntry.Instance?.Monitor?.Log($"Error applying magic level change: {ex.Message}", LogLevel.Error);
        }
    }

    private void TryGiveMasterSpellbook()
    {
        if (!Context.IsWorldReady || Game1.player is null)
            return;

        try
        {
            Farmer player = Game1.player;

            // Remove all existing spellbooks
            for (int i = player.Items.Count - 1; i >= 0; i--)
            {
                if (player.Items[i] is Spellbook)
                    player.removeItemFromInventory(player.Items[i]);
            }

            // Create new Master Spellbook
            Spellbook masterBook = new Spellbook { Tier = SpellbookTier.Master };

            if (!player.addItemToInventoryBool(masterBook))
            {
                Game1.createItemDebris(masterBook, player.getStandingPosition(), 0);
            }

            Game1.addHUDMessage(new HUDMessage("You received a Master Spellbook!", HUDMessage.newQuest_type));
            NemosMagicMod.ModEntry.Instance?.Monitor?.Log("Replaced all spellbooks with a Master Spellbook via config option.", LogLevel.Info);
        }
        catch (System.Exception ex)
        {
            NemosMagicMod.ModEntry.Instance?.Monitor?.Log($"Error giving Master Spellbook: {ex.Message}", LogLevel.Error);
        }
    }

}
