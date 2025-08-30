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

    protected virtual bool FreezePlayerDuringCast => true; // default: freeze
    protected virtual bool UseBookAnimation => true; // NEW: toggle book animation

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int ManaCost { get; }

    public readonly int ExperienceGained;
    public bool IsActive { get; set; }

    // Spellbook animation
    private const float SpellbookDuration = 0.5f; // seconds
    private float spellbookTimer = 0f;
    private Texture2D spellbookTexture;
    private bool subscribedDraw = false;

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

    public virtual void Cast(Farmer who)
    {
        // Check mana
        if (!ManaManager.HasEnoughMana(ManaCost))
        {
            Game1.showRedMessage("Not enough mana!");
            return;
        }

        // Disable other active spells
        foreach (var spell in ModEntry.ActiveSpells)
        {
            spell.IsActive = false;
            if (spell is IRenderable r)
                r.Unsubscribe();
        }
        ModEntry.ActiveSpells.Clear();

        ManaManager.SpendMana(ManaCost);
        GrantExperience(who);

        // Activate spell
        IsActive = true;
        ModEntry.RegisterActiveSpell(this);

        // Choose animation type based on UseBookAnimation flag
        if (UseBookAnimation)
        {
            // Use the book reading animation instead of spellbook
            TriggerBookReadingAnimation(who);
        }
        else
        {
            // Use original spellbook animation
            StartSpellbookAnimation(who);
        }

        // Play casting sound
        Game1.playSound("wand");
    }

    /// <summary>
    /// NEW: Triggers the book reading animation using your CustomBook
    /// </summary>
    private void TriggerBookReadingAnimation(Farmer who)
    {
        if (FreezePlayerDuringCast)
            who.canMove = false;

        // Create and trigger the custom book animation
        var customBook = new CustomBook();

        // Backup experience before animation
        int[] expBackup = new int[who.experiencePoints.Count];
        who.experiencePoints.CopyTo(expBackup, 0);

        // Use reflection to call the readBook method
        var method = typeof(StardewValley.Object).GetMethod(
            "readBook",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
        );

        if (method != null)
        {
            method.Invoke(customBook, new object[] { who.currentLocation });

            // Restore experience after animation (since we already granted it above)
            for (int i = 0; i < expBackup.Length; i++)
            {
                who.experiencePoints[i] = expBackup[i];
            }
        }

        // Restore movement after a delay (book animation duration)
        if (FreezePlayerDuringCast)
        {
            DelayedAction.functionAfterDelay(() => who.canMove = true, 1000); // 1 second delay
        }
    }

    /// <summary>
    /// Original spellbook animation (renamed for clarity)
    /// </summary>
    private void StartSpellbookAnimation(Farmer who)
    {
        // Start spellbook animation
        spellbookTimer = SpellbookDuration;

        if (FreezePlayerDuringCast)
            who.canMove = false;

        // **Force Farmer to reading frame**
        TriggerReadingFrame(who);

        // Subscribe to rendering & update events
        SubscribeDraw();
    }

    /// <summary>Force the farmer sprite to frame 57 temporarily.</summary>
    protected void TriggerReadingFrame(Farmer who)
    {
        if (who?.FarmerSprite != null)
        {
            // Set the current frame to 57
            who.FarmerSprite.CurrentFrame = 57;

            // Optional: prevent automatic animation updates during spellbook
            who.FarmerSprite.PauseForSingleAnimation = true;
        }
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
            spellbookTimer -= 1f / 60f; // assuming 60 FPS
        }
        else
        {
            EndSpellbookAnimation();
        }
    }

    private void EndSpellbookAnimation()
    {
        ModEntry.Instance.Helper.Events.Display.RenderedWorld -= OnRenderedWorld;
        ModEntry.Instance.Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        subscribedDraw = false;

        // Restore movement
        if (FreezePlayerDuringCast)
            Game1.player.canMove = true;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (spellbookTimer <= 0f) return;

        Farmer who = Game1.player;

        // Base position above farmer
        Vector2 position = who.Position + new Vector2(0, -who.Sprite.SpriteHeight / 2);

        // Adjust position slightly based on facing direction
        switch (who.FacingDirection)
        {
            case 0: position += new Vector2(0, -10); break; // up
            case 1: position += new Vector2(10, -5); break; // right
            case 2: position += new Vector2(0, 0); break;   // down
            case 3: position += new Vector2(-10, -5); break;// left
        }

        // Draw the spellbook (slightly larger)
        e.SpriteBatch.Draw(
            spellbookTexture,
            Game1.GlobalToLocal(Game1.viewport, position),
            null,
            Color.White,
            0f,
            new Vector2(spellbookTexture.Width / 2, spellbookTexture.Height / 2),
            3f, // increased scale
            SpriteEffects.None,
            1f
        );
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