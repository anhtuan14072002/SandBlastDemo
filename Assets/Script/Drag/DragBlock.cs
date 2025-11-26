using System;
using Core;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace Sand
{
    public class DragBlock : MonoBehaviour
    {
        [SerializeField] private BlockManager _blockManager;
        [SerializeField] private RenderMap _map;

        private GameObject _objDrag;
        private Vector3 _startPos;
        private Vector3 _offset;
        private bool _isDragging;
        IDisposable _dragSub;

        private void Start()
        {
            _dragSub = Observable.EveryUpdate()
                .Subscribe(_ => HandleDragInput());
        }

        private void HandleDragInput()
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                StartDrag();
            else if (Input.GetMouseButton(0) && _isDragging)
                Drag();
            else if (Input.GetMouseButtonUp(0) && _isDragging)
                EndDrag();
        }

        private void StartDrag()
        {
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero, Mathf.Infinity, 1 << 6);

            if (hit.collider == null) return;
            _objDrag = hit.collider.gameObject;
            _isDragging = true;
            _startPos = _objDrag.transform.position;
            _objDrag.transform.localScale = Vector3.one * 8f;

            Vector3 objectWorldPos = _objDrag.transform.position;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = objectWorldPos.z;
            _offset = objectWorldPos - mouseWorldPos;
        }

        private void Drag()
        {
            if (_objDrag == null) return;
            var mapRenderer = _map._spriteRenderer;
            if (mapRenderer == null) return;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3 mouseForBounds = mouseWorldPos;
            mouseForBounds.z = mapRenderer.transform.position.z;
            if (mapRenderer.bounds.Contains(mouseForBounds))
            {
                Vector3 localPos = _map.transform.InverseTransformPoint(mouseWorldPos);
                var spriteWidth = mapRenderer.sprite.bounds.size.x;
                var spriteHeight = mapRenderer.sprite.bounds.size.y;

                int cellX = Mathf.RoundToInt((localPos.x / spriteWidth + 0.5f) * _map._wight);
                int cellY = Mathf.RoundToInt((localPos.y / spriteHeight + 0.5f) * _map._hight);

                cellX = Mathf.Clamp(cellX, 0, _map._wight - 1);
                cellY = Mathf.Clamp(cellY, 0, _map._hight - 1);

                float fx = ((float)cellX / _map._wight - 0.5f) * spriteWidth;
                float fy = ((float)cellY / _map._hight - 0.5f) * spriteHeight;
                Vector3 snappedLocal = new Vector3(fx, fy, 0);
                Vector3 snappedWorld = _map.transform.TransformPoint(snappedLocal);

                _objDrag.transform.localScale = Vector3.one * 8f;

                snappedWorld.z = _objDrag.transform.position.z;
                _objDrag.transform.position = snappedWorld;
            }
            else
            {
                mouseWorldPos.z = _objDrag.transform.position.z;
                _objDrag.transform.position = mouseWorldPos + _offset;
            }
        }

        private void EndDrag()
        {
            if (_objDrag != null)
            {
                bool placedOnMap = false;
                Vector3 dropPosition = _objDrag.transform.position;

                if (IsDroppedOnMap())
                {
                    var block = _objDrag.GetComponent<BlockInfo>();
                    var sr = _objDrag.GetComponent<SpriteRenderer>();

                    if (block != null && _blockManager != null && _map != null)
                    {
                        placedOnMap = _blockManager.SpawnSandWithSprite(
                            _map._map,
                            _map._spriteRenderer,
                            sr.sprite
                        );
                    }
                }

                if (placedOnMap)
                {
                    Delay(0.75f).Forget();
                    Global.Send(new SignalOpenEffectTextScore()
                    {
                        Score = Random.Range(20, 40), 
                        Position = dropPosition + Vector3.up  
                    });

                    var spawnSystem = FindObjectOfType<BlockSpawn>();
                    spawnSystem.ReturnBlock(_objDrag);
                }
                else
                {
                    _objDrag.transform.position = _startPos;
                    _objDrag.transform.localScale = Vector3.one * 5f;
                }

                _objDrag = null;
            }

            _isDragging = false;
        }


        private bool IsDroppedOnMap()
        {
            if (_map == null) return false;
            var mapRenderer = _map.GetComponent<SpriteRenderer>();
            if (mapRenderer == null) return false;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = mapRenderer.transform.position.z;
            return mapRenderer.bounds.Contains(mouseWorldPos);
        }

        private async UniTask Delay(float seconds)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(seconds));
        }

        private void OnDestroy()
        {
            _dragSub?.Dispose();
        }
    }
}