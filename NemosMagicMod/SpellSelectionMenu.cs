using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

public class SpellSelectionMenu : IClickableMenu
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    private List<Spell> spells = new();
    private int selectedSpellIndex = 0;

    // Layout
    private const int maxPerColumn = 8;
    private const int spellSpacing = 40;

    public SpellSelectionMenu(IModHelper helper, IMonitor monitor)
        : base(Game1.uiViewport.Width / 2 - 300, Game1.uiViewport.Height / 2 - 225, 600, 450, true)
    {
        this.helper = helper;
        this.monitor = monitor;

        RefreshSpellList();
    }

    private void RefreshSpellList()
    {
        spells.Clear();
        foreach (var spell in SpellRegistry.Spells)
            if (SpellRegistry.PlayerData.IsSpellUnlocked(spell))
                spells.Add(spell);

        if (SpellRegistry.SelectedSpell != null)
        {
            int index = spells.FindIndex(s => s.Id == SpellRegistry.SelectedSpell.Id);
            selectedSpellIndex = index >= 0 ? index : 0;
        }
        else
        {
            selectedSpellIndex = 0;
        }
    }

    public override void draw(SpriteBatch b)
    {
        RefreshSpellList();

        // Draw menu background
        b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), new Color(240, 230, 180));

        // Draw title
        SpriteFont font = Game1.dialogueFont;
        string title = "Select a Spell";
        Vector2 titleSize = font.MeasureString(title);
        b.DrawString(font, title, new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, yPositionOnScreen + 15), Color.Black);

        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();

        int columnWidth = width / 2 - 40;

        // Keep track of hovered spell for tooltip
        Spell hoveredSpell = null;

        // Draw two-column spell list
        for (int i = 0; i < spells.Count; i++)
        {
            int column = i / maxPerColumn;
            int row = i % maxPerColumn;

            Vector2 pos = new Vector2(
                xPositionOnScreen + 30 + column * (width / 2),
                yPositionOnScreen + 60 + row * spellSpacing
            );

            Rectangle spellBorder = new Rectangle((int)pos.X - 10, (int)pos.Y - 5, columnWidth, 35);

            bool isHovered = spellBorder.Contains(mouseX, mouseY);
            bool isSelected = i == selectedSpellIndex;

            // Draw subtle border
            b.Draw(Game1.staminaRect, spellBorder, new Color(150, 100, 50, 180));

            // Fill background for hover/selected
            if (isSelected)
                b.Draw(Game1.staminaRect, spellBorder, new Color(255, 215, 0, 120)); // light gold
            else if (isHovered)
                b.Draw(Game1.staminaRect, spellBorder, new Color(255, 255, 255, 80)); // light white

            Color textColor = isSelected ? Color.DarkGoldenrod : Color.Black;
            b.DrawString(Game1.smallFont, spells[i].Name, pos, textColor);

            if (isHovered)
                hoveredSpell = spells[i];
        }

        // Draw tooltip on top of everything
        if (hoveredSpell != null)
        {
            string tooltip = $"{hoveredSpell.Description}\nMana Cost: {hoveredSpell.ManaCost}";
            IClickableMenu.drawHoverText(b, tooltip, Game1.smallFont);
        }

        base.drawMouse(b);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        int columnWidth = width / 2 - 40;

        for (int i = 0; i < spells.Count; i++)
        {
            int column = i / maxPerColumn;
            int row = i % maxPerColumn;

            Rectangle spellBorder = new Rectangle(
                xPositionOnScreen + 30 + column * (width / 2) - 10,
                yPositionOnScreen + 60 + row * spellSpacing - 5,
                columnWidth,
                35
            );

            if (spellBorder.Contains(x, y))
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

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.Up)
        {
            if (selectedSpellIndex % maxPerColumn > 0)
                selectedSpellIndex--;
            Game1.playSound("shiny4");
        }
        else if (key == Keys.Down)
        {
            if (selectedSpellIndex % maxPerColumn < maxPerColumn - 1 && selectedSpellIndex + 1 < spells.Count)
                selectedSpellIndex++;
            Game1.playSound("shiny4");
        }
        else if (key == Keys.Left)
        {
            if (selectedSpellIndex >= maxPerColumn)
                selectedSpellIndex -= maxPerColumn;
            Game1.playSound("shiny4");
        }
        else if (key == Keys.Right)
        {
            if (selectedSpellIndex + maxPerColumn < spells.Count)
                selectedSpellIndex += maxPerColumn;
            Game1.playSound("shiny4");
        }
        else if (key == Keys.Enter)
        {
            spells[selectedSpellIndex].Cast(Game1.player);
            Game1.exitActiveMenu();
            Game1.playSound("coin");
        }
        else if (key == Keys.Escape)
        {
            Game1.exitActiveMenu();
        }
    }
}
