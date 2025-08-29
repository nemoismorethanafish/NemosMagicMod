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
        Basic, //remove this later
        Novice,
        Apprentice,
        Adept,
        Master
    }

    // --- Upgrade System ---
    public static class SpellbookUpgradeSystem
    {
        public static bool QueueUpgradeMenu = false;
        public static Farmer? QueuedPlayer = null;
        public static Spellbook? QueuedSpellbook = null;
        public static IMonitor? QueuedMonitor = null;
        // Costs per tier
        public static readonly int[] GoldCostPerTier = { 1000, 2500, 5000 };
        public static readonly (string name, int count)[][] MaterialCostPerTier =
        {
            new[] { ("Fire Quartz", 1), ("Iron Bar", 1) },       // Apprentice
            new[] { ("Iridium Bar", 2), ("Magic Essence", 1) },  // Adept
            new[] { ("Prismatic Shard", 1), ("Solar Essence", 5) } // Master
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
                // Unsubscribe immediately
                Game1.currentLocation.afterQuestion -= OnResponse;

                if (whichAnswer == "Yes")
                {
                    // Fully clear dialogue first
                    Game1.dialogueUp = false;
                    Game1.activeClickableMenu = null;

                    // Clear speaker so game stops thinking a conversation is active
                    Game1.currentSpeaker = null;

                    // Then queue our menu for the next tick
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
                : base((Game1.uiViewport.Width - 400) / 2, (Game1.uiViewport.Height - 300) / 2, 400, 300, false)
            {
                this.spellbook = spellbook;
                this.monitor = monitor;

                int nextTierIndex = (int)spellbook.Tier;
                goldCost = GoldCostPerTier[nextTierIndex];
                requiredMaterials = MaterialCostPerTier[nextTierIndex].ToList();

                upgradeButton = new ClickableComponent(new Rectangle(xPositionOnScreen + 50, yPositionOnScreen + height - 60, 120, 40), "Upgrade");
                cancelButton = new ClickableComponent(new Rectangle(xPositionOnScreen + width - 170, yPositionOnScreen + height - 60, 120, 40), "Cancel");
            }

            public override void draw(SpriteBatch b)
            {
                // --- Draw main menu box with tan background ---
                Color menuColor = new Color(245, 235, 190); // soft tan / parchment color
                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 0, 16, 16),
                    xPositionOnScreen,
                    yPositionOnScreen,
                    width,
                    height,
                    menuColor
                );

                // Draw title
                SpriteText.drawString(b, $"Upgrade Spellbook: {spellbook.Tier}", xPositionOnScreen + 20, yPositionOnScreen + 20);

                // Draw gold requirement
                SpriteText.drawString(b, $"Gold: {goldCost}", xPositionOnScreen + 20, yPositionOnScreen + 60);

                // Draw required materials
                int offsetY = 100;
                foreach (var (name, count) in requiredMaterials)
                {
                    int playerCount = Game1.player.Items
                        .Where(i => i != null && i.Name == name)
                        .Sum(i => i.Stack);

                    SpriteText.drawString(b, $"{name}: {playerCount}/{count}", xPositionOnScreen + 20, yPositionOnScreen + offsetY);
                    offsetY += 30;
                }

                // Draw buttons with default menu texture
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 0, 16, 16), upgradeButton.bounds.X, upgradeButton.bounds.Y, upgradeButton.bounds.Width, upgradeButton.bounds.Height, Color.White);
                IClickableMenu.drawTextureBox(b, Game1.menuTexture, new Rectangle(0, 0, 16, 16), cancelButton.bounds.X, cancelButton.bounds.Y, cancelButton.bounds.Width, cancelButton.bounds.Height, Color.White);

                SpriteText.drawString(b, "Upgrade", upgradeButton.bounds.X + 20, upgradeButton.bounds.Y + 10);
                SpriteText.drawString(b, "Cancel", cancelButton.bounds.X + 20, cancelButton.bounds.Y + 10);

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
