using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using NemosMagicMod.Spells;
using SpaceCore;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

public class FireCyclone : Spell, Spell.IRenderable
{
    private const float Radius = 256f;
    private const float PushStrength = 12f;
    private const int DurationMs = 30000;

    private float timer = 0f;
    private Farmer owner;
    private Random random = new Random();
    private List<TemporaryAnimatedSprite> sprites = new List<TemporaryAnimatedSprite>();
    private float rotation = 0f;
    private GameLocation currentLocation;
    private Texture2D magmaTexture;

    protected override SpellbookTier MinimumTier => SpellbookTier.Adept;

    public FireCyclone()
        : base(
            id: "nemo.FireCyclone",
            name: "Fire Cyclone",
            description: "Create a swirling fire cyclone around you, pushing enemies away.",
            manaCost: 100,
            experienceGained: 50,
            false,
            "assets/MagmaSparker.png"
        )
    {
        magmaTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/MagmaSparker.png");
        iconTexture = magmaTexture;
    }

    public override void Cast(Farmer who)
    {
        if (!CanCast(who))
            return;

        base.Cast(who);

        if (IsActive)
        {
            Game1.showRedMessage("Fire Cyclone is already active!");
            return;
        }

        owner = who;
        currentLocation = who.currentLocation;
        timer = DurationMs / 1000f;
        IsActive = true;
        rotation = 0f;

        // Clear any existing sprites
        sprites.Clear();

        // Create the cyclone sprites similar to ActiveCyclone
        CreateCycloneSprites();
    }

    private void CreateCycloneSprites()
    {
        if (owner?.currentLocation == null) return;

        // Create 16 flame sprites in a circle (similar to ActiveCyclone's 24)
        int spriteCount = 16;

        for (int i = 0; i < spriteCount; i++)
        {
            // Random fire colors (orange/red/yellow)
            Color flameColor = new Color(
                255,
                random.Next(140, 255), // Green component for orange/red
                random.Next(0, 100)    // Blue component for fire effect
            );

            // Use a known working sprite - hearts from cursors work reliably
            var sprite = new TemporaryAnimatedSprite(
                textureName: "LooseSprites\\Cursors",
                sourceRect: new Rectangle(211, 428, 7, 6), // Heart sprite
                animationInterval: 120f,
                animationLength: 1,
                numberOfLoops: 9999, // Loop indefinitely
                position: owner.Position, // Will be updated in Update()
                flicker: false,
                flipped: false
            )
            {
                scale = 3f,
                color = flameColor,
                layerDepth = 1f,
                rotationChange = 0.03f,
                alphaFade = 0f // Don't fade until we want to end
            };

            currentLocation.temporarySprites.Add(sprite);
            sprites.Add(sprite);
        }
    }

    public override void Update(GameTime gameTime, Farmer who)
    {
        if (!IsActive) return;

        timer -= (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (timer <= 0f)
        {
            EndCyclone();
            return;
        }

        // Update rotation (similar to ActiveCyclone)
        rotation += 0.03f;

        // Update sprite positions in a circle around the player
        for (int i = 0; i < sprites.Count; i++)
        {
            float angle = rotation + (float)Math.PI * 2f * (float)i / (float)sprites.Count;
            Vector2 offset = new Vector2(
                (float)Math.Cos(angle),
                (float)Math.Sin(angle)
            ) * 192f; // Use same distance as ActiveCyclone (192f)

            // Use exact same positioning as ActiveCyclone
            sprites[i].position = owner.Position + offset + new Vector2(0f, -24f);
        }

        // Handle location changes
        if (owner.currentLocation != currentLocation)
        {
            OnLocationChanged();
        }

        // Damage and knockback monsters
        DamageMonstersInArea();

        // Optional: Break objects like the original ActiveCyclone
        // BreakObjectsInArea();
    }

    private void OnLocationChanged()
    {
        // Remove sprites from old location and add to new location
        currentLocation = owner.currentLocation;

        foreach (var sprite in sprites)
        {
            currentLocation.temporarySprites.Add(sprite);
        }
    }

    private void DamageMonstersInArea()
    {
        if (owner?.currentLocation == null) return;

        // Create a weapon for damage calculation (similar to ActiveCyclone)
        var weapon = new MeleeWeapon("17") // Steel Falchion ID
        {
            minDamage = { Value = 15 },
            maxDamage = { Value = 25 },
            knockback = { Value = 0.3f },
            critChance = { Value = 0.05f },
            critMultiplier = { Value = 2f }
        };

        var monsters = currentLocation.characters.OfType<Monster>().ToList();

        foreach (var monster in monsters)
        {
            float distance = Vector2.Distance(monster.Position, owner.Position);

            if (distance <= Radius)
            {
                // Damage the monster
                Rectangle damageArea = new Rectangle(
                    (int)(owner.Position.X - Radius),
                    (int)(owner.Position.Y - Radius),
                    (int)(Radius * 2f),
                    (int)(Radius * 2f)
                );

                currentLocation.damageMonster(
                    damageArea,
                    weapon.minDamage.Value,
                    weapon.maxDamage.Value,
                    false, // isBomb
                    weapon.knockback.Value,
                    weapon.addedPrecision.Value,
                    weapon.critChance.Value,
                    weapon.critMultiplier.Value,
                    weapon.type.Value != 1,
                    owner
                );

                // Apply knockback
                Vector2 knockbackDirection = Vector2.Normalize(monster.Position - owner.Position) * PushStrength;
                monster.setTrajectory((int)knockbackDirection.X, (int)knockbackDirection.Y);
            }
        }
    }

    //private void BreakObjectsInArea()
    //{
    //    if (owner?.currentLocation == null) return;

    //    // Create a pickaxe for breaking objects (like ActiveCyclone)
    //    var pickaxe = new Pickaxe { UpgradeLevel = 3 };

    //    var objectsToCheck = currentLocation.objects.Keys.ToList();

    //    foreach (var tilePos in objectsToCheck)
    //    {
    //        var obj = currentLocation.objects[tilePos];
    //        if (obj == null) continue;

    //        string objName = obj.Name?.ToLower() ?? "";

    //        // Check if it's a breakable object
    //        if (objName.Contains("stone") || objName.Contains("rock") ||
    //            objName.Contains("node") || objName.Contains("crate") ||
    //            objName.Contains("barrel") || objName.Contains("box") ||
    //            objName.Contains("weed") || objName.Contains("litter"))
    //        {
    //            Vector2 objectWorldPos = tilePos * 64f + new Vector2(32f, 32f);

    //            if (Vector2.Distance(owner.Position, objectWorldPos) < Radius)
    //            {
    //                float originalStamina = owner.stamina;
    //                pickaxe.DoFunction(currentLocation, (int)(tilePos.X * 64f), (int)(tilePos.Y * 64f), 0, owner);
    //                owner.stamina = originalStamina; // Restore stamina
    //            }
    //        }
    //    }
    //}

    private void EndCyclone()
    {
        IsActive = false;
        timer = 0f;

        // Fade out all sprites
        foreach (var sprite in sprites)
        {
            sprite.alphaFade = 0.05f;
        }

        sprites.Clear();
    }

    public void Unsubscribe()
    {
        if (IsActive)
        {
            EndCyclone();
        }
    }
}