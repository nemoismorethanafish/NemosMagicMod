using System.Collections.Generic;

namespace NemosMagicMod
{
    public class PlayerSaveData
    {
        public HashSet<string> UnlockedSpellIds { get; set; } = new();
    }
}
