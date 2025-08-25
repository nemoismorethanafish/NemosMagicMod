using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;
using StardewModdingAPI;
using System.Collections.Generic;

namespace NemosMagicMod
{
    public class Spellbook : Tool
    {
        public static Texture2D? IconTexture;

        // Keep track of sprites spawned by casting spells so we can remove them cleanly
        private readonly List<TemporaryAnimatedSprite> trackedSpellSprites = new();

        public Spellbook() : base("Spellbook", 0, 0, 0, false)
        {
            UpgradeLevel = 0;
        }

        public static void LoadIcon(IModHelper helper)
        {
            IconTexture = helper.ModContent.Load<Texture2D>("assets/magic-icon-smol.png");
        }

        protected override Item GetOneNew()
        {
            return new Spellbook();
        }

        public override bool canBeTrashed()
        {
            return false;
        }

        // Prevent the default tool animation from triggering
        public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
        {
            who.UsingTool = false;
            who.canReleaseTool = true;
            who.completelyStopAnimatingOrDoingAction();
            who.Halt();

            // Cast the spell here instead of in DoFunction
            if (SpellRegistry.SelectedSpell != null)
            {
                // Clear old sprites as needed, then cast
                // Example:
                ModEntry.Instance.CastSpell(SpellRegistry.SelectedSpell);
            }

            return false; // prevent animation
        }

        public override bool onRelease(GameLocation location, int x, int y, Farmer who)
        {
            return false; // no extra action on release
        }

        public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who)
        {
            // Intentionally left empty since casting is handled in beginUsing
        }

        // Override tickUpdate to do nothing, avoiding default tool behavior
        public override void tickUpdate(GameTime time, Farmer who)
        {
            // Intentionally left blank
        }

        public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
        {
            if (IconTexture == null)
                return;

            spriteBatch.Draw(
                IconTexture,
                location + new Vector2(32f, 32f),
                null,
                color * transparency,
                0f,
                new Vector2(IconTexture.Width / 2f, IconTexture.Height / 2f),
                scaleSize * 4f,
                SpriteEffects.None,
                layerDepth
            );
        }
    }
}
