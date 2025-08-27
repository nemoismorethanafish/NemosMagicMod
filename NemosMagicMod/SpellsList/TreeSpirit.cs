using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;

namespace NemosMagicMod
{
    public class TreeSpirit : Spell
    {
        private Texture2D axeTexture;
        private float activeTimer = 0f;
        private readonly float duration = 10f; // Seconds spell lasts
        private Vector2 spiritPosition;

        public bool IsActive => activeTimer > 0f;

        public TreeSpirit()
            : base(
                  id: "tree_spirit",
                  name: "Tree Spirit",
                  description: "Summons a spirit in the form of an axe. While active, trees drop slightly more wood.",
                  manaCost: 20,
                  experienceGained: 50
              )
        {
        }

        public override void Cast(Farmer who)
        {
            base.Cast(who);

            // Summon the spirit at the player's position
            spiritPosition = who.Position;

            // Load the steel axe texture
            axeTexture = Game1.content.Load<Texture2D>("Tools/axe");

            activeTimer = duration;

            Game1.playSound("leafrustle");
        }

        public override void Update(GameTime gameTime, Farmer who)
        {
            if (activeTimer > 0f)
            {
                activeTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsActive && axeTexture != null)
            {
                spriteBatch.Draw(
                    axeTexture,
                    spiritPosition,
                    null,
                    Color.White,
                    0f,
                    new Vector2(axeTexture.Width / 2, axeTexture.Height / 2),
                    1f,
                    SpriteEffects.None,
                    1f
                );
            }
        }

        /// <summary>
        /// Increases wood drops by 25% while active
        /// </summary>
        public int ModifyWoodDrop(Tree tree, int baseAmount)
        {
            if (IsActive)
            {
                int extra = (int)Math.Ceiling(baseAmount * 0.25);
                return baseAmount + extra;
            }
            return baseAmount;
        }
    }
}

[HarmonyPatch(typeof(Tree), nameof(Tree.performToolAction))]
public static class Tree_PerformToolAction_Patch
{
    static void Postfix(Tree __instance, Tool t, int damage, Vector2 tileLocation, GameLocation location)
    {
        // Only apply if TreeSpirit is active
        TreeSpirit treeSpirit = SpellRegistry.TreeSpirit;
        if (treeSpirit.IsActive && t != null && t.Category == -96) // category -96 = axe
        {
            int baseWood = 1; // Adjust based on tree type if you want
            int modifiedWood = treeSpirit.ModifyWoodDrop(__instance, baseWood);

            // Spawn the extra wood
            Game1.createObjectDebris("Wood", (int)tileLocation.X, (int)tileLocation.Y, modifiedWood);
        }
    }
}

