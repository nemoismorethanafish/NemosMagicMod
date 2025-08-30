using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NemosMagicMod.Spells
{
    // --- Tier enum for Spellbook ---
    public enum SpellbookTier
    {
        Novice,    // starting tier
        Apprentice,
        Adept,
        Master     // final tier, no further upgrades
    }

    // --- Upgrade System ---
    public static class SpellbookUpgradeSystem
    {
        // Costs per tier
        public static readonly int[] GoldCostPerTier = { 2000, 5000, 20000 };
        public static readonly (string name, int count)[][] MaterialCostPerTier =
        {
    new[] { ("Frozen Tear", 1), ("Fire Quartz", 1) },       // Apprentice
    new[] { ("Void Essence", 5), ("Solar Essence", 5) },   // Adept
    new[] { ("Prismatic Shard", 1), ("Dragon Tooth", 5) }  // Master
};

        // Remove items from inventory
        public static void RemoveItemsFromInventory(Farmer player, string itemName, int count)
        {
            for (int i = 0; i < player.Items.Count; i++)
            {
                var item = player.Items[i];
                if (item == null || item.Name != itemName)
                    continue;

                if (item.Stack > count)
                {
                    item.Stack -= count;
                    break;
                }
                else
                {
                    count -= item.Stack;
                    player.Items[i] = null;
                    if (count <= 0) break;
                }
            }
        }

        // Offer upgrade dialogue
        // Offer upgrade dialogue
        public static void OfferWizardUpgrade(Farmer player, Spellbook spellbook, IMonitor monitor)
        {
            // Already at highest tier? Don’t show menu
            if (spellbook.Tier >= SpellbookTier.Master)
            {
                Game1.showGlobalMessage("Your Spellbook is already at the highest tier!");
                return;
            }

            Response[] responses = new Response[]
            {
        new Response("Yes", "Yes"),
        new Response("No", "No")
            };

            Game1.currentLocation.createQuestionDialogue(
                $"The Wizard offers to help upgrade your Spellbook from {spellbook.Tier} tier. Study today?",
                responses,
                "UpgradeSpellbook"
            );

            void OnResponse(Farmer who, string whichAnswer)
            {
                Game1.currentLocation.afterQuestion -= OnResponse;

                if (whichAnswer == "Yes")
                {
                    Game1.dialogueUp = false;
                    Game1.activeClickableMenu = null;
                    Game1.currentSpeaker = null;

                    Game1.delayedActions.Add(new DelayedAction(1, () =>
                    {
                        Game1.activeClickableMenu = new SpellbookUpgradeMenu(player, spellbook, monitor);
                    }));
                }
            }

            Game1.currentLocation.afterQuestion += OnResponse;
        }


        // --- Upgrade Menu ---
        public class SpellbookUpgradeMenu : IClickableMenu
        {
            private readonly Spellbook spellbook;
            private readonly IMonitor monitor;

            private readonly List<(string name, int count)> requiredMaterials;
            private readonly int goldCost;

            private readonly ClickableComponent upgradeButton;
            private readonly ClickableComponent cancelButton;

            public SpellbookUpgradeMenu(Farmer player, Spellbook spellbook, IMonitor monitor)
                : base((Game1.uiViewport.Width - 800) / 2, (Game1.uiViewport.Height - 576) / 2, 800, 576, false)
            {
                this.spellbook = spellbook;
                this.monitor = monitor;

                int nextTierIndex = (int)spellbook.Tier;

                // --- SAFETY CHECK: prevent IndexOutOfRange ---
                if (nextTierIndex >= GoldCostPerTier.Length || nextTierIndex >= MaterialCostPerTier.Length)
                {
                    monitor.Log($"No upgrade available for tier {spellbook.Tier}", LogLevel.Warn);
                    Game1.showRedMessage("This Spellbook cannot be upgraded further.");
                    Game1.exitActiveMenu();
                    return;
                }

                // --- safe to read arrays now ---
                goldCost = GoldCostPerTier[nextTierIndex];
                requiredMaterials = MaterialCostPerTier[nextTierIndex].ToList();

                upgradeButton = new ClickableComponent(
                    new Rectangle(xPositionOnScreen + 100, yPositionOnScreen + height - 100, 180, 50),
                    "Upgrade"
                );
                cancelButton = new ClickableComponent(
                    new Rectangle(xPositionOnScreen + width - 280, yPositionOnScreen + height - 100, 180, 50),
                    "Cancel"
                );
            }

            public override void draw(SpriteBatch b)
            {
                // --- Draw main menu box like vanilla inventory ---
                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    xPositionOnScreen,
                    yPositionOnScreen,
                    width,
                    height,
                    Color.White,
                    1f,
                    true
                );

                int padding = 20;

                // Title
                SpriteText.drawString(
                    b,
                    $"Upgrade Spellbook: {spellbook.Tier}",
                    xPositionOnScreen + padding,
                    yPositionOnScreen + padding
                );

                // Gold requirement
                SpriteText.drawString(
                    b,
                    $"Gold: {goldCost}",
                    xPositionOnScreen + padding,
                    yPositionOnScreen + padding + 40
                );

                var itemIndices = new Dictionary<string, int>()
{
    { "Frozen Tear", 84 },
    { "Fire Quartz", 82 },
    { "Void Essence", 769 },
    { "Solar Essence", 768 },
    { "Prismatic Shard", 74 },
    { "Dragon Tooth", 852 },
    { "Iridium Bar", 337 }
};

                int tileSize = 16;
                int scale = 4; // icon scale
                int offsetY = yPositionOnScreen + padding + 120; // moved down 40 pixels

                foreach (var (name, count) in requiredMaterials)
                {
                    int playerCount = Game1.player.Items
                        .Where(i => i != null && i.Name == name)
                        .Sum(i => i.Stack);

                    if (itemIndices.TryGetValue(name, out int parentSheetIndex))
                    {
                        int columns = Game1.objectSpriteSheet.Width / tileSize;
                        int row = parentSheetIndex / columns;
                        int col = parentSheetIndex % columns;
                        Rectangle sourceRect = new Rectangle(col * tileSize, row * tileSize, tileSize, tileSize);

                        b.Draw(
                            Game1.objectSpriteSheet,
                            new Vector2(xPositionOnScreen + padding, offsetY),
                            sourceRect,
                            Color.White,
                            0f,
                            Vector2.Zero,
                            scale,
                            SpriteEffects.None,
                            1f
                        );
                    }

                    SpriteText.drawString(
                        b,
                        $"{name}: {playerCount}/{count}",
                        xPositionOnScreen + padding + tileSize * scale + 10,
                        offsetY + 4
                    );

                    offsetY += tileSize * scale + 20;
                }

                // Buttons at bottom
                int buttonY = yPositionOnScreen + height - 90;
                int buttonWidth = 200; // slightly bigger
                int buttonHeight = 60; // slightly bigger
                int buttonSpacing = 40;

                upgradeButton.bounds = new Rectangle(
                    xPositionOnScreen + width / 2 - buttonWidth - buttonSpacing / 2,
                    buttonY,
                    buttonWidth,
                    buttonHeight
                );

                cancelButton.bounds = new Rectangle(
                    xPositionOnScreen + width / 2 + buttonSpacing / 2,
                    buttonY,
                    buttonWidth,
                    buttonHeight
                );

                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    upgradeButton.bounds.X,
                    upgradeButton.bounds.Y,
                    upgradeButton.bounds.Width,
                    upgradeButton.bounds.Height,
                    Color.White,
                    1f,
                    true
                );

                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    cancelButton.bounds.X,
                    cancelButton.bounds.Y,
                    cancelButton.bounds.Width,
                    cancelButton.bounds.Height,
                    Color.White,
                    1f,
                    true
                );

                SpriteText.drawString(
                    b,
                    "Upgrade",
                    upgradeButton.bounds.X + 40,
                    upgradeButton.bounds.Y + 18
                );

                SpriteText.drawString(
                    b,
                    "Cancel",
                    cancelButton.bounds.X + 40,
                    cancelButton.bounds.Y + 18
                );

                base.draw(b);
                Game1.mouseCursorTransparency = 1f;
                drawMouse(b);
            }


            public override void receiveLeftClick(int x, int y, bool playSound = true)
            {
                if (upgradeButton.containsPoint(x, y))
                    AttemptUpgrade();
                else if (cancelButton.containsPoint(x, y))
                    Game1.exitActiveMenu();
            }

            private void AttemptUpgrade()
            {
                Farmer player = Game1.player;

                // Check gold
                if (player.Money < goldCost)
                {
                    Game1.showRedMessage("Not enough gold!");
                    return;
                }

                // Check materials
                foreach (var (name, count) in requiredMaterials)
                {
                    int playerCount = player.Items.Where(i => i != null && i.Name == name).Sum(i => i.Stack);
                    if (playerCount < count)
                    {
                        Game1.showRedMessage($"Missing {name} x{count - playerCount}");
                        return;
                    }
                }

                // Deduct gold and materials
                player.Money -= goldCost;
                foreach (var (name, count) in requiredMaterials)
                    SpellbookUpgradeSystem.RemoveItemsFromInventory(player, name, count);

                // Upgrade tier
                spellbook.Tier++;
                Game1.showGlobalMessage($"Your Spellbook has been upgraded to {spellbook.Tier}!");
                monitor.Log($"Spellbook upgraded to {spellbook.Tier}", LogLevel.Info);

                Game1.exitActiveMenu();
            }
        }
    }
}
