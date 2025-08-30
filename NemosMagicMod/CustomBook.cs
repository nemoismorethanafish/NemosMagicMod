using Microsoft.Xna.Framework;
using StardewValley;
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

        // Backup experience
        int[] expBackup = new int[player.experiencePoints.Count];
        player.experiencePoints.CopyTo(expBackup, 0);

        // Call internal readBook method via reflection
        var method = typeof(StardewValley.Object).GetMethod(
            "readBook",
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (method != null)
            method.Invoke(this, new object[] { location });

        // Restore experience so no XP is gained
        for (int i = 0; i < expBackup.Length; i++)
            player.experiencePoints[i] = expBackup[i];
    }
}
