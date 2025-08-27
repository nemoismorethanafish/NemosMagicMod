using Microsoft.Xna.Framework;
using NemosMagicMod;
using SpaceCore;
using StardewValley;
using static SpellRegistry;

public abstract class Spell
{
    public const string SkillID = ModEntry.SkillID; // or just a fixed int for your skill

    // Other properties
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public int ManaCost { get; }

    public readonly int ExperienceGained;

    public bool IsActive { get; set; }

    public Spell(string id, string name, string description, int manaCost, int experienceGained = 25, bool isActive = false)
    {
        Id = id;
        Name = name;
        Description = description;
        ManaCost = manaCost;
        ExperienceGained = experienceGained;
        IsActive = isActive;
    }

    public virtual bool IsUnlocked => PlayerData.IsSpellUnlocked(this);

    public virtual void Cast(Farmer who)
    {
        // Disable any active spells first
        foreach (var spell in ModEntry.ActiveSpells)
        {
            spell.IsActive = false;

            // If the spell has a RenderedWorld hook or sprites, unsubscribe events
            if (spell is IRenderable r)
                r.Unsubscribe();
        }

        // Clear the list of active spells
        ModEntry.ActiveSpells.Clear();

        // Check mana
        if (!ManaManager.HasEnoughMana(ManaCost))
        {
            Game1.showRedMessage("Not enough mana!");
            return;
        }

        ManaManager.SpendMana(ManaCost);
        GrantExperience(who);

        // Add this spell to active spells
        IsActive = true;
        ModEntry.RegisterActiveSpell(this);
    }


    protected virtual void GrantExperience(Farmer who)
    {
        string readableName = Skills.GetSkill(ModEntry.SkillID)?.GetName() ?? "Skill";
        Skills.AddExperience(who, ModEntry.SkillID, ExperienceGained);
        //Game1.showGlobalMessage($"{who.Name} gained {ExperienceGained} {readableName} experience!");
    }

    public virtual void Update(GameTime gameTime, Farmer who) { }

    public interface IRenderable
    {
        void Unsubscribe();
    }
}
