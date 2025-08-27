using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using NemosMagicMod;

namespace NemosMagicMod.Spells
{
    public class WaterSpirit : Spell
    {
        private const int DurationSeconds = 5;
        private const float WaterRadius = 1f;

        private float timer = 0f;
        private float elapsedTime = 0f;
        private bool active = false;
        private Farmer? caster;

        public WaterSpirit()
            : base(
                id: "nemo.WaterSpirit",                      // Unique ID
                name: "Water Spirit",
                description: "Helps water your crops!",
                manaCost: 75,
                experienceGained: 40                        // Optional override
            )
        {
        }

        public override bool IsUnlocked => ModEntry.MagicLevel >= 2;

        public override void Cast(Farmer who)
        {
            base.Cast(who); // handle mana check and XP

            if (!ManaManager.HasEnoughMana(ManaCost)) // double-check in case base didn't block cast
                return;

            caster = who;
            active = true;
            elapsedTime = 0f;
            timer = 0f;

            Game1.showGlobalMessage("A Water Spirit has been summoned to water your crops!");
            Game1.playSound("wateringCan");
        }

        public override void Update(GameTime gameTime, Farmer who)
        {
            if (!active)
                return;

            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds;
            elapsedTime += deltaSeconds;
            timer += deltaSeconds;

            if (timer >= 1f)
            {
                timer = 0f;

                Vector2 tilePosition = new(
                    (int)(who.Position.X / Game1.tileSize),
                    (int)(who.Position.Y / Game1.tileSize)
                );

                WaterNearbyCrops(who.currentLocation, tilePosition);
            }

            if (elapsedTime >= DurationSeconds)
            {
                active = false;
                Game1.showGlobalMessage("The Water Spirit has vanished.");
                Game1.playSound("waterSlosh");
            }
        }

        private void WaterNearbyCrops(GameLocation location, Vector2 centerTile)
        {
            int radius = (int)WaterRadius;

            for (int x = (int)centerTile.X - radius; x <= centerTile.X + radius; x++)
            {
                for (int y = (int)centerTile.Y - radius; y <= centerTile.Y + radius; y++)
                {
                    Vector2 tile = new(x, y);

                    if (Vector2.Distance(tile, centerTile) <= WaterRadius)
                    {
                        if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrainFeature)
                            && terrainFeature is HoeDirt dirt
                            && dirt.state.Value != HoeDirt.watered)
                        {
                            dirt.state.Value = HoeDirt.watered;
                        }
                    }
                }
            }
        }
    }
}
