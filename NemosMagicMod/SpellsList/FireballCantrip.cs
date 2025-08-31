using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Projectiles;

namespace NemosMagicMod.Spells
{
    public class FireballCantrip : Spell
    {
        public override bool IsUnlocked => SpellRegistry.PlayerData.UnlockedSpellIds.Contains(this.Id);
        protected override bool UseBookAnimation => false;        // skip book animation
        protected override bool FreezePlayerDuringCast => false;


        public FireballCantrip()
            : base(
                id: "nemo.Fireball",
                name: "Fireball",
                description: "Throws a fiery explosive projectile.",
                manaCost: 0,
                experienceGained: 0
            )
        {
            if (!SpellRegistry.PlayerData.UnlockedSpellIds.Contains(this.Id))
                SpellRegistry.PlayerData.UnlockedSpellIds.Add(this.Id);
        }


        public override void Cast(Farmer who)
        {
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            base.Cast(who);

            NemosMagicMod.ModEntry.Instance.Monitor.Log("Fireball cast via spellbook!", LogLevel.Info);

            // Cursor world position
            Vector2 cursorScreenPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());
            Vector2 cursorWorldPos = cursorScreenPos + new Vector2(Game1.viewport.X, Game1.viewport.Y);

            // Start at farmer chest, adjusted by facing direction
            Vector2 chestPosition = who.getStandingPosition();
            switch (who.FacingDirection)
            {
                case 0: // Up
                    chestPosition.Y -= 64f; // higher chest
                    break;
                case 1: // Right
                    chestPosition.X += 32f; // shift to right shoulder
                    chestPosition.Y -= 48f;
                    break;
                case 2: // Down
                    chestPosition.Y -= 32f; // lower since facing down
                    break;
                case 3: // Left
                    chestPosition.X -= 32f; // shift to left shoulder
                    chestPosition.Y -= 48f;
                    break;
            }

            // Calculate velocity from chest toward cursor
            Vector2 velocity = cursorWorldPos - chestPosition;
            velocity.Normalize();
            velocity *= 10f;

            var fireball = new BasicProjectile(
                damageToFarmer: 25,
                spriteIndex: 0,
                bouncesTillDestruct: 0,
                tailLength: 0,
                rotationVelocity: 0f,
                xVelocity: velocity.X,
                yVelocity: velocity.Y,
                startingPosition: chestPosition,
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
