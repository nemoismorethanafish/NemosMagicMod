using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using StardewValley;
using System.Collections.Generic;
using StardewModdingAPI;

// Make sure this matches your SpellRegistry namespace!
using NemosMagicMod.Spells;

public class SpellSelectionMenu : IClickableMenu
{
    private readonly List<Spell> spells;
    private int selectedSpellIndex = 0;
    private readonly IModHelper helper;

    public SpellSelectionMenu(IModHelper helper)
        : base(Game1.uiViewport.Width / 2 - 200, Game1.uiViewport.Height / 2 - 150, 400, 300, true)
    {
        this.spells = SpellRegistry.Spells;  // Directly use the SpellRegistry list
        this.helper = helper;
    }

    public override void draw(SpriteBatch b)
    {
        // Draw menu background
        IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        // Draw title
        SpriteFont font = Game1.dialogueFont;
        string title = "Select a Spell";
        Vector2 titleSize = font.MeasureString(title);
        b.DrawString(font, title, new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, yPositionOnScreen + 10), Color.Black);

        // Draw spell list
        for (int i = 0; i < spells.Count; i++)
        {
            Color textColor = i == selectedSpellIndex ? Color.Yellow : Color.Black;
            string spellName = spells[i].Name;
            b.DrawString(font, spellName, new Vector2(xPositionOnScreen + 20, yPositionOnScreen + 50 + i * 30), textColor);
        }

        base.drawMouse(b);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < spells.Count; i++)
        {
            Rectangle spellRect = new Rectangle(xPositionOnScreen + 20, yPositionOnScreen + 50 + i * 30 - 5, 200, 30);
            if (spellRect.Contains(x, y))
            {
                selectedSpellIndex = i;
                Game1.playSound("smallSelect");

                // ✅ Prepare the spell instead of casting
                SpellRegistry.SelectedSpell = spells[selectedSpellIndex];
                Game1.addHUDMessage(new HUDMessage($"Prepared {spells[selectedSpellIndex].Name}", HUDMessage.newQuest_type));

                Game1.exitActiveMenu();
                break;
            }
        }
    }


    // Optional: allow arrow keys to navigate list
    public override void receiveKeyPress(Microsoft.Xna.Framework.Input.Keys key)
    {
        if (key == Microsoft.Xna.Framework.Input.Keys.Up)
        {
            selectedSpellIndex = (selectedSpellIndex - 1 + spells.Count) % spells.Count;
            Game1.playSound("shiny4");
        }
        else if (key == Microsoft.Xna.Framework.Input.Keys.Down)
        {
            selectedSpellIndex = (selectedSpellIndex + 1) % spells.Count;
            Game1.playSound("shiny4");
        }
        else if (key == Microsoft.Xna.Framework.Input.Keys.Enter)
        {
            spells[selectedSpellIndex].Cast(Game1.player);
            Game1.exitActiveMenu();
            Game1.playSound("coin");
        }
        else if (key == Microsoft.Xna.Framework.Input.Keys.Escape)
        {
            Game1.exitActiveMenu();
        }
    }
}
