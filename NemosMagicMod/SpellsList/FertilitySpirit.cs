using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using static SpellRegistry;

namespace NemosMagicMod.Spells
{
    public class FertilitySpirit : Spell, Spell.IRenderable
    {
        private const int GrowthRadius = 1; // radius in tiles around player
        private const int ParticleCount = 12;
        private const string LastCastKey = "NemosMagicMod.FertilitySpirit.LastCastDay";

        public FertilitySpirit()
            : base("nemo.FertilitySpirit", "Fertility Spirit", "Advances crops by one growth stage in a small area.", 15)
        { }

        protected override bool FreezePlayerDuringCast => true;

        public override void Cast(Farmer who)
        {
            // Generate a simple unique ID for today
            int todayId = Game1.year * 1000 + Game1.currentSeason.GetHashCode() * 100 + Game1.dayOfMonth;

            // Check if already cast today
            if (who.modData.TryGetValue(LastCastKey, out string lastDayStr) && int.TryParse(lastDayStr, out int lastDay))
            {
                if (lastDay == todayId)
                {
                    Game1.showRedMessage("Fertility Spirit can only be cast once per day!");
                    return; // exit early, do NOT spend mana
                }
            }

            // Spend mana & activate spell
            base.Cast(who);

            // Get player tile
            Vector2 playerTile = new Vector2((int)(who.Position.X / Game1.tileSize), (int)(who.Position.Y / Game1.tileSize));
            GameLocation location = who.currentLocation;

            // Apply growth in radius
            for (int x = -GrowthRadius; x <= GrowthRadius; x++)
            {
                for (int y = -GrowthRadius; y <= GrowthRadius; y++)
                {
                    Vector2 tile = playerTile + new Vector2(x, y);

                    if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) && feature is HoeDirt dirt && dirt.crop != null)
                    {
                        var crop = dirt.crop;
                        if (crop.currentPhase.Value < crop.phaseDays.Count - 1)
                        {
                            crop.currentPhase.Value++;

                            // Spawn magical sparkles
                            for (int i = 0; i < ParticleCount; i++)
                            {
                                location.temporarySprites.Add(new TemporaryAnimatedSprite(
                                    17,
                                    tile * Game1.tileSize + new Vector2(Game1.random.Next(-16, 16), Game1.random.Next(-16, 16)),
                                    Color.LimeGreen,
                                    8,
                                    false,
                                    50f
                                ));
                            }
                        }
                    }
                }
            }

            // Play magical growth sound
            Game1.playSound("yoba");

            // Record that spell was cast today
            who.modData[LastCastKey] = todayId.ToString();
        }

        public override void Update(GameTime gameTime, Farmer who) { }

        public void Unsubscribe() { }
    }
}
