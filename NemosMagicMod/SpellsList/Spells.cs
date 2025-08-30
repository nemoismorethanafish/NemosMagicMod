using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NemosMagicMod;
using SpaceCore;
using StardewValley;
using StardewModdingAPI.Events;
using static SpellRegistry;

public abstract class Spell
{
    public const string SkillID = ModEntry.SkillID;

    protected virtual bool FreezePlayerDuringCast => true;
    protected virtual bool UseBookAnimation => true;

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int ManaCost { get; }

    public readonly int ExperienceGained;
    public bool IsActive { get; set; }

    private Texture2D spellbookTexture;
    private bool subscribedDraw = false;
    private float spellbookTimer = 0f;

    public Spell(string id, string name, string description, int manaCost, int experienceGained = 25, bool isActive = false)
    {
        Id = id;
        Name = name;
        Description = description;
        ManaCost = manaCost;
        ExperienceGained = experienceGained;
        IsActive = isActive;

        spellbookTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/spellbooktexture");
    }

    public virtual bool IsUnlocked => PlayerData.IsSpellUnlocked(this);

    /// <summary>Cast spell: animation happens first, then spell effects</summary>
    public virtual void Cast(Farmer who)
    {
        if (!ManaManager.HasEnoughMana(ManaCost))
        {
            Game1.showRedMessage("Not enough mana!");
            return;
        }

        if (UseBookAnimation)
        {
            TriggerBookReadingAnimation(who, () =>
            {
                ApplySpellEffects(who);
            });
        }
        else
        {
            ApplySpellEffects(who);
        }
    }

    private void ApplySpellEffects(Farmer who)
    {
        foreach (var spell in ModEntry.ActiveSpells)
        {
            spell.IsActive = false;
            if (spell is IRenderable r)
                r.Unsubscribe();
        }
        ModEntry.ActiveSpells.Clear();

        ManaManager.SpendMana(ManaCost);
        GrantExperience(who);

        IsActive = true;
        ModEntry.RegisterActiveSpell(this);
    }

    /// <summary>Trigger book reading animation and optional callback when done</summary>
    public virtual void TriggerBookReadingAnimation(Farmer who, System.Action onAnimationComplete)
    {
        if (FreezePlayerDuringCast)
            who.canMove = false;

        // Backup original frame
        int originalFrame = who.FarmerSprite.CurrentFrame;
        who.FarmerSprite.CurrentFrame = 57; // reading frame
        who.FarmerSprite.PauseForSingleAnimation = true;

        // Create custom book object
        var customBook = new CustomBook();

        // Backup experience so XP isn't granted yet
        int[] expBackup = new int[who.experiencePoints.Count];
        who.experiencePoints.CopyTo(expBackup, 0);

        var method = typeof(StardewValley.Object).GetMethod(
            "readBook",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        if (method != null)
            method.Invoke(customBook, new object[] { who.currentLocation });

        for (int i = 0; i < expBackup.Length; i++)
            who.experiencePoints[i] = expBackup[i];

        // Start spellbook timer and subscribe to draw
        spellbookTimer = 1f; // 1 second duration
        SubscribeDraw();

        // Delay for animation duration
        DelayedAction.functionAfterDelay(() =>
        {
            // Restore player
            who.canMove = true;
            who.FarmerSprite.CurrentFrame = originalFrame;
            who.FarmerSprite.PauseForSingleAnimation = false;

            // End drawing
            EndSpellbookAnimation();

            // Continue with spell effects
            onAnimationComplete?.Invoke();
        }, 1000); // 1 second delay
    }

    private void SubscribeDraw()
    {
        if (!subscribedDraw)
        {
            ModEntry.Instance.Helper.Events.Display.RenderedWorld += OnRenderedWorld;
            ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            subscribedDraw = true;
        }
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (spellbookTimer > 0f)
        {
            spellbookTimer -= 1f / 60f; // assume 60 FPS
        }
        else
        {
            EndSpellbookAnimation();
        }
    }

    private void EndSpellbookAnimation()
    {
        if (!subscribedDraw) return;

        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribedDraw = false;
        spellbookTimer = 0f;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (spellbookTimer <= 0f) return;

        var who = Game1.player;
        Vector2 position = who.Position + new Vector2(0, -who.Sprite.SpriteHeight / 2);

        switch (who.FacingDirection)
        {
            case 0: position += new Vector2(0, -10); break;
            case 1: position += new Vector2(10, -5); break;
            case 2: position += new Vector2(0, 0); break;
            case 3: position += new Vector2(-10, -5); break;
        }
    }

    protected virtual void GrantExperience(Farmer who)
    {
        string readableName = Skills.GetSkill(ModEntry.SkillID)?.GetName() ?? "Skill";
        Skills.AddExperience(who, ModEntry.SkillID, ExperienceGained);
    }

    public virtual void Update(GameTime gameTime, Farmer who) { }

    public interface IRenderable
    {
        void Unsubscribe();
    }
}
