using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NemosMagicMod;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System;

internal class ManaBar
{
    private readonly Func<int> getCurrentMana;
    private readonly Func<int> getMaxMana;
    private readonly IModHelper helper;

    private Texture2D manaBarTexture;
    private Texture2D manaBarFullTexture;

    public int X { get; set; }
    public int Y { get; set; }

    private int width;
    private int height;

    private bool isDragging = false;
    private Point dragOffset;
    private MouseState previousMouseState;

    public ManaBar(Func<int> getCurrentMana, Func<int> getMaxMana, IModHelper helper)
    {
        this.getCurrentMana = getCurrentMana;
        this.getMaxMana = getMaxMana;
        this.helper = helper;

        // Load textures
        manaBarTexture = helper.ModContent.Load<Texture2D>("assets/ManaBar.png");
        manaBarFullTexture = helper.ModContent.Load<Texture2D>("assets/ManaBarFull.png");

        // Scale up by 20%
        width = (int)(manaBarTexture.Width * 1f);
        height = (int)(manaBarTexture.Height * 1f);

        LoadPosition();
        previousMouseState = Mouse.GetState();
    }

    public void DrawManaBar(SpriteBatch spriteBatch, bool drawTooltip = true)
    {
        if (manaBarTexture == null)
            return;

        // Current mana percentage
        float percent = (float)getCurrentMana() / getMaxMana();

        // Fill scaling (makes bar fill slower)
        float fillScale = 0.82f;
        percent *= fillScale;
        percent = MathHelper.Clamp(percent, 0f, 1f);

        // Draw empty background
        spriteBatch.Draw(
            manaBarTexture,
            new Rectangle(X, Y, width, height),
            Color.White
        );

        // Draw full bar slice
        if (manaBarFullTexture != null && percent > 0f)
        {
            int fillHeight = (int)(height * percent);

            Rectangle sourceRect = new Rectangle(0, manaBarFullTexture.Height - (int)(manaBarFullTexture.Height * percent),
                                                 manaBarFullTexture.Width, (int)(manaBarFullTexture.Height * percent));
            Rectangle destRect = new Rectangle(X, Y + height - fillHeight, width, fillHeight);

            spriteBatch.Draw(
                manaBarFullTexture,
                destRect,
                sourceRect,
                Color.White
            );
        }

        if (drawTooltip)
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
        config.ManaBarX = (float)X / Game1.viewport.Width;
        config.ManaBarY = (float)Y / Game1.viewport.Height;
        helper.WriteConfig(config);
    }

    private void LoadPosition()
    {
        var config = helper.ReadConfig<ModConfig>();
        X = (int)(config.ManaBarX * Game1.viewport.Width);
        Y = (int)(config.ManaBarY * Game1.viewport.Height);
    }

    public void OnRenderingHud(object? sender, RenderingHudEventArgs e)
    {
        if (Game1.eventUp || Game1.isFestival())
            return;

        if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is not GameMenu)
            return;

        HandleDragging();
        DrawManaBar(Game1.spriteBatch, drawTooltip: false);
    }

    public void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (Game1.eventUp || Game1.isFestival())
            return;

        if (Game1.activeClickableMenu != null && Game1.activeClickableMenu is not GameMenu)
            return;

        if (!Game1.displayHUD)
            return;

        DrawTooltip(Game1.spriteBatch);
    }

    public void SubscribeToEvents()
    {
        helper.Events.Display.RenderingHud += OnRenderingHud;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.GameLoop.DayStarted += (_, _) => ManaManager.Refill();
        helper.Events.Display.WindowResized += (_, _) => LoadPosition();
    }

    private class ModConfig
    {
        public float ManaBarX { get; set; } = 0.05f;
        public float ManaBarY { get; set; } = 0.05f;
    }
}
