using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sand
{
    [Serializable]
    public class UserData
    {
        [SerializeField] 
        public SerializableReactiveProperty<int> HighScore = new(0);
        [SerializeField] 
        public SerializableReactiveProperty<int> Gems = new(0);
        [SerializeField]
        public SerializableReactiveProperty<int> MagicBrush = new(0);
        [SerializeField]
        public SerializableReactiveProperty<int> Boom = new(0);
        [SerializeField]
        public SerializableReactiveProperty<int> Map = new(0);
        [SerializeField]
        public SerializableReactiveProperty<int> LevelModClassic = new(0);
        [SerializeField]
        public SerializableReactiveProperty<int> CurrentScore = new(0);
        [SerializeField]
        public SerializableReactiveProperty<int> CountDrawInGame = new(0);
        [SerializeField]
        public SerializableReactiveProperty<int> CountDrawPicture = new(0);
        [SerializeField]
        public List<int> CompletedPictureIndices = new();
        [SerializeField]
        public Dictionary<int, int> PictureFillProgress = new();
        [SerializeField]
        public Dictionary<int, bool> BoxSpawnIsLockState = new();
        [SerializeField] 
        public List<int> SoldPictureIndices = new();
        [SerializeField]
        public Dictionary<int, int> PictureFilledRegions = new();
        public void OnDeserialized()
        {
            List<ISerializationCallbackReceiver> receivers = new();
            receivers.Add(HighScore);
            receivers.Add(Gems);
            receivers.Add(MagicBrush);
            receivers.Add(Boom);
            receivers.Add(LevelModClassic);
            receivers.Add(CurrentScore);
            receivers.Add(CountDrawInGame);
            receivers.Add(CountDrawPicture);

            foreach (var serializationCallbackReceiver in receivers)
            {
                serializationCallbackReceiver.OnAfterDeserialize();
            }

            if (PictureFillProgress == null)
                PictureFillProgress = new Dictionary<int, int>();
            if (CompletedPictureIndices == null)
                CompletedPictureIndices = new List<int>();
            if (SoldPictureIndices == null)
                SoldPictureIndices = new List<int>();
            if (PictureFilledRegions == null)
                PictureFilledRegions = new Dictionary<int, int>();
        }
        
        public int CurrentScoreValue => CurrentScore.Value;
        public int HighScoreValue => HighScore.Value;
        public int GemsValue => Gems.Value;
        public int MagicBrushValue => MagicBrush.Value;
        public int BoomValue => Boom.Value;
        public int MapValue => Map.Value;
        public int LevelModClassicValue => LevelModClassic.Value;
    }
}
