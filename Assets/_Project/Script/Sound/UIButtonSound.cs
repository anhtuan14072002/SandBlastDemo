using Sand;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class UIButtonSound : MonoBehaviour
{
    private Button _button;
    SoundManager _soundManager;
    VibrationManager _vibrationManager;

    [Inject]
    void Construct(SoundManager soundManager, VibrationManager vibrationManager)
    {
        _soundManager = soundManager;
        _vibrationManager = vibrationManager;
    }
    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        _soundManager.OnPlaySound(SoundType.Click);
        _vibrationManager.SelectionButton();
    }
}