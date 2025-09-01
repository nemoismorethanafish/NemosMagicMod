using System.Collections.Generic;

public class PlayerSaveData
{
    public HashSet<string> UnlockedSpellIds { get; set; } = new();

    // Persistent hotkey
    public string? HotkeyedSpellId { get; set; } = null;
}
