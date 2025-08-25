using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace NemosMagicMod.Menus
{
    internal class ProfessionChoiceMenu : IClickableMenu
    {
        private readonly Action<MagicProfession> onChosen;

        private readonly List<ClickableComponent> options = new();
        private readonly List<string> optionNames = new() { "Wind Spirit", "Fireball" };
        private readonly List<MagicProfession> professions = new() { MagicProfession.WindSpirit, MagicProfession.Fireball };

        private const int ButtonWidth = 300;
        private const int ButtonHeight = 100;

        public ProfessionChoiceMenu(Action<MagicProfession> onChosen)
        {
            this.onChosen = onChosen;

            width = ButtonWidth + 64;
            height = (ButtonHeight + 32) * optionNames.Count;
            xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
            yPositionOnScreen = Game1.uiViewport.Height / 2 - height / 2;

            for (int i = 0; i < optionNames.Count; i++)
            {
                options.Add(new ClickableComponent(
                    new Rectangle(
                        xPositionOnScreen + 32,
                        yPositionOnScreen + 32 + i * (ButtonHeight + 32),
                        ButtonWidth,
                        ButtonHeight),
                    optionNames[i]
                ));
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].containsPoint(x, y))
                {
                    Game1.playSound("smallSelect");
                    onChosen(professions[i]);
                    exitThisMenu();
                    return;
                }
            }
        }

        public override void draw(SpriteBatch b)
        {
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

            for (int i = 0; i < options.Count; i++)
            {
                IClickableMenu.drawTextureBox(
                    b,
                    Game1.menuTexture,
                    new Rectangle(0, 256, 60, 60),
                    options[i].bounds.X,
                    options[i].bounds.Y,
                    options[i].bounds.Width,
                    options[i].bounds.Height,
                    Color.White,
                    1f,
                    false
                );

                Utility.drawTextWithShadow(
                    b,
                    optionNames[i],
                    Game1.dialogueFont,
                    new Vector2(options[i].bounds.X + 20, options[i].bounds.Y + 25),
                    Game1.textColor
                );
            }

            drawMouse(b);
        }
    }
}
