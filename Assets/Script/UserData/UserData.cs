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

        public void OnDeserialized()
        {
            List<ISerializationCallbackReceiver> receivers = new();
            receivers.Add(HighScore);
            receivers.Add(Gems);
            receivers.Add(MagicBrush);
            receivers.Add(Boom);
            
            foreach (var serializationCallbackReceiver in receivers)
            {
                serializationCallbackReceiver.OnAfterDeserialize();
            }
        }
    }
}