using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using static SpellRegistry;

namespace NemosMagicMod.Spells
{
    public class HomeWarp : Spell, Spell.IRenderable
    {
        private const int FadeDuration = 1000; // milliseconds
        protected override SpellbookTier MinimumTier => SpellbookTier.Adept;

        public HomeWarp()
            : base("nemo.HomeWarp", "Home Warp", "Teleports you safely to your farmhouse.", 50)
        {
        }

        protected override bool FreezePlayerDuringCast => true;

        public override void Cast(Farmer who)
        {
            // --- Not Enough Mana check ---
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            // --- Minimum spellbook tier check ---
            if (!HasSufficientSpellbookTier(who))
            {
                string requiredTierName = MinimumTier.ToString(); // "Adept"
                Game1.showRedMessage($"Requires {requiredTierName} spellbook or higher!");
                return; // Cancel spell
            }

            // Check if already home BEFORE spending mana
            GameLocation home = Game1.getLocationFromName(who.homeLocation.Value) ?? Game1.getFarm();
            if (who.currentLocation == home)
            {
                ModEntry.Instance.Monitor.Log(
                    "HomeWarp: Player is already in their home. Warp canceled.",
                    LogLevel.Info
                );
                return; // Exit early
            }

            // --- Base cast (spends mana, triggers standard effects) ---
            base.Cast(who);

            // --- Delay everything by 1 second (1000ms) ---
            DelayedAction.functionAfterDelay(() =>
            {
                // Play teleport bubble animation
                for (int i = 0; i < 12; i++)
                {
                    who.currentLocation.temporarySprites.Add(
                        new TemporaryAnimatedSprite(
                            354,
                            Game1.random.Next(25, 75),
                            6,
                            1,
                            who.Position + new Vector2(Game1.random.Next(-64, 64), Game1.random.Next(-64, 64)),
                            flicker: false,
                            Game1.random.NextBool()
                        )
                    );
                }

                who.playNearbySoundAll("wand");
                Game1.displayFarmer = false;
                who.temporarilyInvincible = true;
                who.temporaryInvincibilityTimer = -2000;
                who.freezePause = 1000;
                Game1.flashAlpha = 1f;

                // Warp the player
                WarpToHome(who);
            }, 1000);
        }

        private void WarpToHome(Farmer who)
        {
            // Get the player's home location
            GameLocation home = Game1.getLocationFromName(who.homeLocation.Value) ?? Game1.getFarm();

            // If the player is already in their home, don't warp
            if (who.currentLocation == home)
            {
                ModEntry.Instance.Monitor.Log(
                    "HomeWarp: Player is already in their home. Warp canceled.",
                    LogLevel.Info
                );
                return; // exit early
            }

            ModEntry.Instance.Monitor.Log(
                $"HomeWarp: Warping to home location {home.Name}.",
                LogLevel.Info
            );

            // Use a safe tile on the home (fallback to (64,15))
            Vector2 warpTile = new Vector2(64, 15);

            // Warp the player
            Game1.warpFarmer(home.Name, (int)warpTile.X, (int)warpTile.Y, who.FacingDirection);

            // Restore player state
            Game1.fadeToBlackAlpha = 0.99f;
            Game1.screenGlow = false;
            who.temporarilyInvincible = false;
            who.temporaryInvincibilityTimer = 0;
            Game1.displayFarmer = true;
        }

        public override void Update(GameTime gameTime, Farmer who) { }

        public void Unsubscribe() { }
    }
}
