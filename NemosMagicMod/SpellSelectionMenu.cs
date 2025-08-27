using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using StardewValley;
using System.Collections.Generic;
using StardewModdingAPI;

// Namespace should match your mod structure
using NemosMagicMod.Spells;

public class SpellSelectionMenu : IClickableMenu
{
    private readonly List<Spell> spells;
    private int selectedSpellIndex = 0;
    private readonly IModHelper helper;

    private readonly IMonitor Monitor;

    public SpellSelectionMenu(IModHelper helper, IMonitor monitor)
        : base(Game1.uiViewport.Width / 2 - 300, Game1.uiViewport.Height / 2 - 225, 600, 450, true)
    {
        this.helper = helper;
        this.Monitor = monitor;
        this.spells = new List<Spell>();

        foreach (var spell in SpellRegistry.Spells)
        {
            bool unlocked = SpellRegistry.PlayerData.IsSpellUnlocked(spell);
            Monitor.Log($"Spell {spell.Name} (ID: {spell.Id}) unlocked? {unlocked}", LogLevel.Info);

            if (unlocked)
            {
                this.spells.Add(spell);
            }
        }
    }



    public override void draw(SpriteBatch b)
    {
        // Draw menu background
        IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        // Draw title
        SpriteFont font = Game1.dialogueFont;
        string title = "Select a Spell";
        Vector2 titleSize = font.MeasureString(title);
        b.DrawString(font, title, new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, yPositionOnScreen + 15), Color.Black);

        // Get mouse position
        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();

        // Draw spell list
        for (int i = 0; i < spells.Count; i++)
        {
            Vector2 pos = new Vector2(xPositionOnScreen + 30, yPositionOnScreen + 60 + i * 40);
            Rectangle spellRect = new Rectangle((int)pos.X - 10, (int)pos.Y - 5, width - 60, 35);

            bool isHovered = spellRect.Contains(mouseX, mouseY);
            bool isSelected = i == selectedSpellIndex;

            // Simplified background highlight
            if (isSelected)
            {
                b.Draw(Game1.staminaRect, spellRect, Color.Gold * 0.3f);
            }
            else if (isHovered)
            {
                b.Draw(Game1.staminaRect, spellRect, Color.Black * 0.1f);
            }

            // Draw spell name
            Color textColor = isSelected ? Color.DarkGoldenrod : Color.Black;
            b.DrawString(Game1.smallFont, spells[i].Name, pos, textColor);
        }

        // Draw hover tooltip for the hovered spell
        for (int i = 0; i < spells.Count; i++)
        {
            Rectangle spellRect = new Rectangle(xPositionOnScreen + 20, yPositionOnScreen + 60 + i * 40 - 5, width - 40, 35);
            if (spellRect.Contains(mouseX, mouseY))
            {
                string tooltip = $"{spells[i].Description}\nMana Cost: {spells[i].ManaCost}";
                IClickableMenu.drawHoverText(b, tooltip, Game1.smallFont);
                break;
            }
        }

        // Draw cursor
        base.drawMouse(b);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        for (int i = 0; i < spells.Count; i++)
        {
            Rectangle spellRect = new Rectangle(xPositionOnScreen + 30, yPositionOnScreen + 60 + i * 40 - 5, width - 60, 35);
            if (spellRect.Contains(x, y))
            {
                selectedSpellIndex = i;
                Game1.playSound("smallSelect");

                SpellRegistry.SelectedSpell = spells[selectedSpellIndex];
                Game1.addHUDMessage(new HUDMessage($"Prepared {spells[selectedSpellIndex].Name}", HUDMessage.newQuest_type));

                Game1.exitActiveMenu();
                break;
            }
        }
    }

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
