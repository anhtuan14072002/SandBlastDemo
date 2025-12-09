using R3;
using TMPro;
using UnityEngine;
using Zenject;

namespace Sand
{
    public class CountDrawView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _countDrawInGame;
        [SerializeField] private TextMeshProUGUI _countDrawPicture;
        
        private UserData _userData;

        [Inject]
        void Construct(UserData userData)
        {
            _userData = userData;
        }

        private void Start()
        {
            if (_userData == null) return;
            if (_countDrawInGame != null) 
                _countDrawInGame.text = _userData.CountDrawInGame.Value.ToString();
            if (_countDrawPicture != null)
                _countDrawPicture.text = _userData.CountDrawPicture.Value.ToString();

            _userData.CountDrawInGame.Subscribe(v =>
                {
                    if (_countDrawInGame != null) _countDrawInGame.text = v.ToString();
                })
                .AddTo(this);

            _userData.CountDrawPicture.Subscribe(v =>
                {
                    if (_countDrawPicture != null) _countDrawPicture.text = v.ToString();
                })
                .AddTo(this);
        }
    }
}