using StardewModdingAPI;
using Microsoft.Xna.Framework.Graphics;

public class ModAssets
{
    private readonly IModHelper _helper;

    // Constructor that takes an IModHelper and initializes asset loading
    public ModAssets(IModHelper helper)
    {
        _helper = helper;
    }

    // Method to load an asset by file name (e.g., magic-icon-smol.png)
    public T Load<T>(string fileName) where T : class
    {
        return _helper.ModContent.Load<T>("assets/" + fileName);  // Assuming assets are in an "assets" folder
    }
}
