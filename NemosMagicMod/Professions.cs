using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpaceCore;
using StardewModdingAPI;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace NemosMagicMod
{
    // === Handles saving/loading profession choice ===
    public static class ProfessionManager
    {
        private const string ProfessionKey = "NemosMagicMod.Profession";

        public static void SetProfession(Farmer player, string professionId)
        {
            player.modData[ProfessionKey] = professionId;
        }

        public static string GetProfession(Farmer player)
        {
            if (player.modData.TryGetValue(ProfessionKey, out string? val))
                return val;
            return "None";
        }
    }

    // === Manages professions at level-up ===
    public class Professions
    {
        private readonly IMonitor Monitor;
        private readonly IModHelper Helper;

        public Professions(IModHelper helper, IMonitor monitor)
        {
            Helper = helper;
            Monitor = monitor;

            Helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void OnDayStarted(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            Farmer player = Game1.player;

            int magicLevel = Skills.GetSkillLevel(player, "nemosmagicmod.Magic");

            if (magicLevel >= 5 && ProfessionManager.GetProfession(player) == "None")
            {
                Game1.activeClickableMenu = new ProfessionChoiceMenu(player);
            }
        }

        // Apply effects once chosen
        public static void AssignProfession(Farmer player, string professionId)
        {
            ProfessionManager.SetProfession(player, professionId);

            Game1.showGlobalMessage($"You chose the {professionId} profession!");
        }
    }

    // === Custom choice menu for professions ===
    public class ProfessionChoiceMenu : IClickableMenu
    {
        private readonly Farmer player;
        private readonly ClickableComponent windButton;
        private readonly ClickableComponent fireButton;

        public ProfessionChoiceMenu(Farmer farmer)
            : base(Game1.viewport.Width / 2 - 300, Game1.viewport.Height / 2 - 200, 600, 400, true)
        {
            player = farmer;

            windButton = new ClickableComponent(
                new Rectangle(xPositionOnScreen + 100, yPositionOnScreen + 150, 400, 64),
                "WindSpiritCantrip"
            );

            fireButton = new ClickableComponent(
                new Rectangle(xPositionOnScreen + 100, yPositionOnScreen + 250, 400, 64),
                "FireballCantrip"
            );
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                xPositionOnScreen, yPositionOnScreen, width, height,
                Color.White, 1f, true
            );

            SpriteText.drawStringHorizontallyCenteredAt(
                b, "Choose Your Cantrip",
                xPositionOnScreen + width / 2, yPositionOnScreen + 50
            );

            drawButton(b, windButton.bounds, "Wind Spirit (Free spell)");
            drawButton(b, fireButton.bounds, "Fireball (Free spell)");

            base.draw(b);
            drawMouse(b);
        }

        private void drawButton(SpriteBatch b, Rectangle rect, string text)
        {
            IClickableMenu.drawTextureBox(
                b, Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                rect.X, rect.Y, rect.Width, rect.Height,
                Color.White, 1f, false
            );
            SpriteText.drawStringHorizontallyCenteredAt(b, text, rect.X + rect.Width / 2, rect.Y + 20);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (windButton.containsPoint(x, y))
            {
                Professions.AssignProfession(player, "WindSpiritCantrip");
                exitThisMenu();
            }
            else if (fireButton.containsPoint(x, y))
            {
                Professions.AssignProfession(player, "FireballCantrip");
                exitThisMenu();
            }
        }
    }
}
