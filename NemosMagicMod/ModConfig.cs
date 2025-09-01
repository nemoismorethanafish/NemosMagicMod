// Updated ModConfig.cs
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
    public int MagicLevel { get; set; } = 0; // Remove the OverrideMagicLevel boolean

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
                MagicLevel = 0; // Default to 0 (natural progression)
            },
            save: () =>
            {
                helper.WriteConfig(this);
                // Apply magic level change immediately when config is saved
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

        // Magic level - updated tooltip
        gmcm.AddNumberOption(
            mod: manifest,
            name: () => "Set Magic Level",
            tooltip: () => "Set your Magic Level (0 = natural progression). Changes apply when you save config.",
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
        // level is 0..10; returns total XP needed to be exactly at that level (floor)
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
            if (MagicLevel <= 0)
                return; // let natural progression handle <=0 per your comment

            const string skillId = "nemosmagicmod.Magic";

            // Use whatever you already have to read the current level.
            // If you have a getter, prefer that. Otherwise cache it, or derive it from your own stored XP.
            int currentLevel = SpaceCore.Skills.GetSkillLevel(Game1.player, skillId);

            int targetLevel = MagicLevel;
            int currentFloorXp = TotalXpForLevel(currentLevel);
            int targetFloorXp = TotalXpForLevel(targetLevel);
            int delta = targetFloorXp - currentFloorXp; // negative if lowering

            if (delta != 0)
            {
                // Public SpaceCore API call – this compiles.
                SpaceCore.Skills.AddExperience(Game1.player, skillId, delta);
            }

            NemosMagicMod.ModEntry.MagicLevel = targetLevel;

            // Recalculate any dependent stats
            ManaManager.RecalculateMaxMana();

            Game1.addHUDMessage(new HUDMessage($"Magic Level set to {targetLevel}", HUDMessage.achievement_type));
            NemosMagicMod.ModEntry.Instance?.Monitor?.Log($"Magic Level set to {targetLevel} via config", LogLevel.Info);
        }
        catch (System.Exception ex)
        {
            NemosMagicMod.ModEntry.Instance?.Monitor?.Log($"Error applying magic level change: {ex.Message}", LogLevel.Error);
        }
    }
}