using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using System.Xml.Serialization;
using SpaceCore;

[XmlType("Mods_NemosMagicMod_Spellbook")]
public class Spellbook : Tool
{

    public override string getDescription() => "A book filled with arcane knowledge.";

    public override string getCategoryName() => "Magic";

    public string GetSpriteName() => "NemosMagicMod.Spellbook";

    public override string Name => "Spellbook";


    public static Texture2D? SpellbookTexture;

    private readonly List<TemporaryAnimatedSprite> trackedSpellSprites = new();

    public Spellbook() : base("Spellbook", 0, 0, 0, false)
    {
        UpgradeLevel = 0;
    }

    public static void LoadIcon(IModHelper helper)
    {
        SpellbookTexture = helper.ModContent.Load<Texture2D>("assets/spellbooktexture");
    }

    public override bool canBeTrashed() => false;

    public override string DisplayName => "Spellbook";

    public override bool canBeShipped() => false;

    public override bool canStackWith(ISalable other) => false;

    protected override Item GetOneNew() => new Spellbook();

    public override bool beginUsing(GameLocation location, int x, int y, Farmer who)
    {
        who.UsingTool = false;
        who.canReleaseTool = true;
        who.completelyStopAnimatingOrDoingAction();
        who.Halt();

        if (SpellRegistry.SelectedSpell != null && Game1.player != null)
        {
            SpellRegistry.SelectedSpell.Cast(Game1.player);
        }

        return false;
    }

    public override bool onRelease(GameLocation location, int x, int y, Farmer who) => false;

    public override void DoFunction(GameLocation location, int x, int y, int power, Farmer who) { }

    public override void tickUpdate(GameTime time, Farmer who) { }

    public override void drawInMenu(SpriteBatch spriteBatch, Vector2 location, float scaleSize, float transparency, float layerDepth, StackDrawType drawStackNumber, Color color, bool drawShadow)
    {
        if (SpellbookTexture == null)
            return;

        spriteBatch.Draw(
            SpellbookTexture,
            location + new Vector2(32f, 32f),
            null,
            color * transparency,
            0f,
            new Vector2(SpellbookTexture.Width / 2f, SpellbookTexture.Height / 2f),
            scaleSize * 4f,
            SpriteEffects.None,
            layerDepth
        );
    }
}
