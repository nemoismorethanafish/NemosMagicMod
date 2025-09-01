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
        private Texture2D windTexture;


        public WindSpirit()
            : base(
                "nemo.WindSpirit",
                "Wind Spirit",
                "Temporarily increases your movement speed.",
                50,
                10,
                false,
                "assets/windSpirit.png"
              )
        {
            windTexture = ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/windSpirit.png");
            iconTexture = windTexture;
        }


        public override void Cast(Farmer who)
        {
            if (!CanCast(who))
                return;

            base.Cast(who);

            // --- Determine tier-based values ---
            SpellbookTier tier = GetCurrentSpellbookTier(who);
            int durationMs = tier switch
            {
                SpellbookTier.Novice => 60_000,
                SpellbookTier.Apprentice => 90_000,
                SpellbookTier.Adept => 120_000,
                SpellbookTier.Master => 180_000,
                _ => 60_000
            };

            float speedBuff = tier switch
            {
                SpellbookTier.Novice => 0.25f,
                SpellbookTier.Apprentice => 0.5f,
                SpellbookTier.Adept => 0.75f,
                SpellbookTier.Master => 1.0f,
                _ => 0.25f
            };

            // --- Apply buff after 1 second delay ---
            DelayedAction.functionAfterDelay(() =>
            {
                // Remove existing Wind Spirit buff
                if (who.buffs.IsApplied(BuffId))
                    who.buffs.Remove(BuffId);

                Texture2D icon = NemosMagicMod.ModEntry.Instance.Helper.ModContent.Load<Texture2D>("assets/magic-icon-smol.png");

                var buff = new Buff(
                    id: BuffId,
                    displayName: "Wind Spirit",
                    iconTexture: icon,
                    iconSheetIndex: 0,
                    duration: durationMs,
                    effects: new BuffEffects { Speed = { Value = speedBuff } },
                    description: $"You feel lighter on your feet! (+{speedBuff} speed for {durationMs / 1000}s)"
                );

                who.buffs.Apply(buff);
                ModEntry.Instance.Monitor.Log($"Wind Spirit buff applied (+{speedBuff} speed, {durationMs / 1000}s)", LogLevel.Info);

            }, 1000);
        }
    }
}
