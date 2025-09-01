using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using NemosMagicMod.Spells;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Monsters;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

public class FireCyclone : Spell, Spell.IRenderable
{
    private const string BuffId = "NemosMagicMod_FireCyclone";
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
    private Texture2D buffIconTexture;

    protected override SpellbookTier MinimumTier => SpellbookTier.Adept;

    public FireCyclone()
        : base(
            id: "nemo.FireCyclone",
            name: "Fire Cyclone",
            description: "Create a swirling fire cyclone around you, pushing enemies away.",
            manaCost: 100,
            experienceGained: 50,
            false,
            iconPath: "assets/MagmaSparker.png"
        )
    {
        magmaTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/MagmaSparker.png");
        buffIconTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/FireCycloneBuffIcon.png");

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

        // Apply Buff
        ApplyFireCycloneBuff(owner);

        // Create the cyclone sprites
        CreateCycloneSprites();

        Game1.playSound("fireball");
    }

    private void ApplyFireCycloneBuff(Farmer who)
    {
        // Remove existing buff if present
        if (who.buffs.IsApplied(BuffId))
            who.buffs.Remove(BuffId);

        var buff = new Buff(
            id: BuffId,
            displayName: "Fire Cyclone",
            iconTexture: buffIconTexture,
            iconSheetIndex: 0,
            duration: DurationMs,
            effects: new BuffEffects(), // purely cosmetic
            description: "A swirling cyclone of fire surrounds you!"
        );

        who.buffs.Apply(buff);
    }

    private void RemoveFireCycloneBuff(Farmer who)
    {
        if (who?.buffs?.IsApplied(BuffId) == true)
        {
            who.buffs.Remove(BuffId);
        }
    }

    private void CreateCycloneSprites()
    {
        if (owner?.currentLocation == null) return;

        int spriteCount = 16;
        for (int i = 0; i < spriteCount; i++)
        {
            Color flameColor = new Color(
                255,
                random.Next(140, 255),
                random.Next(0, 100)
            );

            var sprite = new TemporaryAnimatedSprite(
                "LooseSprites\\Cursors",
                new Rectangle(211, 428, 7, 6),
                120f,
                1,
                9999,
                owner.Position,
                false,
                false
            )
            {
                scale = 3f,
                color = flameColor,
                layerDepth = 1f,
                rotationChange = 0.03f,
                alphaFade = 0f
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

        rotation += 0.03f;

        for (int i = 0; i < sprites.Count; i++)
        {
            float angle = rotation + MathHelper.TwoPi * i / sprites.Count;
            Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 192f;

            sprites[i].position = owner.Position + offset + new Vector2(0f, -24f);
        }

        if (owner.currentLocation != currentLocation)
        {
            OnLocationChanged();
        }

        DamageMonstersInArea();
    }

    private void OnLocationChanged()
    {
        currentLocation = owner.currentLocation;
        foreach (var sprite in sprites)
            currentLocation.temporarySprites.Add(sprite);
    }

    private void DamageMonstersInArea()
    {
        if (owner?.currentLocation == null) return;

        var weapon = new MeleeWeapon("17")
        {
            minDamage = { Value = 15 },
            maxDamage = { Value = 25 },
            knockback = { Value = 0.3f },
            critChance = { Value = 0.05f },
            critMultiplier = { Value = 2f }
        };

        foreach (var monster in currentLocation.characters.OfType<Monster>())
        {
            float distance = Vector2.Distance(monster.Position, owner.Position);
            if (distance <= Radius)
            {
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
                    false,
                    weapon.knockback.Value,
                    weapon.addedPrecision.Value,
                    weapon.critChance.Value,
                    weapon.critMultiplier.Value,
                    weapon.type.Value != 1,
                    owner
                );

                Vector2 knockbackDirection = Vector2.Normalize(monster.Position - owner.Position) * PushStrength;
                monster.setTrajectory((int)knockbackDirection.X, (int)knockbackDirection.Y);
            }
        }
    }

    private void EndCyclone()
    {
        IsActive = false;
        timer = 0f;

        foreach (var sprite in sprites)
            sprite.alphaFade = 0.05f;

        sprites.Clear();

        RemoveFireCycloneBuff(owner);

        Game1.addHUDMessage(new HUDMessage("Fire Cyclone dissipated.", 1));
    }

    public void Unsubscribe()
    {
        if (IsActive)
            EndCyclone();
    }
}
