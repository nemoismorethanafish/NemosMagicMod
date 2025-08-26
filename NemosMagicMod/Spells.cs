using Microsoft.Xna.Framework;
using StardewValley;
using SpaceCore;
using System.Linq; // Needed for .Last()

namespace NemosMagicMod.Spells
{
    public abstract class Spell
    {
        public string Name { get; }
        public string Description { get; }
        public int ManaCost { get; }
        public int ExperienceGained { get; }
        public string SkillId { get; }  // <-- Make sure this exists

        public Spell(string name, string description, int manaCost, int experienceGained, string skillId = "YourMod.Magic")
        {
            Name = name;
            Description = description;
            ManaCost = manaCost;
            ExperienceGained = experienceGained;
            SkillId = skillId;
        }

        public virtual void Cast(Farmer who)
        {
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            ManaManager.SpendMana(ManaCost);
            GrantExperience(who);
        }

        protected virtual void GrantExperience(Farmer who)
        {
            Skills.AddExperience(who, SkillId, ExperienceGained);
            string readableName = SkillId.Split('.').Last(); // e.g. "Magic"
            Game1.showGlobalMessage($"{who.Name} gained {ExperienceGained} {readableName} experience!");
        }

        public virtual void Update(GameTime gameTime, Farmer who) { }
        public virtual bool IsExpired() => false;
    }
}
