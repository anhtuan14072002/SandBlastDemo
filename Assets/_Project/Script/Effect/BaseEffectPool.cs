using System;
using System.Collections.Generic;
using Core;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Sand
{
    public abstract class BaseEffectPool<T> : GameElement, IReceive<T>
    {
        [SerializeField] protected GameObject _effect;
        [SerializeField] protected int _poolSize = 10;
        [SerializeField] protected float _effectDuration = 1.5f;

        protected Queue<GameObject> _pool = new();

        private void Start()
        {
            InitPool();
        }

        protected virtual void InitPool()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var obj = Instantiate(_effect, transform.position, Quaternion.identity);
                obj.transform.SetParent(transform);
                obj.transform.localScale = Vector3.one;
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        protected virtual void GetEffect(string text = "", Vector3 position = default, int score = 0)
        {
            if (_pool.Count > 0)
            {
                var effect = _pool.Dequeue();
                effect.transform.localScale = Vector3.one;

                if (position != default(Vector3))
                    effect.transform.position = position;
                else
                    effect.transform.position = transform.position;

                effect.SetActive(true);

                if (!string.IsNullOrEmpty(text))
                {
                    var textComp = effect.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComp == null)
                        textComp = effect.GetComponentInChildren<TextMeshProUGUI>();
                    if (textComp != null)
                    {
                        textComp.text = text;
                        if (score > 100) textComp.color = Color.red;
                        else textComp.color = Color.white; 
                    }
                    else
                    {
                        Debug.LogError("No TextMeshPro component found!");
                    }
                }
                ReturnToPool(effect).Forget();
            }
        }

        protected abstract void ConfigureEffect(GameObject effect, string text = "");

        protected virtual async UniTask ReturnToPool(GameObject effect)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_effectDuration));
            effect.SetActive(false);
            _pool.Enqueue(effect);
        }

        public abstract void Receive(in T signal);
    }
}