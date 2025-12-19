using System;
using UnityEngine;

namespace Sand
{
    public class BlockInfo : MonoBehaviour
    {
        [Header("Runtime")]
        public int PrefabIndex;

        [Header("Spawn Rules")]
        [Min(0)] public int MinLevelToAppear = 0;

        [Tooltip("-1 = xuất hiện mãi")]
        public int MaxLevelToAppear = -1;

        [Min(0f)] [Tooltip("0 = không bao giờ spawn. Càng lớn càng dễ ra.")]
        public float RateSpawn = 1f;

        [Header("Rate theo Level")]
        [Tooltip("Nếu để trống sẽ dùng RateSpawn mặc định cho mọi level")]
        public LevelRate[] RatesByLevel;

        public bool IsAvailableAtLevel(int level)
        {
            if (GetRateAtLevel(level) <= 0f) return false;
            if (level < MinLevelToAppear) return false;
            if (MaxLevelToAppear >= 0 && level > MaxLevelToAppear) return false;
            return true;
        }

        public float GetRateAtLevel(int level)
        {
            if (RatesByLevel == null || RatesByLevel.Length == 0)
                return RateSpawn;
            float rate = RateSpawn; 
            foreach (var levelRate in RatesByLevel)
            {
                if (level >= levelRate.FromLevel && (levelRate.ToLevel == -1 || level <= levelRate.ToLevel))
                {
                    rate = levelRate.Rate;
                    break;
                }
            }

            return rate;
        }

        [Serializable]
        public struct LevelRate
        {
            [Tooltip("Level bắt đầu")]
            public int FromLevel;
            [Tooltip("Level kết thúc (-1 = vô hạn)")]
            public int ToLevel;
            [Min(0f)]
            [Tooltip("Tỉ lệ spawn trong khoảng level này")]
            public float Rate;
        }
    }
}