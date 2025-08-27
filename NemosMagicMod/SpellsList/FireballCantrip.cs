using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Projectiles;

namespace NemosMagicMod.Spells
{
    public class FireballCantrip : Spell
    {
        public FireballCantrip()
            : base("Fireball", "Throws a fiery explosive projectile.", 0, 0, ModEntry.SkillID, false)
        { }

        public override void Cast(Farmer who)
        {
            //if (!ManaManager.HasEnoughMana(ManaCost))
            //{
            //    Game1.showRedMessage("Not enough mana!");
            //    return;
            //}
            base.Cast(who);

            //Game1.showGlobalMessage("You hurl a fireball!");
            NemosMagicMod.ModEntry.Instance.Monitor.Log("Fireball cast via spellbook!", LogLevel.Info);

            // Get mouse cursor world position
            Vector2 cursorScreenPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());
            Vector2 cursorWorldPos = cursorScreenPos + new Vector2(Game1.viewport.X, Game1.viewport.Y);

            // Get direction from player to cursor
            Vector2 startPosition = new Vector2(who.getStandingPosition().X, who.getStandingPosition().Y);
            Vector2 velocity = cursorWorldPos - startPosition;
            velocity.Normalize();
            velocity *= 8f; // Projectile speed

            // Get projectile texture and source rectangle
            Texture2D projectileTexture = Game1.mouseCursors;
            Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16); // Index 0 = fireball sprite

            // Create and launch the fireball projectile
            var fireball = new BasicProjectile(
                damageToFarmer: 25,
                spriteIndex: 0,                   // default sprite index
                bouncesTillDestruct: 0,           // no bounces
                tailLength: 0,                    // no tail
                rotationVelocity: 0f,             // no rotation
                xVelocity: velocity.X,
                yVelocity: velocity.Y,
                startingPosition: who.getStandingPosition(),
                collisionSound: "fireball_hit",  // collision sound name
                bounceSound: "fireball_bounce",  // bounce sound name
                firingSound: "fireball_launch",  // firing sound name
                explode: true,                    // explodes on impact
                damagesMonsters: true,
                location: who.currentLocation,   // current location of the player firing
                firer: who,
                collisionBehavior: null,          // no special collision behavior
                shotItemId: null                  // no specific shot item
            );

            who.currentLocation.projectiles.Add(fireball);


            who.currentLocation.projectiles.Add(fireball);
        }
    }
}
