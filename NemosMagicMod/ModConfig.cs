using StardewModdingAPI;
using StardewValley;

public class ModConfig
{
    public SButton SpellSelectionKey { get; set; } = SButton.D9;
    public int ManaBarX { get; set; } = 1120;
    public int ManaBarY { get; set; } = 500;
    public bool godMode { get; set; } = false;

    public void RegisterGMCM(IModHelper helper, SpaceShared.APIs.IGenericModConfigMenuApi gmcm, IManifest manifest)
    {
        gmcm.Register(
            mod: manifest,
            reset: () =>
            {
                SpellSelectionKey = SButton.D9;
                ManaBarX = 1120;
                ManaBarY = 500;
                godMode = false;
            },
            save: () => helper.WriteConfig(this)
        );

        gmcm.AddKeybind(
            mod: manifest,
            name: () => "Spell Selection Key",
            tooltip: () => "The key to open the spell selection menu.",
            getValue: () => SpellSelectionKey,
            setValue: val => SpellSelectionKey = val
        );

        gmcm.AddNumberOption(
            mod: manifest,
            name: () => "Mana Bar X",
            tooltip: () => "Horizontal position of the mana bar.",
            getValue: () => ManaBarX,
            setValue: val => ManaBarX = val,
            min: 0,
            max: Game1.uiViewport.Width
        );

        gmcm.AddNumberOption(
            mod: manifest,
            name: () => "Mana Bar Y",
            tooltip: () => "Vertical position of the mana bar.",
            getValue: () => ManaBarY,
            setValue: val => ManaBarY = val,
            min: 0,
            max: Game1.uiViewport.Height
        );

        gmcm.AddBoolOption(
            mod: manifest,
            name: () => "God Mode",
            tooltip: () => "Infinite Mana, no level reqs, etc.",
            getValue: () => godMode,
            setValue: val => godMode = val
        );
    }
}
