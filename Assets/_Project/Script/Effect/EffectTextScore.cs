using Core;
using DamageNumbersPro;
using UnityEngine;

namespace Sand
{
    public class EffectTextScore : GameElement, IReceive<SignalOpenEffectTextScore>
    {
        public DamageNumber numberPrefab;

        public void Receive(in SignalOpenEffectTextScore signal)
        {
            SpawnDamageNumber(signal.Position, signal.Score, signal.Combo);
            Global.Send(new SignalScoreOnGame() { Score = signal.Score });
        }

        private void SpawnDamageNumber(Vector3 position, int score, int combo)
        {
            if (numberPrefab == null) return;

            DamageNumber damageNumber = numberPrefab.Spawn(position);

            if (combo > 1)
            {
                SetupComboDisplay(damageNumber, score, combo);
            }
            else
            {
                SetupNormalDisplay(damageNumber, score);
            }
        }

        private void SetupComboDisplay(DamageNumber damageNumber, int score, int combo)
        {
            damageNumber.enableNumber = false;
            damageNumber.enableTopText = true;
            string comboColorHex = combo >= 6 ? "#FF0000" : "#FFD700";
            damageNumber.topText =
                $"<b><color={comboColorHex}>Combo {combo}</color></size></b>";
            damageNumber.enableBottomText = true;
            damageNumber.bottomText = $"<b><color=#FF0000>+{score}</color></b>";
            damageNumber.SetScale(1.1f);
        }

        private void SetupNormalDisplay(DamageNumber damageNumber, int score)
        {
            damageNumber.number = score;
            Color textColor = score > 100 ? Color.red : Color.white;
            damageNumber.SetColor(textColor);
        }
    }
}