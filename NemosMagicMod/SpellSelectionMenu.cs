using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NemosMagicMod;
using NemosMagicMod.Spells;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Linq;

public class SpellSelectionMenu : IClickableMenu
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Spellbook playerSpellbook;

    private List<Spell> spells = new();
    private int selectedSpellIndex = 0;

    private const int maxPerColumn = 8;
    private const int spellSpacing = 40;

    // Study button
    private ClickableComponent studyButton;
    private bool studyButtonHovered = false;

    public SpellSelectionMenu(IModHelper helper, IMonitor monitor, Spellbook spellbook)
        : base(Game1.uiViewport.Width / 2 - 300, Game1.uiViewport.Height / 2 - 225, 600, 450, true)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.playerSpellbook = spellbook;

        RefreshSpellList();

        int buttonWidth = 180;
        int buttonHeight = 50;
        studyButton = new ClickableComponent(
            new Rectangle(
                xPositionOnScreen + width - buttonWidth - 20,
                yPositionOnScreen + height - buttonHeight - 20,
                buttonWidth,
                buttonHeight
            ),
            "Study"
        );
    }

    private void RefreshSpellList()
    {
        spells.Clear();
        foreach (var spell in SpellRegistry.Spells)
            if (SpellRegistry.PlayerData.IsSpellUnlocked(spell, ModEntry.Instance.Config))
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

        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();
        studyButtonHovered = studyButton.bounds.Contains(mouseX, mouseY);

        // Menu background
        IClickableMenu.drawTextureBox(
            b, Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            xPositionOnScreen, yPositionOnScreen,
            width, height, Color.White, 1f, true
        );

        // Title
        SpriteFont font = Game1.dialogueFont;
        string title = "Select a Spell";
        Vector2 titleSize = font.MeasureString(title);
        b.DrawString(font, title, new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, yPositionOnScreen + 15), Color.Black);

        int columnWidth = width / 2 - 40;
        Spell hoveredSpell = null;

        // Draw spells
        for (int i = 0; i < spells.Count; i++)
        {
            int column = i / maxPerColumn;
            int row = i % maxPerColumn;

            Vector2 pos = new Vector2(xPositionOnScreen + 30 + column * (width / 2), yPositionOnScreen + 90 + row * spellSpacing);
            Rectangle spellBorder = new Rectangle((int)pos.X - 10, (int)pos.Y - 5, columnWidth, 35);

            bool isHovered = spellBorder.Contains(mouseX, mouseY);
            bool isSelected = i == selectedSpellIndex;

            b.Draw(Game1.staminaRect, spellBorder, new Color(150, 100, 50, 180));
            if (isSelected) b.Draw(Game1.staminaRect, spellBorder, new Color(255, 215, 0, 120));
            else if (isHovered) b.Draw(Game1.staminaRect, spellBorder, new Color(255, 255, 255, 80));

            Color textColor = isSelected ? Color.DarkGoldenrod : Color.Black;
            b.DrawString(Game1.smallFont, spells[i].Name, pos, textColor);

            if (isHovered) hoveredSpell = spells[i];
        }

        // Tooltip
        if (hoveredSpell != null)
        {
            string tooltip = $"{hoveredSpell.Description}\nMana Cost: {hoveredSpell.ManaCost}";
            IClickableMenu.drawHoverText(b, tooltip, Game1.smallFont);
        }

        // Draw Study button
        Color buttonColor = studyButtonHovered ? new Color(255, 215, 0, 180) : Color.White;
        IClickableMenu.drawTextureBox(
            b, Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            studyButton.bounds.X, studyButton.bounds.Y,
            studyButton.bounds.Width, studyButton.bounds.Height,
            buttonColor, 1f, true
        );

        Vector2 textSize = Game1.smallFont.MeasureString("Study");
        b.DrawString(Game1.smallFont, "Study",
            new Vector2(studyButton.bounds.X + (studyButton.bounds.Width - textSize.X) / 2,
                        studyButton.bounds.Y + (studyButton.bounds.Height - textSize.Y) / 2),
            Color.Black
        );

        base.drawMouse(b);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (studyButton.containsPoint(x, y))
        {
            Game1.playSound("smallSelect");
            Game1.activeClickableMenu = new SpellbookUpgradeSystem.SpellbookUpgradeMenu(
                Game1.player, playerSpellbook, monitor
            );
            return;
        }

        int columnWidth = width / 2 - 40;
        for (int i = 0; i < spells.Count; i++)
        {
            int column = i / maxPerColumn;
            int row = i % maxPerColumn;

            Rectangle spellBorder = new Rectangle(
                xPositionOnScreen + 30 + column * (width / 2) - 10,
                yPositionOnScreen + 90 + row * spellSpacing - 5,
                columnWidth, 35
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
            if (selectedSpellIndex % maxPerColumn > 0) selectedSpellIndex--;
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
            if (selectedSpellIndex >= maxPerColumn) selectedSpellIndex -= maxPerColumn;
            Game1.playSound("shiny4");
        }
        else if (key == Keys.Right)
        {
            if (selectedSpellIndex + maxPerColumn < spells.Count) selectedSpellIndex += maxPerColumn;
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
