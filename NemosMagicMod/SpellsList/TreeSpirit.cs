using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI.Events;
using StardewValley;
using System;
using static Spell;

public class TreeSpirit : Spell, IRenderable
{
    private Texture2D axeTexture;
    private float activeTimer = 0f;
    private readonly float duration = 10f; // seconds
    private bool subscribed = false;

    public bool IsActive => activeTimer > 0f;

    public TreeSpirit()
        : base("tree_spirit", "Tree Spirit",
              "Summons a spirit in the form of an axe.",
              20, 50)
    { }

    public override void Cast(Farmer who)
    {
        base.Cast(who); // base Cast now handles disabling other spells

        try
        {
            axeTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/TreeSpiritAxe.png");
        }
        catch
        {
            axeTexture = null;
        }

        activeTimer = duration;

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
    }

    public void Unsubscribe()
    {
        if (subscribed)
        {
            ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
            subscribed = false;
        }
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!IsActive || axeTexture == null || Game1.player == null)
            return;

        SpriteBatch spriteBatch = e.SpriteBatch;

        float floatAmplitude = 10f;
        float floatSpeed = 2f;
        float bobbing = floatAmplitude * (float)Math.Sin(Game1.currentGameTime.TotalGameTime.TotalSeconds * floatSpeed);

        float baseOffset = 48f;
        float scale = 2f;

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
            1f
        );
    }
}
