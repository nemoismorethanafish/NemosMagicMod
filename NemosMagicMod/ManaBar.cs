using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;

internal class ManaBar
{
    private readonly Func<int> getCurrentMana;
    private readonly Func<int> getMaxMana;

    public int X { get; set; }
    public int Y { get; set; }

    private readonly int width = 25;
    private readonly int height = 200;

    public ManaBar(Func<int> getCurrentMana, Func<int> getMaxMana, int x, int y)
    {
        this.getCurrentMana = getCurrentMana;
        this.getMaxMana = getMaxMana;
        this.X = x;
        this.Y = y;
    }

    public void DrawManaBar(SpriteBatch spriteBatch)
    {
        float percent = (float)getCurrentMana() / getMaxMana();

        // Outer border
        IClickableMenu.drawTextureBox(
            spriteBatch,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            X - 8, Y - 8, width + 16, height + 16,
            Color.White);

        // Background
        IClickableMenu.drawTextureBox(
            spriteBatch,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            X, Y, width, height,
            Color.Black * 0.5f);

        // Fill
        int fillHeight = (int)((height - 8) * percent);
        int fillY = Y + (height - 4) - fillHeight;

        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(X + 4, fillY, width - 8, fillHeight),
            Color.Blue);

        // Draw tooltip on top if mouse is over
        DrawTooltip(spriteBatch);
    }

    public void DrawTooltip(SpriteBatch spriteBatch)
    {
        var mouse = Game1.getMousePosition();
        var rect = new Rectangle(X, Y, width, height);
        if (rect.Contains(mouse))
        {
            string tooltip = $"Mana: {getCurrentMana()} / {getMaxMana()}";
            // Draw on top of the bar
            IClickableMenu.drawHoverText(spriteBatch, tooltip, Game1.smallFont);
        }
    }

    public void OnRenderingHud(object? sender, StardewModdingAPI.Events.RenderingHudEventArgs e)
    {
        // Don’t draw if a festival, event, or special cutscene is active
        if (Game1.eventUp || Game1.isFestival())
            return;

        // Optionally: don’t show if another HUD-blocking menu is open
        if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is not GameMenu)
            return;

        DrawManaBar(Game1.spriteBatch);
    }


    public void SubscribeToEvents(IModHelper helper)
    {
        helper.Events.Display.RenderingHud += OnRenderingHud;
        helper.Events.GameLoop.DayStarted += (_, _) => ManaManager.Refill();

    }
}
