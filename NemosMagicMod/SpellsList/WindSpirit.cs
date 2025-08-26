using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buffs;

namespace NemosMagicMod.Spells
{
    public class WindSpirit : Spell
    {
        private const string BuffId = "NemosMagicMod_WindSpirit";
        private const float SpeedIncrease = 1f;
        private const int DurationMs = 60_000;

        public WindSpirit()
            : base(
                name: "Wind Spirit",
                description: "Temporarily increases your movement speed.",
                manaCost: 10,
                experienceGained: 5,
                skillId: "nemosmagicmod.Magic") // <-- Use your actual registered ID here
        {
        }


        public override void Cast(Farmer who)
        {
            base.Cast(who);

            // If mana was insufficient, base.Cast() will return early
            if (!ManaManager.HasEnoughMana(ManaCost))
                return;

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
            ModEntry.Instance.Monitor.Log($"Wind Spirit buff applied (+{SpeedIncrease} speed, {DurationMs / 1000}s)", StardewModdingAPI.LogLevel.Info);
        }
    }
}
