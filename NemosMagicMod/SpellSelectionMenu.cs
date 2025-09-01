using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NemosMagicMod;
using NemosMagicMod.Spells;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;

public class SpellSelectionMenu : IClickableMenu
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Spellbook playerSpellbook;

    private List<Spell> spells = new();
    private int selectedSpellIndex = 0;
    private bool needsRefresh = true;

    private const int maxPerColumn = 8;
    private const int spellSpacing = 40;

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
        var previousSpellCount = spells.Count;
        var previousSelectedSpell = selectedSpellIndex < spells.Count ? spells[selectedSpellIndex] : null;

        spells.Clear();
        foreach (var spell in SpellRegistry.Spells)
        {
            if (SpellRegistry.PlayerData.IsSpellUnlocked(spell, ModEntry.Instance.Config))
                spells.Add(spell);
        }

        if (previousSelectedSpell != null)
        {
            int index = spells.FindIndex(s => s.Id == previousSelectedSpell.Id);
            selectedSpellIndex = index >= 0 ? index : 0;
        }
        else if (SpellRegistry.SelectedSpell != null)
        {
            int index = spells.FindIndex(s => s.Id == SpellRegistry.SelectedSpell.Id);
            selectedSpellIndex = index >= 0 ? index : 0;
        }
        else
        {
            selectedSpellIndex = 0;
        }

        if (selectedSpellIndex >= spells.Count)
            selectedSpellIndex = Math.Max(0, spells.Count - 1);

        needsRefresh = false;

        if (previousSpellCount != spells.Count)
        {
            monitor.Log($"Spell list refreshed: {spells.Count} spells available", LogLevel.Debug);
        }
    }

    public void ForceRefresh() => needsRefresh = true;

    // Persistent hotkey stored in PlayerSaveData
    public string? HotkeyedSpellId
    {
        get => ModEntry.SaveData.HotkeyedSpellId;
        set => ModEntry.SaveData.HotkeyedSpellId = value;
    }

    public override void draw(SpriteBatch b)
    {
        if (needsRefresh) RefreshSpellList();

        int mouseX = Game1.getMouseX();
        int mouseY = Game1.getMouseY();
        studyButtonHovered = studyButton.bounds.Contains(mouseX, mouseY);

        IClickableMenu.drawTextureBox(
            b, Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            xPositionOnScreen, yPositionOnScreen,
            width, height, Color.White, 1f, true
        );

        SpriteFont font = Game1.dialogueFont;
        string title = "Select a Spell";
        Vector2 titleSize = font.MeasureString(title);
        b.DrawString(font, title, new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, yPositionOnScreen + 15), Color.Black);

        int columnWidth = width / 2 - 40;
        Spell hoveredSpell = null;

        for (int i = 0; i < spells.Count; i++)
        {
            int column = i / maxPerColumn;
            int row = i % maxPerColumn;

            Vector2 pos = new Vector2(xPositionOnScreen + 30 + column * (width / 2), yPositionOnScreen + 90 + row * spellSpacing);
            Rectangle spellBorder = new Rectangle((int)pos.X - 10, (int)pos.Y - 5, columnWidth, 35);

            bool isHovered = spellBorder.Contains(mouseX, mouseY);
            bool isSelected = i == selectedSpellIndex;
            bool isHotkeyed = spells[i].Id == HotkeyedSpellId;

            b.Draw(Game1.staminaRect, spellBorder, new Color(150, 100, 50, 180));
            if (isSelected) b.Draw(Game1.staminaRect, spellBorder, new Color(255, 215, 0, 120));
            else if (isHotkeyed) b.Draw(Game1.staminaRect, spellBorder, new Color(100, 255, 100, 120));
            else if (isHovered) b.Draw(Game1.staminaRect, spellBorder, new Color(255, 255, 255, 80));

            Color textColor = isSelected ? Color.DarkGoldenrod : Color.Black;
            b.DrawString(Game1.smallFont, spells[i].Name, pos, textColor);

            if (isHovered) hoveredSpell = spells[i];
        }

        if (spells.Count == 0)
        {
            string noSpellsText = "No spells available";
            Vector2 noSpellsTextSize = Game1.smallFont.MeasureString(noSpellsText);
            b.DrawString(Game1.smallFont, noSpellsText,
                new Vector2(xPositionOnScreen + width / 2 - noSpellsTextSize.X / 2, yPositionOnScreen + height / 2 - noSpellsTextSize.Y / 2),
                Color.Gray);
        }

        if (hoveredSpell != null)
        {
            string tooltip = $"{hoveredSpell.Description}\nMana Cost: {hoveredSpell.ManaCost}";
            if (hoveredSpell.Id == HotkeyedSpellId)
                tooltip += "\n[Right-click to remove hotkey]";
            else
                tooltip += "\n[Right-click to assign hotkey]";

            IClickableMenu.drawHoverText(b, tooltip, Game1.smallFont);
        }

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

        if (spells.Count == 0) return;
        SelectSpellAtPoint(x, y);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        if (spells.Count == 0) return;

        for (int i = 0; i < spells.Count; i++)
        {
            int column = i / maxPerColumn;
            int row = i % maxPerColumn;

            Rectangle spellBorder = new Rectangle(
                xPositionOnScreen + 30 + column * (width / 2) - 10,
                yPositionOnScreen + 90 + row * spellSpacing - 5,
                width / 2 - 40, 35
            );

            if (spellBorder.Contains(x, y))
            {
                string spellId = spells[i].Id;
                if (HotkeyedSpellId == spellId)
                {
                    HotkeyedSpellId = null;
                    Game1.addHUDMessage(new HUDMessage($"Removed hotkey from {spells[i].Name}", HUDMessage.newQuest_type));
                }
                else
                {
                    HotkeyedSpellId = spellId;
                    Game1.addHUDMessage(new HUDMessage($"Assigned hotkey to {spells[i].Name}", HUDMessage.newQuest_type));
                }
                Game1.playSound("smallSelect");
                break;
            }
        }
    }

    private void SelectSpellAtPoint(int x, int y)
    {
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
}
