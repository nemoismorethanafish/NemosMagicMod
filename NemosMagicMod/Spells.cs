using MagicSkillDefinition;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Monsters; // if needed
using StardewValley.Objects; // if needed for sprites
using System.Collections.Generic;

public class Spell
{
    public string Name { get; }
    public int ManaCost { get; }
    public string Description { get; }
    public int ExperienceReward { get; }

    public Spell(string name, int manaCost, string description, int experienceReward = 10)
    {
        Name = name;
        ManaCost = manaCost;
        Description = description;
        ExperienceReward = experienceReward;
    }

    public virtual bool IsExpired()
    {
        return false;
    }

    public virtual void Update(GameTime gameTime, Farmer who)
    {
        // Default no-op
    }

    public virtual void Cast(Farmer caster)
    {
        // Default cast logic (messages, effects, etc.)

        // Grant XP to Magic skill if available
        if (caster != null && NemosMagicMod.ModEntry.MagicSkillInstance != null)
        {
            SpaceCore.Skills.AddExperience(caster, MagicSkill.MagicSkillId, ExperienceReward);
            NemosMagicMod.ModEntry.Instance.Monitor.Log(
                $"Granted {ExperienceReward} XP to Magic skill for casting {Name}.",
                StardewModdingAPI.LogLevel.Info);
        }
    }

    public virtual List<TemporaryAnimatedSprite> CastWithSprites(Farmer caster)
    {
        Cast(caster);
        return new List<TemporaryAnimatedSprite>();
    }
}
