using System;
using UnityEngine;

namespace Sand
{
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundConfig _soundConfig;
        [SerializeField] private AudioSource _soundBlock;
        [SerializeField] private AudioSource _music;
        [SerializeField] private AudioSource _click;
        [SerializeField] private AudioSource _good;
        private bool _soundOn;

        private void Start()
        {
            _soundOn = PlayerPrefs.GetInt("SoundOn", 0) == 0;
        }

        public void OnPlaySound(SoundType soundType)
        {
            if (!_soundOn) return;
            Sound sound = _soundConfig.soundList.Find(x => x.Type == soundType);
            if (sound == null) return;
            _soundBlock.PlayOneShot(sound.Clip);
        }

        public void PlayClick()
        {
            _click.Play();
            Handheld.Vibrate();
        }

        public void PlayGood()
        {
            _good.Play();
        }
        public void MuteMusic()
        {
            if (_music != null) _music.mute = true;
        }

        public void UnmuteMusic()
        {
            if (_music != null) _music.mute = false;
        }

        public void MuteAllSounds()
        {
            if (_music != null) _music.mute = true;

            if (_soundBlock != null) _soundBlock.mute = true;

            if (_click != null) _click.mute = true;

            _soundOn = false;
            PlayerPrefs.SetInt("SoundOn", 1); 
            PlayerPrefs.Save();
        }

        public void UnmuteAllSounds()
        {
            if (_music != null) _music.mute = false;

            if (_soundBlock != null) _soundBlock.mute = false;

            if (_click != null) _click.mute = false;
            _soundOn = true;
            PlayerPrefs.SetInt("SoundOn", 0);
            PlayerPrefs.Save();
        }

        public void ToggleAllSounds()
        {
            if (_soundOn)
            {
                MuteAllSounds();
            }
            else
            {
                UnmuteAllSounds();
            }
        }
    }

    [Serializable]
    public class Sound
    {
        public string SoundName;
        public AudioClip Clip;
        public SoundType Type;
    }

    public enum SoundType
    {
        SandDrop,
        Combo1,
        Combo2,
        Combo3,
        Combo4,
        Combo5,
        Combo6,
        Combo7,
        Combo8,
        Combo9,
        GameOver,
        Boom,
        MusicMain,
        Click,
        LevelUp,
        Reward,
        Good,
        Great,
        Superb,
        WellDone,
        Wonderful,
        StartGame,
    }
}