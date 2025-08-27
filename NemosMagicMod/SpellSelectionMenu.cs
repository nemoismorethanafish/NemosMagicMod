using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;
using Microsoft.Xna.Framework;


public class SpellSelectionMenu : IClickableMenu
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;

    private List<Spell> spells = new();
    private int selectedSpellIndex = 0;

    public SpellSelectionMenu(IModHelper helper, IMonitor monitor)
        : base(Game1.uiViewport.Width / 2 - 300, Game1.uiViewport.Height / 2 - 225, 600, 450, true)
    {
        this.helper = helper;
        this.monitor = monitor;

        RefreshSpellList();
    }

    /// <summary>
    /// Rebuilds the spell list and sets the selected index based on the currently prepared spell.
    /// </summary>
    private void RefreshSpellList()
    {
        spells.Clear();
        foreach (var spell in SpellRegistry.Spells)
        {
            if (SpellRegistry.PlayerData.IsSpellUnlocked(spell))
                spells.Add(spell);
        }

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
        // Always refresh list in case unlocked spells changed since last menu
        RefreshSpellList();

        // Draw menu background
        IClickableMenu.drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

        // Draw title
        SpriteFont font = Game1.dialogueFont;
        string title = "Select a Spell";
        Vector2 titleSize = font.MeasureString(title);
        b.DrawString(font, title, new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, yPositionOnScreen + 15), Color.Black);

        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();

        // Draw spell list
        for (int i = 0; i < spells.Count; i++)
        {
            Vector2 pos = new Vector2(xPositionOnScreen + 30, yPositionOnScreen + 60 + i * 40);
            Rectangle spellRect = new Rectangle((int)pos.X - 10, (int)pos.Y - 5, width - 60, 35);

            bool isHovered = spellRect.Contains(mouseX, mouseY);
            bool isSelected = i == selectedSpellIndex;

            if (isSelected)
                b.Draw(Game1.staminaRect, spellRect, Color.Gold * 0.3f);
            else if (isHovered)
                b.Draw(Game1.staminaRect, spellRect, Color.Black * 0.1f);

            Color textColor = isSelected ? Color.DarkGoldenrod : Color.Black;
            b.DrawString(Game1.smallFont, spells[i].Name, pos, textColor);
        }

        // Draw hover tooltip
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
