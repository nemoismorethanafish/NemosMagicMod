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

    // Add the tier textures
    private static Dictionary<SpellbookTier, Texture2D>? tierTextures;

    public static void LoadIcon(IModHelper helper)
    {
        SpellbookTexture = helper.ModContent.Load<Texture2D>("assets/spellbooktexture");

        // Load tier-specific textures based on book sprites
        LoadTierTextures(helper);
    }

    private static void LoadTierTextures(IModHelper helper)
    {
        // We'll use the game's book sprites
        tierTextures = new Dictionary<SpellbookTier, Texture2D>
        {
            { SpellbookTier.Novice, Game1.objectSpriteSheet },      // All use the same sprite sheet
            { SpellbookTier.Apprentice, Game1.objectSpriteSheet },  // but different source rectangles
            { SpellbookTier.Adept, Game1.objectSpriteSheet },
            { SpellbookTier.Master, Game1.objectSpriteSheet }
        };
    }

    private static int GetBookIdForTier(SpellbookTier tier)
    {
        return tier switch
        {
            SpellbookTier.Novice => 102,      // Mapping Cave Systems
            SpellbookTier.Apprentice => 106,  // Way of the Wind pt. 1
            SpellbookTier.Adept => 108,       // Dwarvish Safety Manual
            SpellbookTier.Master => 107,      // Book of Stars
            _ => 102
        };
    }

    // Update the sprite when tier changes
    public void UpdateTierAppearance()
    {
        // This will be used in the drawing method
    }

    public override string getCategoryName() => "Magic";
    public override string DisplayName => $"Spellbook ({Tier})";

    public override string getDescription()
    {
        return Tier switch
        {
            SpellbookTier.Novice => "A simple tome containing the most basic arcane knowledge.",
            SpellbookTier.Apprentice => "An upgraded tome focused on elemental mastery.",
            SpellbookTier.Adept => "A technical grimoire filled with advanced magical theory.",
            SpellbookTier.Master => "A celestial tome containing the deepest magical secrets.",
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
        // Draw the appropriate book sprite based on tier
        int bookId = GetBookIdForTier(Tier);
        int tileSize = 16;
        int columns = Game1.objectSpriteSheet.Width / tileSize;
        int row = bookId / columns;
        int col = bookId % columns;
        Rectangle sourceRect = new Rectangle(col * tileSize, row * tileSize, tileSize, tileSize);

        spriteBatch.Draw(
            Game1.objectSpriteSheet,
            location + new Vector2(32f, 32f),
            sourceRect,
            color * transparency,
            0f,
            new Vector2(8f, 8f),
            scaleSize * 4f,
            SpriteEffects.None,
            layerDepth
        );
    }
}