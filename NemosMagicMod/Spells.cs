using Microsoft.Xna.Framework;
using StardewValley;

namespace NemosMagicMod.Spells
{
    public abstract class Spell
    {
        public string Name { get; }
        public int SkillLevelRequired { get; }
        public string Description { get; }
        public int ManaCost { get; }

        // Constructor
        public Spell(string name, int skillLevelRequired, string description, int manaCost)
        {
            Name = name;
            SkillLevelRequired = skillLevelRequired;
            Description = description;
            ManaCost = manaCost;
        }

        // Cast the spell (override this for actual functionality)
        public virtual void Cast(Farmer who)
        {
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            ManaManager.SpendMana(ManaCost);
        }

        // The Update method can be overridden for spells that need updates each frame
        public virtual void Update(GameTime gameTime, Farmer who)
        {
            // Default implementation: no behavior
        }

        // The IsExpired method (for spells with duration)
        public virtual bool IsExpired()
        {
            return false; // Default: not expired
        }
    }
}
