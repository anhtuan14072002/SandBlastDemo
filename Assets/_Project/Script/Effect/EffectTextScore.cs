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
                // Setup cho combo display
                SetupComboDisplay(damageNumber, score, combo);
            }
            else
            {
                // Setup cho điểm bình thường
                SetupNormalDisplay(damageNumber, score);
            }
        }

        private void SetupComboDisplay(DamageNumber damageNumber, int score, int combo)
        {
            // Tắt number mặc định
            damageNumber.enableNumber = false;

            // Combo text ở trên với voffset để giảm khoảng cách
            damageNumber.enableTopText = true;
            string comboColorHex = combo >= 6 ? "#FF0000" : "#FFD700";
            damageNumber.topText =
                $"<b><color={comboColorHex}>Combo {combo}</color></size></b>";

            // Điểm ở dưới với voffset để giảm khoảng cách
            damageNumber.enableBottomText = true;
            damageNumber.bottomText = $"<b><color=#FF0000>+{score}</color></b>";

            // Có thể tăng scale cho combo
            damageNumber.SetScale(1.1f);
        }

        private void SetupNormalDisplay(DamageNumber damageNumber, int score)
        {
            // Hiển thị điểm bình thường
            damageNumber.number = score;

            // Tùy chỉnh màu sắc
            Color textColor = score > 100 ? Color.red : Color.white;
            damageNumber.SetColor(textColor);
        }
    }
}