using Microsoft.Xna.Framework;
using StardewValley;

namespace NemosMagicMod.Spells
{
    public class Heal : Spell
    {
        private const int HealAmount = 40;

        public Heal()
            : base(
                name: "Heal",
                description: "Restores some health.",
                manaCost: 5,
                experienceGained: 100,
                skillId: "nemosmagicmod.Magic") // 👈 Pass your actual SpaceCore skill ID
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
    }
}
