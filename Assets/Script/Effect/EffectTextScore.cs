using Core;
using UnityEngine;

namespace Sand
{
    public class EffectTextScore : BaseEffectPool<SignalOpenEffectTextScore>
    {
        public override void Receive(in SignalOpenEffectTextScore signal)
        {
            string displayText;

            if (signal.Combo > 1)
            {
                string comboColorHex;
                if (signal.Combo >= 2 && signal.Combo <= 5)
                {
                    comboColorHex = "#FFD700";
                }
                else 
                {
                    comboColorHex = "#FF0000";
                }
                var comboPart = $"<size=150%><color={comboColorHex}>Combo {signal.Combo}</color></size>";
                string scorePart = $" <color=#FFFFFF>+{signal.Score}</color>";

                displayText = comboPart + scorePart;
            }
            else
            {
                displayText = $"+{signal.Score}";
            }

            GetEffect(displayText, signal.Position, signal.Score);
            Global.Send(new SignalScoreOnGame() { Score = signal.Score });
        }

        protected override void ConfigureEffect(GameObject effect, string text = "")
        {
        }
    }
}
