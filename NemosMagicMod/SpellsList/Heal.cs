using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewValley;

namespace NemosMagicMod.Spells
{
    public class Heal : Spell
    {
        private const int HealAmount = 40;
        private Texture2D healTexture;


        public Heal() : base("nemo.Heal", "Heal", "Fully restores health.", 30, 10, false, "assets/Heal.png")
        {
            healTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/Heal.png");
            iconTexture = healTexture;
        }

        public override void Cast(Farmer who)
        {
            if (!CanCast(who))
                return;

            base.Cast(who);

            if (who.health < who.maxHealth)
            {
                who.health = System.Math.Min(who.health + HealAmount, who.maxHealth);
                Game1.showGlobalMessage("You feel rejuvenated!");
            }
            else
            {
                Game1.showRedMessage("You're already at full health.");
            }
        }

        public override void Update(GameTime gameTime, Farmer who)
        {
            // Heal is instant, so no ongoing effects
        }
    }
}
