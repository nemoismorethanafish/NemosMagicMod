using StardewValley;
using System;

public enum MagicProfession
{
    None = 0,
    WindSpiritCantripProfession = 1,
    FireballCantripProfession = 2,
}

public static class ProfessionManager
{
    private const string ProfessionKey = "NemosMagicMod.Profession";

    public static void SetProfession(Farmer player, MagicProfession profession)
    {
        player.modData[ProfessionKey] = ((int)profession).ToString();
    }

    public static MagicProfession GetProfession(Farmer player)
    {
        if (player.modData.TryGetValue(ProfessionKey, out string? val) &&
            int.TryParse(val, out int profInt) &&
            Enum.IsDefined(typeof(MagicProfession), profInt))
        {
            return (MagicProfession)profInt;
        }

        return MagicProfession.None;
    }

    public static bool HasProfession(Farmer player, MagicProfession profession)
    {
        return GetProfession(player) == profession;
    }
}
