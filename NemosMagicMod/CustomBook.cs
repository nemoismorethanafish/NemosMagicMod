using StardewValley;
using Microsoft.Xna.Framework;
using System.Reflection;

public class CustomBook : StardewValley.Object
{
    public CustomBook() : base("102", 1)
    {
        // Set up the book properties
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

        // Backup experience
        int[] expBackup = new int[player.experiencePoints.Count];
        player.experiencePoints.CopyTo(expBackup, 0);

        // Use reflection to call the internal readBook method that shows the animation
        var method = typeof(StardewValley.Object).GetMethod(
            "readBook",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (method != null)
        {
            method.Invoke(this, new object[] { location });

            // Restore experience after animation
            for (int i = 0; i < expBackup.Length; i++)
            {
                player.experiencePoints[i] = expBackup[i];
            }
        }
        else
        {
            // Fallback: just play sound if reflection fails
            Game1.playSound("dwop");
        }
    }
}