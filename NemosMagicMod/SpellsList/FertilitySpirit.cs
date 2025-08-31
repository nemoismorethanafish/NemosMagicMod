using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using SpaceCore;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;
using static SpellRegistry;
using System.Linq;


namespace NemosMagicMod.Spells
{
    public class FertilitySpirit : Spell, Spell.IRenderable
    {
        private const int GrowthRadius = 1; // radius in tiles around player
        private const int ParticleCount = 12;
        private const string LastCastKey = "NemosMagicMod.FertilitySpirit.LastCastDay";
        protected override SpellbookTier MinimumTier => SpellbookTier.Adept;

        private int GetProfessionId(string skillId, string professionId)
        {
            return Skills.GetSkill(skillId)
                         .Professions
                         .Single(p => p.Id == professionId)
                         .GetVanillaId();
        }


        public FertilitySpirit()
            : base("nemo.FertilitySpirit", "Fertility Spirit", "Advances crops by one growth stage in a small area.", 15)
        { }

        protected override bool FreezePlayerDuringCast => true;

        public override void Cast(Farmer who)
        {
            // --- Minimum spellbook tier check ---
            if (!HasSufficientSpellbookTier(who))
            {
                string requiredTierName = MinimumTier.ToString();
                Game1.showRedMessage($"Requires {requiredTierName} spellbook or higher!");
                return;
            }

            // --- Generate unique day ID ---
            int todayId = Game1.year * 1000 + Game1.currentSeason.GetHashCode() * 100 + Game1.dayOfMonth;

            // --- Check normal cast ---
            bool normalUsedToday = who.modData.TryGetValue(LastCastKey, out string lastDayStr)
                                   && int.TryParse(lastDayStr, out int lastDay)
                                   && lastDay == todayId;

            // --- Check Bonus Daily cast ---
            int bonusDailyId = GetProfessionId(ModEntry.SkillID, "BonusDaily");
            bool hasBonusDaily = who.professions.Contains(bonusDailyId);
            bool bonusUsedToday = who.modData.TryGetValue($"{LastCastKey}.BonusDailyUsed", out string usedStr)
                                  && int.TryParse(usedStr, out int usedDay)
                                  && usedDay == todayId;

            // --- Block if both normal and bonus are spent ---
            if (normalUsedToday && (!hasBonusDaily || bonusUsedToday))
            {
                Game1.showRedMessage("Fertility Spirit can only be cast once per day!");
                return;
            }

            // --- Not Enough Mana check ---
            if (who.Stamina < this.ManaCost)
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            // --- Base cast (spends mana, triggers standard effects) ---
            base.Cast(who);

            // --- Record usage ---
            if (!normalUsedToday)
            {
                who.modData[LastCastKey] = todayId.ToString(); // spend normal cast
            }
            else if (hasBonusDaily && !bonusUsedToday)
            {
                who.modData[$"{LastCastKey}.BonusDailyUsed"] = todayId.ToString(); // spend bonus cast
            }

            // --- Delayed custom effects ---
            DelayedAction.functionAfterDelay(() =>
            {
                Vector2 playerTile = new Vector2((int)(who.Position.X / Game1.tileSize), (int)(who.Position.Y / Game1.tileSize));
                GameLocation location = who.currentLocation;

                for (int x = -GrowthRadius; x <= GrowthRadius; x++)
                {
                    for (int y = -GrowthRadius; y <= GrowthRadius; y++)
                    {
                        Vector2 tile = playerTile + new Vector2(x, y);

                        if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature feature)
                            && feature is HoeDirt dirt && dirt.crop != null)
                        {
                            var crop = dirt.crop;
                            if (crop.currentPhase.Value < crop.phaseDays.Count - 1)
                            {
                                crop.currentPhase.Value++;

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

                Game1.playSound("yoba");
            }, 1000); // 1-second delay
        }

        public override void Update(GameTime gameTime, Farmer who) { }

        public void Unsubscribe() { }
    }
}
