using Microsoft.Xna.Framework;
using StardewValley;
using NemosMagicMod;

namespace NemosMagicMod.Spells
{
    public class Heal : Spell
    {
        private const int HealAmount = 40;

        public Heal() : base("nemo.Heal", "Heal", "Restores some health.", 5, 100)
        {
        }

        public override void Cast(Farmer who)
        {
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

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
