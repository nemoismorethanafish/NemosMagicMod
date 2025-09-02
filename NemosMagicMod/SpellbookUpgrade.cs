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
using SpaceCore;

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

        // Experience rewards per tier upgrade
        public static readonly int[] ExperienceRewardPerTier = { 100, 500, 1000 }; // Apprentice, Adept, Master

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

        // Grant Magic skill experience
        public static void GrantMagicExperience(int amount, IMonitor monitor = null)
        {
            try
            {
                // Using SpaceCore's Skills system to add experience
                Skills.AddExperience(Game1.player, "nemosmagicmod.Magic", amount);
                monitor?.Log($"Granted {amount} Magic experience from spellbook study", LogLevel.Info);
            }
            catch (Exception ex)
            {
                monitor?.Log($"Error granting Magic experience: {ex.Message}", LogLevel.Error);
            }
        }

        // --- Upgrade Menu ---
        public class SpellbookUpgradeMenu : IClickableMenu
        {
            private readonly Spellbook spellbook;
            private readonly IMonitor monitor;

            private readonly List<(string name, int count)> requiredMaterials;
            private readonly int goldCost;
            private readonly int experienceReward;

            private readonly ClickableComponent upgradeButton;
            private readonly ClickableComponent cancelButton;

            public SpellbookUpgradeMenu(Farmer player, Spellbook spellbook, IMonitor monitor)
                : base((Game1.uiViewport.Width - 800) / 2, (Game1.uiViewport.Height - 576) / 2, 800, 576, false)
            {
                this.spellbook = spellbook;
                this.monitor = monitor;

                int nextTierIndex = (int)spellbook.Tier;

                // Safety check
                if (nextTierIndex >= GoldCostPerTier.Length || nextTierIndex >= MaterialCostPerTier.Length)
                {
                    monitor.Log($"No upgrade available for tier {spellbook.Tier}", LogLevel.Warn);
                    Game1.showRedMessage("This Spellbook cannot be upgraded further.");
                    Game1.exitActiveMenu();
                    return;
                }

                goldCost = GoldCostPerTier[nextTierIndex];
                requiredMaterials = MaterialCostPerTier[nextTierIndex].ToList();
                experienceReward = ExperienceRewardPerTier[nextTierIndex];

                int buttonWidth = 220;
                int buttonHeight = 70;
                int buttonSpacing = 40;
                int buttonY = yPositionOnScreen + height - 90;

                upgradeButton = new ClickableComponent(
                    new Rectangle(
                        xPositionOnScreen + width / 2 - buttonWidth - buttonSpacing / 2,
                        buttonY,
                        buttonWidth,
                        buttonHeight),
                    "Study"
                );

                cancelButton = new ClickableComponent(
                    new Rectangle(
                        xPositionOnScreen + width / 2 + buttonSpacing / 2,
                        buttonY,
                        buttonWidth,
                        buttonHeight),
                    "Cancel"
                );
            }

            private bool CanStudy => Game1.timeOfDay < 1800; // true before 6 PM

            public override void draw(SpriteBatch b)
            {
                // Draw main menu box
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

                // Title: show next tier
                SpellbookTier nextTier = spellbook.Tier < SpellbookTier.Master ? spellbook.Tier + 1 : SpellbookTier.Master;
                SpriteText.drawString(
                    b,
                    $"Upgrade Spellbook: {nextTier}",
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

                // Experience reward
                SpriteText.drawString(
                    b,
                    $"Magic XP: +{experienceReward}",
                    xPositionOnScreen + padding,
                    yPositionOnScreen + padding + 80
                );

                // Draw required materials
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
                int scale = 4;
                int offsetY = yPositionOnScreen + padding + 160; // Adjusted for XP display

                foreach (var (name, count) in requiredMaterials)
                {
                    int playerCount = Game1.player.Items.Where(i => i != null && i.Name == name).Sum(i => i.Stack);

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

                // Draw buttons
                Color studyColor = CanStudy ? Color.White : Color.Gray;

                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    upgradeButton.bounds.X,
                    upgradeButton.bounds.Y,
                    upgradeButton.bounds.Width,
                    upgradeButton.bounds.Height,
                    studyColor,
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
                    "Study",
                    upgradeButton.bounds.X + 40,
                    upgradeButton.bounds.Y + 18
                );

                SpriteText.drawString(
                    b,
                    "Cancel",
                    cancelButton.bounds.X + 40,
                    cancelButton.bounds.Y + 18
                );

                // Hover tooltip for Study button
                if (upgradeButton.containsPoint(Game1.getOldMouseX(), Game1.getOldMouseY()))
                {
                    string tooltip = CanStudy
                        ? $"Studying this spellbook will advance it to the next tier and grant {experienceReward} Magic XP. It will take 6 hours."
                        : "Cannot study after 6 PM.";

                    // Use the same hover text method as SpellSelectionMenu
                    IClickableMenu.drawHoverText(
                        b,
                        tooltip,
                        Game1.smallFont
                    );
                }

                base.draw(b);
                Game1.mouseCursorTransparency = 1f;
                drawMouse(b);
            }

            public override void receiveLeftClick(int x, int y, bool playSound = true)
            {
                if (upgradeButton.containsPoint(x, y))
                {
                    if (CanStudy)
                        AttemptUpgrade();
                    else
                        Game1.showRedMessage("Cannot study after 6 PM.");
                }
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
                spellbook.UpdateTierAppearance();

                // Grant Magic experience
                GrantMagicExperience(experienceReward, monitor);

                Game1.showGlobalMessage($"Your Spellbook has been upgraded to {spellbook.Tier}! (+{experienceReward} Magic XP)");
                monitor.Log($"Spellbook upgraded to {spellbook.Tier}, granted {experienceReward} Magic XP", LogLevel.Info);

                // Advance time by 6 hours
                Game1.timeOfDay += 600;
                if (Game1.timeOfDay >= 2600) Game1.timeOfDay = 2600; // cap at 2:00 AM

                Game1.exitActiveMenu();
            }
        }
    }
}