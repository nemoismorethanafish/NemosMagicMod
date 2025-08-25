using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewModdingAPI;
using StardewValley.Buffs;

namespace NemosMagicMod.Spells
{
    public class WindSpirit : Spell
    {
        private const string BuffId = "NemosMagicMod_WindSpirit";
        private const float SpeedIncrease = 1; //This will be divided by four later
        private const int DurationMs = 60_000; // 60 seconds

        public WindSpirit() : base("Wind Spirit", 50, "Temporarily increases your movement speed.", 10)
        {
        }

        public override void Cast(Farmer who)
        {
            base.Cast(who);

            // Remove existing buff with the same ID to replace it
            if (who.buffs.IsApplied(BuffId))
                who.buffs.Remove(BuffId);

            Texture2D icon = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/magic-icon-smol.png");

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
            //Game1.showGlobalMessage("You feel lighter on your feet!");
            ModEntry.Instance.Monitor.Log($"Wind Spirit buff applied (+{SpeedIncrease} speed, {DurationMs / 1000}s)", LogLevel.Info);
        }

        public override bool IsExpired()
        {
            return !Game1.player.buffs.IsApplied(BuffId);
        }
    }
}
