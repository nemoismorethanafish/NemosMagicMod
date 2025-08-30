using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewModdingAPI;
using StardewValley.Buffs;
using NemosMagicMod;

namespace NemosMagicMod.Spells
{
    public class WindSpirit : Spell
    {
        private const string BuffId = "NemosMagicMod_WindSpirit";
        private const float SpeedIncrease = 1; //This will be divided by four later
        private const int DurationMs = 60_000; // 60 seconds

        public WindSpirit()
            : base(
                "nemo.WindSpirit",
                "Wind Spirit",
                "Temporarily increases your movement speed.",
                50,
                75
              )
        { }


        public override void Cast(Farmer who)
        {
            // --- Not Enough Mana check ---
            if (!ManaManager.HasEnoughMana(ManaCost))
            {
                Game1.showRedMessage("Not enough mana!");
                return;
            }

            // --- Spend mana / base cast immediately ---
            base.Cast(who);

            // --- Delay the actual buff application ---
            DelayedAction.functionAfterDelay(() =>
            {
                // Remove existing buff with the same ID to replace it
                if (who.buffs.IsApplied(BuffId))
                    who.buffs.Remove(BuffId);

                Texture2D icon = NemosMagicMod.ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/magic-icon-smol.png");

                var buff = new Buff(
                    id: BuffId,
                    displayName: "Wind Spirit",
                    iconTexture: icon,
                    iconSheetIndex: 0,
                    duration: DurationMs,
                    effects: new BuffEffects { Speed = { Value = SpeedIncrease / 4 } },
                    description: "You feel lighter on your feet!"
                );

                who.buffs.Apply(buff);
                ModEntry.Instance.Monitor.Log($"Wind Spirit buff applied (+{SpeedIncrease} speed, {DurationMs / 1000}s)", LogLevel.Info);

            }, 1000); // 1-second delay
        }
    }
}
