using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;

namespace BusUnjam
{
    public class BusManager : MonoBehaviour
    {
        private const float BUS_DISTANCE = 4.3f;
        private const int BUS_POOL_SIZE = 5;

        public event Action<int> OnBusArrived; // params: active buses count;
        public event Action OnAllBusDone;
        
        [SerializeField] private float _moveDuration = 2.0f;
        [SerializeField] private GameObject _busPrefab;
        [SerializeField] private ColorDataSO _colorData;
        
#if UNITY_EDITOR
        [Header("Editor")]
        [SerializeField] private bool e_SpawnRedBus = false;
        [SerializeField] private bool e_SpawnBlueBus = false;
        [SerializeField] private bool e_SpawnGreenBus = false;
        [SerializeField] private bool e_MoveToNextBus = false;
#endif
        
        private readonly List<Bus> _busPool = new();
        private readonly List<Bus> _remainLevelBuses = new();
        private readonly List<Bus> _remainActiveBuses = new();
        
        private bool _busReachingDestination = false;
        
        private void Start()
        {
            InstantiateBusPool();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (e_SpawnRedBus)
            {
                TrySpawnNewBus(eColorType.Red,
                    new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal });
                e_SpawnRedBus = false;
            }
            if (e_SpawnBlueBus)
            {
                TrySpawnNewBus(eColorType.Blue,
                    new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal });
                e_SpawnBlueBus = false;
            }
            if (e_SpawnGreenBus)
            {
                TrySpawnNewBus(eColorType.Green,
                    new[] { ePassengerType.Normal, ePassengerType.Normal, ePassengerType.Normal });
                e_SpawnGreenBus = false;
            }
            if (e_MoveToNextBus)
            {
                MoveToNextDestination().Forget();
                e_MoveToNextBus = false;
            }
#endif
        }

        // Tạo trước một số _poolSize của _busPrefab vào trong _busPool và ẩn nó đi
        private void InstantiateBusPool()
        {
            for (int i = 0; i < BUS_POOL_SIZE; i++)
            {
                GameObject busGameObject = Instantiate(_busPrefab, transform);
                busGameObject.SetActive(false);
                Bus bus = busGameObject.GetComponent<Bus>();
                if (bus == null)
                {
                    Debug.LogError("There is no bus component in bus prefab!.");
                    return;
                }
                _busPool.Add(bus);
            }
            Debug.Log($"Initialized bus pool with {BUS_POOL_SIZE} buses.");
        }

        public void LoadBusFromLevel(LevelData levelData)
        {
            Bus[] list = levelData.buses;
            for (int i = 0; i < list.Length; i++)
            {
                Bus bus = list[i];
                if (!TrySpawnNewBus(bus.colorType, bus.required))
                {
                    _remainLevelBuses.Add(bus);
                }
            }
        }

        public bool TrySpawnNewBusFromLevel()
        {
            if (_remainLevelBuses.Count == 0) return false;
            foreach (Bus bus in _remainLevelBuses)
            {
                if (TrySpawnNewBus(bus.colorType, bus.required))
                {
                    _remainLevelBuses.Remove(bus);
                    return true;
                }
            }
            return false;
        }
        
        public async UniTask CheckCurrentBusRequirements()
        {
            if (_remainActiveBuses.Count == 0) return;
            Bus bus = _remainActiveBuses[0];
            if (bus != null && bus.IsFull())
            {
                await MoveToNextDestination();
                if (!TrySpawnNewBusFromLevel() && _remainActiveBuses.Count == 0)
                {
                    // Level complete
                    OnAllBusDone?.Invoke();
                }
            }
        }

        public bool IsBusReachingDestination() => _busReachingDestination;
        
        // GetBusFromPool(), thay đổi màu, điều kiện, active và thêm vào hàng đợi _activeBuses
        private bool TrySpawnNewBus(eColorType colorType, ePassengerType[] passengerRequired)
        {
            Bus bus = GetBusFromPool();
            if (bus == null)
            {
                Debug.LogWarning("Cannot spawn new bus. Active queue is full!");
                return false;
            }
            bus.transform.localPosition = new Vector3(-1f * _remainActiveBuses.Count * BUS_DISTANCE, 0, 0);
            bus.colorType = colorType;
            bus.required = passengerRequired;
            bus.occupied = new[] { false, false, false };
            bus.SetColor(GetColorByColorType(colorType));
            
            _remainActiveBuses.Add(bus);
            bus.gameObject.SetActive(true);
            return true;
        }
        
        // Lấy bus đang không active từ _busPool rồi trả về tham chiếu của nó
        private Bus GetBusFromPool()
        {
            Bus bus = _busPool.FirstOrDefault(bus => !bus.gameObject.activeSelf);
            if (bus == null) return null;
            return bus;
        }
        
        // Tắt active của bus và reset trạng thái
        private void ReturnBusToPool(Bus bus)
        {
            bus.transform.DOKill();
            bus.gameObject.SetActive(false);
        }
        
        // Di chuyển xe bus đến địa điểm tiếp theo
        private async UniTask MoveToNextDestination()
        {
            if (_remainActiveBuses.Count == 0 || _busReachingDestination) return;
            _busReachingDestination = true;
            
            List<UniTask> tweenTasks = new()
            {
                _remainActiveBuses.ElementAt(0).transform.DOLocalMoveX(2.5f * BUS_DISTANCE, _moveDuration)
                    .SetEase(Ease.Linear).ToUniTask()
            };

            for (int i = 1; i < _remainActiveBuses.Count; i++)
            {
                tweenTasks.Add(
                    _remainActiveBuses.ElementAt(i).transform.DOLocalMoveX(-1f * (i-1) * BUS_DISTANCE, _moveDuration)
                        .SetEase(Ease.OutQuad).ToUniTask()
                );
            }
            await UniTask.WhenAll(tweenTasks);
            Bus finishedBus = _remainActiveBuses[0];
            ReturnBusToPool(finishedBus);
            _remainActiveBuses.Remove(finishedBus);
            
            _busReachingDestination = false;
            OnBusArrived?.Invoke(_remainActiveBuses.Count);
        }
        
        // Lấy color theo colorType
        private Color GetColorByColorType(eColorType colorType)
        {
            return _colorData.colorDataList.First(x => x.colorType == colorType).color;
        }
    }
}