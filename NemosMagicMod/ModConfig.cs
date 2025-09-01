using StardewModdingAPI;
using StardewValley;

public class ModConfig
{
    public SButton SpellSelectionKey { get; set; } = SButton.Q;
    public SButton HotkeyCast { get; set; } = SButton.Z;
    public int ManaBarX { get; set; } = 1120;
    public int ManaBarY { get; set; } = 500;
    public bool godMode { get; set; } = false;
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
                MagicLevel = 1;
            },
            save: () => helper.WriteConfig(this)
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

        gmcm.AddBoolOption(
            mod: manifest,
            name: () => "Override Magic Level",
            tooltip: () => "Set this to true to override the player's Magic Level with the configured value.",
            getValue: () => OverrideMagicLevel,
            setValue: val => OverrideMagicLevel = val
        );

        // Magic level
        gmcm.AddNumberOption(
            mod: manifest,
            name: () => "Magic Level",
            tooltip: () => "Set your starting magic level.",
            getValue: () => MagicLevel,
            setValue: val => MagicLevel = val,
            min: 0,
            max: 10
        );
    }
}
