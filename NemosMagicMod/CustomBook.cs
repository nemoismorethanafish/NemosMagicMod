using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Extensions;
using System.Reflection;

public class CustomBook : StardewValley.Object
{
    public CustomBook() : base("102", 1)
    {
        // Set up custom book properties
    }

    public override bool performUseAction(GameLocation location)
    {
        TriggerReadAnimation(location);
        return true;
    }

    private void TriggerReadAnimation(GameLocation location)
    {
        var player = Game1.player;
        if (player == null) return;

        // Spawn book-reading animation yourself
        for (int i = 0; i < 12; i++)
        {
            location.temporarySprites.Add(
                new TemporaryAnimatedSprite(
                    354,
                    Game1.random.Next(25, 75),
                    6,
                    1,
                    player.Position + new Vector2(Game1.random.Next(-64, 64), Game1.random.Next(-64, 64)),
                    flicker: false,
                    Game1.random.NextBool()
                )
            );
        }

        // No XP gain, no sound
    }
}