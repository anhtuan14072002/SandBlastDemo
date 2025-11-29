using System;
using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.Serialization;

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
        
        public void OnDeserialized()
        {
            List<ISerializationCallbackReceiver> receivers = new();
            receivers.Add(HighScore);
            receivers.Add(Gems);
            receivers.Add(MagicBrush);
            receivers.Add(Boom);
            receivers.Add(LevelModClassic);
            
            foreach (var serializationCallbackReceiver in receivers)
            {
                serializationCallbackReceiver.OnAfterDeserialize();
            }
        }
    }
}