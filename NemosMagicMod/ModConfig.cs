using NemosMagicMod;
using StardewModdingAPI;
using StardewValley;

public class ModConfig
{
    public SButton SpellSelectionKey { get; set; } = SButton.Q;
    public SButton HotkeyCast { get; set; } = SButton.Z;
    public int ManaBarX { get; set; } = 1120;
    public int ManaBarY { get; set; } = 500;
    public bool godMode { get; set; } = false;

    // Magic level override
    public bool OverrideMagicLevel { get; set; } = false;
    public int MagicLevel { get; set; } = 0;

    public void RegisterGMCM(IModHelper helper, SpaceShared.APIs.IGenericModConfigMenuApi gmcm, IManifest manifest)
    {
        gmcm.Register(
            mod: manifest,
            reset: () =>
            {
                SpellSelectionKey = SButton.D9;
                HotkeyCast = SButton.Z;
                ManaBarX = 1120;
                ManaBarY = 500;
                godMode = false;
                OverrideMagicLevel = false;
                MagicLevel = 0;
            },
            save: () =>
            {
                helper.WriteConfig(this);

                if (OverrideMagicLevel)
                    ApplyMagicLevelChange();
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

        // Mana bar X
        gmcm.AddNumberOption(
            mod: manifest,
            name: () => "Mana Bar X",
            tooltip: () => "Horizontal position of the mana bar.",
            getValue: () => ManaBarX,
            setValue: val => ManaBarX = val,
            min: 0,
            max: Game1.uiViewport.Width
        );

        // Mana bar Y
        gmcm.AddNumberOption(
            mod: manifest,
            name: () => "Mana Bar Y",
            tooltip: () => "Vertical position of the mana bar.",
            getValue: () => ManaBarY,
            setValue: val => ManaBarY = val,
            min: 0,
            max: Game1.uiViewport.Height
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
}
