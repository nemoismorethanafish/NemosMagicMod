using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewValley;
using StardewModdingAPI.Events;
using System;

public class TreeSpirit : Spell
{
    private Texture2D axeTexture;
    private float activeTimer = 0f;
    private readonly float duration = 10f; // seconds
    private bool subscribed = false;

    public bool IsActive => activeTimer > 0f;

    public TreeSpirit()
        : base("tree_spirit", "Tree Spirit", "Summons a spirit in the form of an axe.", 20, 50)
    { }

    public override void Cast(Farmer who)
    {
        base.Cast(who);

        try
        {
            axeTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/TreeSpiritAxe.png");
        }
        catch
        {
            axeTexture = null;
        }

        activeTimer = duration;

        // Hook drawing once
        if (!subscribed)
        {
            ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            subscribed = true;
        }

        Game1.playSound("leafrustle");
    }

    public override void Update(GameTime gameTime, Farmer who)
    {
        if (activeTimer > 0f)
            activeTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        else if (subscribed)
        {
            // Unsubscribe when timer ends
            ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
            subscribed = false;
        }
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!IsActive || axeTexture == null || Game1.player == null)
            return;

        SpriteBatch spriteBatch = e.SpriteBatch;

        // Floating animation
        float floatAmplitude = 10f;
        float floatSpeed = 2f;
        float bobbing = floatAmplitude * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * floatSpeed);

        float baseOffset = 48f;
        float scale = 2f;

        // Position above player
        Vector2 worldPos = Game1.player.Position + new Vector2(0, -(baseOffset * scale + bobbing));
        Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, worldPos);

        spriteBatch.Draw(
            axeTexture,
            screenPos,
            null,
            Color.White,
            0f,
            new Vector2(axeTexture.Width / 2, axeTexture.Height / 2),
            scale,
            SpriteEffects.None,
            1f // render on top
        );
    }
}
