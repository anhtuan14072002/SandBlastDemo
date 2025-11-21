/*using System;
using Core;
using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;
using Tween = PrimeTween.Tween;

namespace WZ
{
    public class MainMenuPopup : Visual, IReceive<SignalOpenCategoryByIndex>
    {
        [SerializeField] private float _targetPosYFocusOpen;
        [SerializeField] private float _targetPosYFocusClose;

        [SerializeField] private float _targetPosYFocusIconOpen;
        [SerializeField] private float _targetPosYFocusIconClose;
        
        [SerializeField] private float _durationTweenOpen;
        [SerializeField] private float _durationTweenObjComp;
        [SerializeField] private float _durationDelayOneTweenObjComp;
        [SerializeField] private float _durationDelayAllTweenObjComp;
        
        private int _currentCategoryIndex = -1;
        
        [Serializable]
        private class CategoryMain
        {
            [SerializeField] public Button _buttonCategory;
            [SerializeField] public PopupType _thisPopupType;

            [SerializeField] public GameObject _prefabCategory;
            [SerializeField] public GameObject _focus;
            [SerializeField] public Image _imageFocus;
            [SerializeField] public GameObject _focusIcon;
            [SerializeField] public GameObject[] _objComponent;

            [SerializeField] public Vector3 _posObj;
            [SerializeField] public float _targetPosObj;
            [NonSerialized] public Vector3[] _originalPositions;

            [SerializeField] public int _indexCat;

            public LayoutElement _thisLayoutElement;
        }

        [SerializeField] private CategoryMain[] _categories;

        private void Start()
        {
            for (int i = 0; i < _categories.Length; i++)
            {
                var index = i;
                var cat = _categories[i];
                cat._buttonCategory.onClick.AddListener(() =>
                {
                    if (_currentCategoryIndex == index) return;
                    _currentCategoryIndex = index;
                    
                    ResetAllButtons();
                    FocusButton(_categories[index]._thisLayoutElement);

                    cat._prefabCategory.SetActive(true);

                    TweenOpenCategory(index);
                    SwitchTweenCat(index);
                });
                cat._originalPositions = new Vector3[cat._objComponent.Length];
                for (int j = 0; j < cat._objComponent.Length; j++)
                {
                    cat._originalPositions[j] = cat._objComponent[j].transform.position;
                }
            }
        }

        public void Receive(in SignalOpenCategoryByIndex signal)
        {
            OpenCategoryByIndex(0);
        }
        
        private void ResetAllButtons()
        {
            foreach (var category in _categories)
            {
                category._thisLayoutElement.flexibleWidth = 1.0f;
                category._prefabCategory.SetActive(false);
                
                Tween.Alpha(category._imageFocus, 1, 0.25f, Ease.Linear);
                Tween.PositionY(category._focus.transform, _targetPosYFocusClose, 0.1f, Ease.Linear);
                Tween.PositionY(category._focusIcon.transform, _targetPosYFocusIconClose, 0.2f, Ease.Linear);
                category._focusIcon.transform.localScale = Vector3.one;
                
            }
        }

        private void FocusButton(LayoutElement layoutElement)
        {
            layoutElement.flexibleWidth = 1.5f;
        }

        private void TweenOpenCategory(int index)
        {
            Tween.Alpha(_categories[index]._imageFocus, 1, 0.5f, Ease.Linear);
            
            Tween.PositionY(_categories[index]._focus.transform, _targetPosYFocusOpen, _durationTweenOpen, Ease.Linear);
            
            _categories[index]._focus.transform.localScale = Vector3.one;

            Tween.PositionY(_categories[index]._focusIcon.transform, _targetPosYFocusIconOpen, _durationTweenOpen,
                    Ease.Linear)
                .OnComplete(() =>
                    Tween.Scale(_categories[index]._focusIcon.transform, 1.5f, _durationTweenOpen, Ease.OutBack)
                );
        }

        private void SwitchTweenCat(int index)
        {
            ResetObjComponentsExcept(index);
            
            var category = _categories[index];

            switch (category._indexCat)
            {
                case 1:
                    TweenObjXComponent(index);
                    break;
                case 2:
                    TweenObjYComponent(index);
                    break;
            }
        }

        private async void TweenObjXComponent(int index)
        {
            var category = _categories[index];

            foreach (var obj in category._objComponent)
            {
                var pos = obj.transform.position;
                obj.transform.position = new Vector3(category._posObj.x, pos.y, pos.z);

                Tween.PositionX(obj.transform, category._targetPosObj, _durationTweenObjComp, Ease.Linear);
                await UniTask.Delay(TimeSpan.FromSeconds(_durationDelayAllTweenObjComp));
            }

            await UniTask.Delay(TimeSpan.FromSeconds(_durationDelayOneTweenObjComp));
        }

        private async void TweenObjYComponent(int index)
        {
            var category = _categories[index];

            foreach (var obj in category._objComponent)
            {
                var pos = obj.transform.position;
                obj.transform.position = new Vector3(pos.x, category._posObj.y, pos.z);

                Tween.PositionY(obj.transform, category._targetPosObj, 0.2f, Ease.Linear);
                await UniTask.Delay(TimeSpan.FromSeconds(0.05f));
            }

            await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
        }

        private void ResetObjComponentsExcept(int exceptIndex)
        {
            for (int i = 0; i < _categories.Length; i++)
            {
                if (i == exceptIndex) continue;
                var cat = _categories[i];

                for (int j = 0; j < cat._objComponent.Length; j++)
                {
                    cat._objComponent[j].transform.position = cat._originalPositions[j];
                }
            }
        }

        private void OpenCategoryByIndex(int index)
        {
            ResetAllButtons();
            _categories[index]._prefabCategory.SetActive(true);
            TweenOpenCategory(0);
            FocusButton(_categories[index]._thisLayoutElement);
        }
    }
}*/