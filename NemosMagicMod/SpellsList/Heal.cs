using Microsoft.Xna.Framework;
using StardewValley;

namespace NemosMagicMod.Spells
{
    public class Heal : Spell
    {
        private const int HealAmount = 40;

        public Heal() : base("Heal", 5, "Restores some health.", 100)
        {
        }

        public override void Cast(Farmer who)
        {
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

        public override bool IsExpired()
        {
            return true; // expires instantly
        }
    }
}
