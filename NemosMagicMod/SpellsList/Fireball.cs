using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Projectiles;

namespace NemosMagicMod.Spells
{
    public class Fireball : Spell
    {
        public override bool IsUnlocked => SpellRegistry.PlayerData.UnlockedSpellIds.Contains(this.Id);
        protected override bool UseBookAnimation => false;        // skip book animation
        protected override bool FreezePlayerDuringCast => false;


        public Fireball()
            : base(
                id: "nemo.Fireball",
                name: "Fireball",
                description: "Throws a fiery explosive projectile.",
                manaCost: 5,
                experienceGained: 1
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

            Vector2 cursorScreenPos = new Vector2(Game1.getMouseX(), Game1.getMouseY());
            Vector2 cursorWorldPos = cursorScreenPos + new Vector2(Game1.viewport.X, Game1.viewport.Y);

            Vector2 startPosition = who.Position;
            Vector2 velocity = cursorWorldPos - startPosition;
            velocity.Normalize();
            velocity *= 10f;

            Texture2D projectileTexture = Game1.mouseCursors;
            Rectangle sourceRect = Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 0, 16, 16);

            var fireball = new BasicProjectile(
                damageToFarmer: 25,
                spriteIndex: 0,
                bouncesTillDestruct: 0,
                tailLength: 0,
                rotationVelocity: 0f,
                xVelocity: velocity.X,
                yVelocity: velocity.Y,
                startingPosition: who.getStandingPosition(),
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
