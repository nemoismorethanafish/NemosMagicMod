//using NemosMagicMod.Spells;
//using StardewModdingAPI;
//using StardewValley;
//using StardewValley.BellsAndWhistles;
//using StardewValley.Menus;
//using System.Collections.Generic;
//using System.Linq;


//public class SpellbookUpgradeMenu : IClickableMenu
//{
//    private readonly Spellbook spellbook;
//    private readonly IMonitor monitor;
//    private readonly List<(string name, int count)> requiredMaterials;
//    private readonly int goldCost;

//    private readonly ClickableComponent upgradeButton;
//    private readonly ClickableComponent cancelButton;

//    public SpellbookUpgradeMenu(Farmer player, Spellbook spellbook, IMonitor monitor)
//    {
//        this.spellbook = spellbook;
//        this.monitor = monitor;

//        int nextTierIndex = (int)spellbook.Tier;
//        goldCost = SpellbookUpgradeSystem.GoldCostPerTier[nextTierIndex];
//        requiredMaterials = SpellbookUpgradeSystem.MaterialCostPerTier[nextTierIndex].ToList();

//        int menuWidth = 400;
//        int menuHeight = 300;
//        xPositionOnScreen = (Game1.uiViewport.Width - menuWidth) / 2;
//        yPositionOnScreen = (Game1.uiViewport.Height - menuHeight) / 2;
//        width = menuWidth;
//        height = menuHeight;

//        upgradeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + 50, yPositionOnScreen + height - 60, 120, 40), "Upgrade");
//        cancelButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 170, yPositionOnScreen + height - 60, 120, 40), "Cancel");
//    }

//    public override void draw(Microsoft.Xna.Framework.Graphics.SpriteBatch b)
//    {
//        // Draw background
//        IClickableMenu.drawTextureBox(
//            b,
//            Game1.menuTexture,
//            new Microsoft.Xna.Framework.Rectangle(0, 0, 16, 16),
//            xPositionOnScreen,
//            yPositionOnScreen,
//            width,
//            height,
//            Microsoft.Xna.Framework.Color.White
//        );

//        // Draw title and requirements
//        SpriteText.drawString(b, $"Upgrade Spellbook: {spellbook.Tier}", xPositionOnScreen + 20, yPositionOnScreen + 20);
//        SpriteText.drawString(b, $"Gold: {goldCost}", xPositionOnScreen + 20, yPositionOnScreen + 60);

//        int offsetY = 100;
//        foreach (var (name, count) in requiredMaterials)
//        {
//            int playerCount = Game1.player.Items.Where(i => i != null && i.Name == name).Sum(i => i.Stack);
//            SpriteText.drawString(b, $"{name}: {playerCount}/{count}", xPositionOnScreen + 20, yPositionOnScreen + offsetY);
//            offsetY += 30;
//        }

//        // Draw buttons
//        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 0, 16, 16),
//            upgradeButton.bounds.X, upgradeButton.bounds.Y, upgradeButton.bounds.Width, upgradeButton.bounds.Height, Color.White);
//        IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 0, 16, 16),
//            cancelButton.bounds.X, cancelButton.bounds.Y, cancelButton.bounds.Width, cancelButton.bounds.Height, Color.White);

//        SpriteText.drawString(b, "Upgrade", upgradeButton.bounds.X + 20, upgradeButton.bounds.Y + 10);
//        SpriteText.drawString(b, "Cancel", cancelButton.bounds.X + 20, cancelButton.bounds.Y + 10);

//        base.draw(b);
//        Game1.mouseCursorTransparency = 1f;
//        drawMouse(b);
//    }

//    public override void receiveLeftClick(int x, int y, bool playSound = true)
//    {
//        if (upgradeButton.containsPoint(x, y))
//            AttemptUpgrade();
//        else if (cancelButton.containsPoint(x, y))
//            Game1.exitActiveMenu();
//    }

//    private void AttemptUpgrade()
//    {
//        Farmer player = Game1.player;

//        if (player.Money < goldCost)
//        {
//            Game1.showRedMessage("Not enough gold!");
//            return;
//        }

//        foreach (var (name, count) in requiredMaterials)
//        {
//            int playerCount = player.Items.Where(i => i != null && i.Name == name).Sum(i => i.Stack);
//            if (playerCount < count)
//            {
//                Game1.showRedMessage($"Missing {name} x{count - playerCount}");
//                return;
//            }
//        }

//        player.Money -= goldCost;
//        foreach (var (name, count) in requiredMaterials)
//            SpellbookUpgradeSystem.RemoveItemsFromInventory(player, name, count);

//        spellbook.Tier++;
//        Game1.showGlobalMessage($"Your Spellbook has been upgraded to {spellbook.Tier}!");
//        monitor.Log($"Spellbook upgraded to {spellbook.Tier}", LogLevel.Info);

//        Game1.exitActiveMenu();
//    }
//}
