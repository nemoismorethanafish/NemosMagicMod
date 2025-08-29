using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod.Spells;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

[XmlType("Mods_NemosMagicMod_Spellbook")]
public class Spellbook : Tool
{
    public SpellbookTier Tier
    {
        get => tier;
        set
        {
            tier = value;
            UpgradeLevel = (int)tier; // sync vanilla property
        }
    }
    private SpellbookTier tier = SpellbookTier.Novice;


    public static Texture2D? SpellbookTexture;

    public static void LoadIcon(IModHelper helper)
    {
        SpellbookTexture = helper.ModContent.Load<Texture2D>("assets/spellbooktexture");
    }

    public override string getCategoryName() => "Magic";
    public override string DisplayName => $"Spellbook ({Tier})";

    public override string getDescription()
    {
        return Tier switch
        {
            SpellbookTier.Novice => "A simple tome containing the most basic arcane knowledge.",
            SpellbookTier.Adept => "An upgraded tome, brimming with stronger magical energy.",
            SpellbookTier.Master => "A powerful spellbook infused with deep magical secrets.",
            _ => "A mysterious spellbook."
        };
    }
    public override bool canBeTrashed() => false;
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
            SpellRegistry.SelectedSpell.Cast(Game1.player);

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

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        if (Tier == SpellbookTier.Basic)
            Tier = SpellbookTier.Novice;
    }

}
