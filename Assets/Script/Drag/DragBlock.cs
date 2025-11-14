using System;
using R3;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sand
{
    public class DragBlock : MonoBehaviour
    {
        [SerializeField] private BlockManager _blockManager;
        [SerializeField] private RenMap _map;
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
            {
                StartDrag();
            }
            else if (Input.GetMouseButton(0) && _isDragging)
            {
                Drag();
            }
            else if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                EndDrag();
            }
        }

        private void StartDrag()
        {
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero, Mathf.Infinity, 1 << 6);

            if (hit.collider == null) return;
            _objDrag = hit.collider.gameObject;
            _objDrag.transform.localScale = Vector3.one;
            _isDragging = true;
            _startPos = _objDrag.transform.position;

            Vector3 objectWorldPos = _objDrag.transform.position;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = objectWorldPos.z;
            _offset = objectWorldPos - mouseWorldPos;
        }

        private void Drag()
        {
            if (_objDrag == null) return;
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            _objDrag.transform.localScale = Vector3.one;
            mouseWorldPos.z = _objDrag.transform.position.z;
            _objDrag.transform.position = mouseWorldPos + _offset;
        }

        private void EndDrag()
        {
            if (_objDrag != null)
            {
                bool placedOnMap = false;

                if (IsDroppedOnMap())
                {
                    var block = _objDrag.GetComponent<BlockTittle>();
                    if (block != null && _blockManager != null && _map != null)
                    {
                        // thử spawn; nếu chồng lên cát sẽ trả về false
                        placedOnMap = _blockManager.SpawnSandWithType(
                            _map._map,
                            _map._spriteRenderer,
                            block.IdColor,
                            block.TypeBlock
                        );
                    }
                }

                if (placedOnMap)
                {
                    // đặt được thì trả block về pool, sinh block mới
                    var spawnSystem = FindObjectOfType<SpawnVisual>();
                    spawnSystem.ReturnBlock(_objDrag);
                }
                else
                {
                    // không đặt được (không rơi vào map hoặc bị trùng ô) -> về vị trí cũ
                    _objDrag.transform.position = _startPos;
                    _objDrag.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
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

        private void OnDestroy()
        {
            _dragSub?.Dispose();
        }
    }
}