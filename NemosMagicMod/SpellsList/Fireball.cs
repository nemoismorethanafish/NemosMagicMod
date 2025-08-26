using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Projectiles;

namespace NemosMagicMod.Spells
{
    public class Fireball : Spell
    {
        public Fireball()
            : base(
                name: "Fireball",
                description: "Throws a fiery explosive projectile.",
                manaCost: 10,
                experienceGained: 5,
                skillId: "nemosmagicmod.Magic") // ✅ Make sure to pass the full SpaceCore skill ID
        {
        }

        public override void Cast(Farmer who)
        {
            base.Cast(who);

            // Check mana again because base.Cast returns early if not enough mana
            if (!ManaManager.HasEnoughMana(ManaCost))
                return;

            NemosMagicMod.ModEntry.Instance.Monitor.Log("Fireball cast via spellbook!", LogLevel.Info);

            // Get mouse world position
            Vector2 cursorScreenPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());
            Vector2 cursorWorldPos = cursorScreenPos + new Vector2(Game1.viewport.X, Game1.viewport.Y);

            // Calculate direction
            Vector2 startPosition = who.getStandingPosition();
            Vector2 velocity = cursorWorldPos - startPosition;
            velocity.Normalize();
            velocity *= 10f; // Adjust projectile speed as desired

            // Create and launch projectile
            var fireball = new BasicProjectile(
                damageToFarmer: 25,
                spriteIndex: 0,
                bouncesTillDestruct: 0,
                tailLength: 0,
                rotationVelocity: 0f,
                xVelocity: velocity.X,
                yVelocity: velocity.Y,
                startingPosition: startPosition,
                collisionSound: "fireball_hit",
                bounceSound: "fireball_bounce",
                firingSound: "fireball_launch",
                explode: true,
                damagesMonsters: true,
                location: who.currentLocation,
                firer: who,
                collisionBehavior: null,
                shotItemId: null
            );

            who.currentLocation.projectiles.Add(fireball);
        }
    }
}
