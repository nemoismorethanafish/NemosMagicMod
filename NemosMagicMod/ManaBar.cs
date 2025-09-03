using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NemosMagicMod;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;

internal class ManaBar
{
    private readonly Func<int> getCurrentMana;
    private readonly Func<int> getMaxMana;
    private readonly IModHelper helper;

    public int X { get; set; }
    public int Y { get; set; }

    private readonly int width = 25;
    private readonly int height = 200;

    private bool isDragging = false;
    private Point dragOffset;
    private MouseState previousMouseState;

    public ManaBar(Func<int> getCurrentMana, Func<int> getMaxMana, IModHelper helper)
    {
        this.getCurrentMana = getCurrentMana;
        this.getMaxMana = getMaxMana;
        this.helper = helper;

        var config = helper.ReadConfig<ModConfig>();
        X = config.ManaBarX;
        Y = config.ManaBarY;

        previousMouseState = Mouse.GetState();
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

        DrawTooltip(spriteBatch);
    }

    public void DrawTooltip(SpriteBatch spriteBatch)
    {
        var mouse = Mouse.GetState();
        var rect = new Rectangle(X, Y, width, height);
        if (rect.Contains(new Point(mouse.X, mouse.Y)))
        {
            string tooltip = $"Mana: {getCurrentMana()} / {getMaxMana()}";
            IClickableMenu.drawHoverText(spriteBatch, tooltip, Game1.smallFont);
        }
    }

    private void HandleDragging()
    {
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        var mousePoint = new Point(mouse.X, mouse.Y);
        var rect = new Rectangle(X, Y, width, height);

        if (!isDragging && rect.Contains(mousePoint) &&
            keyboard.IsKeyDown(Keys.LeftControl) &&
            mouse.LeftButton == ButtonState.Pressed &&
            previousMouseState.LeftButton == ButtonState.Released)
        {
            isDragging = true;
            dragOffset = new Point(mouse.X - X, mouse.Y - Y);
        }

        if (isDragging && mouse.LeftButton == ButtonState.Pressed)
        {
            X = mouse.X - dragOffset.X;
            Y = mouse.Y - dragOffset.Y;
        }
        else if (isDragging)
        {
            isDragging = false;
            SavePosition();
        }

        previousMouseState = mouse;
    }

    private void SavePosition()
    {
        var config = helper.ReadConfig<ModConfig>();
        config.ManaBarX = X;
        config.ManaBarY = Y;
        helper.WriteConfig(config);
    }

    public void OnRenderingHud(object? sender, StardewModdingAPI.Events.RenderingHudEventArgs e)
    {
        if (Game1.eventUp || Game1.isFestival())
            return;

        if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is not GameMenu)
            return;

        HandleDragging();
        DrawManaBar(Game1.spriteBatch);
    }

    public void SubscribeToEvents()
    {
        helper.Events.Display.RenderingHud += OnRenderingHud;
        helper.Events.GameLoop.DayStarted += (_, _) => ManaManager.Refill();
    }

    private class ModConfig
    {
        public int ManaBarX { get; set; } = 50;
        public int ManaBarY { get; set; } = 50;
    }
}
