using System.Collections.Generic;
using Sand;
using UnityEngine;

[CreateAssetMenu(fileName = "SoundConfig", menuName = "Scriptable Objects/SoundConfig")]
public class SoundConfig : ScriptableObject
{
    public List<Sound> soundList;
}
