using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cinemachine;
using Functionality.Entity;
using JetBrains.Annotations;
using Level_Component.Electricity;
using Level_Component;
using UnityEngine;
using Pathfinding;
using Debug = UnityEngine.Debug;

public class LevelManager : MonoBehaviour {
    public static LevelManager Instance = null;

    public static Action OnPlatformDown;
    public static Action OnPlatformTouch;
    [Header("Platform")] public float platformManualDeactivationDelay = 0.1f;
    public float deltaPlatformTouch = 0.2f;
    private float _platformTouchTimer;

    private Collider2D _playerCollider;
    private Rigidbody2D _playerRigidbody;
    private bool _platformDetection;
    private Dictionary<float, Platform> _platforms;
    private float _deltaDelay = 0f;
    private const int PlayerPriority = 8;
    
    // variables to be saved
    private uint _collectedCoin;
    
    [SerializeField] private CinemachineConfiner2D confiner;
    [SerializeField] private Transform cameraTransform;
    [NonSerialized] public PlayerController Controller;
    [ItemNotNull] private Level[] _levels;
    [SerializeField] private int activeLevelIndex = 0;
    [SerializeField] private AstarPath aStar = null;
    [SerializeField] public GameObject grid;
    private Task _checkTargetTask;

    private void Start() {
        if (Instance != null) {
            Debug.Log(Instance.transform.position + " manager");
            return;
        }

        if (grid is null) {
            throw new Exception("Grid must be initialized!");
        }

        _levels = grid.GetComponentsInChildren<Level>();
        if (_levels.Length == 0) {
            throw new Exception("Grid does not contain any level!");
        }

        Instance = this;
        // todo retrieve data
        _collectedCoin = 0;
        
        Controller = FindObjectOfType<PlayerController>();
        _playerCollider = Controller.GetComponent<Collider2D>();
        _playerRigidbody = Controller.GetComponent<Rigidbody2D>();
        _platforms = new Dictionary<float, Platform>();

        StartLevelFirst(activeLevelIndex);
    }

    private void StartLevelFirst(int levelIndex) {
        Level level = _levels[levelIndex];
        InitializePlatforms();
        InitializeTargetEntities(Controller.transform);
    }

    public void StartLevel(int levelIndex) {
        if (activeLevelIndex == levelIndex) return;
        Level level = _levels[levelIndex];

        if (activeLevelIndex >= 0) {
            Level activeLevel = _levels[activeLevelIndex];
            activeLevel.CloseLevel(Controller);
        }

        Transform tf = level.StartLevel(activeLevelIndex, Controller, cameraTransform, out confiner.m_BoundingShape2D);
        InitializePlatforms();
        InitializeTargetEntities(tf);
    }

    private void InitializeTargetEntities(Transform playerTransform) {
        // this function's exec. time is 3500 ticks (0 milliseconds)
        lock (BirdAI.MutexStopCheck) {
            BirdAI.StopCheckThread = true;
        }

        while (true) {
            if (!BirdAI.IsThereCheckThread) {
                break;
            }

            Debug.Log("yo");
            Thread.Sleep(TimeSpan.FromSeconds(0.5));
        }

        BirdAI.StopCheckThread = false;

        // A star
        if (aStar is not null) {
            aStar.Scan();
        }

        Transform[] transforms = grid.GetComponentsInChildren<Transform>();
        BirdAI.EntitiesToTrack.Clear();
        HeavyLookerAI.CordMap.Clear();
        BirdAI.EntitiesToTrack.Add(playerTransform);
        HeavyLookerAI.CordMap.Add(PlayerPriority, new HashSet<Transform>());
        HeavyLookerAI.CordMap[PlayerPriority].Add(playerTransform);
        foreach (Transform tf in transforms) {
            bool attractsBird = false;
            bool attractsHeavyLooker = false;
            int priority = PlayerPriority + 2;
            if (tf.CompareTag("Bait")) {
                attractsBird = true;
            }
            else if (tf.CompareTag("LightBait")) {
                Debug.Log("bait");
                Level_Component.Electricity.Light component = tf.GetComponent<Level_Component.Electricity.Light>();
                if (component is null) {
                    Debug.LogError("Found a light bait that doesn't have Light.cs");
                }
                else if (component.Open && component.isBait) {
                    attractsHeavyLooker = true;
                    priority = 12;
                }
            }

            if (attractsBird) {
                BirdAI.EntitiesToTrack.Add(tf);
            }

            if (attractsHeavyLooker) {
                if (!HeavyLookerAI.CordMap.ContainsKey(priority)) {
                    HeavyLookerAI.CordMap.Add(priority, new HashSet<Transform>());
                }

                HeavyLookerAI.CordMap[priority].Add(tf);
            }
        }

        foreach (Transform tf in BirdAI.EntitiesToTrack) {
            int priority = PlayerPriority + 2;

            if (tf.CompareTag("Bait")) {
                priority = 0;
            }

            BirdAI.CordMap.TryAdd(tf, new EntityValue(tf.position, priority));
        }

        
        if (!BirdAI.CordMap.IsEmpty) {
            Debug.Log("BirdAI.CheckTarget thread launched");
            BirdAI.IsThereCheckThread = true;
            _checkTargetTask = Task.Factory.StartNew(BirdAI.CheckTarget);
        }
    }

    private IEnumerator MapTransformsToCoordinates() {
        int i = 0;
        foreach (Transform tf in BirdAI.EntitiesToTrack) {
            i++;
            BirdAI.CordMap.AddOrUpdate(tf, new EntityValue(tf.position, BirdAI.CordMap[tf].Priority),
                (_, value) => new EntityValue(tf.position, value.Priority));
            if (i % 5 == 0) yield return null;
        }
    }

    private void OnEnable() {
        OnPlatformDown += DisableRb;
        OnPlatformTouch += PlatformTouch;
    }

    private void OnDisable() {
        OnPlatformDown -= DisableRb;
        OnPlatformTouch -= PlatformTouch;
    }

    private void DisableRb() {
        _deltaDelay = platformManualDeactivationDelay;
    }

    private void PlatformTouch() {
        _platformTouchTimer = deltaPlatformTouch;
    }

    private void InitializePlatforms() {
        _platforms.Clear();
        Platform[] items = grid.GetComponentsInChildren<Platform>();
        foreach (Platform platform in items) {
            _platforms.Add(platform.transform.position.y, platform);
        }

        _platformDetection = _platforms.Count > 0;
    }


    private void FixedUpdate() {
        if (_platformDetection) {
            float y = _playerCollider.bounds.min.y;
            bool goingDown = _playerRigidbody.velocity.y < Mathf.Epsilon;

            foreach ((float f, Platform platform) in _platforms) {
                platform.Rb.simulated = f < y && goingDown &&
                                        (_deltaDelay < Mathf.Epsilon || _platformTouchTimer < Mathf.Epsilon);
            }
        }

        if (_deltaDelay > -Mathf.Epsilon)
            _deltaDelay -= Time.deltaTime;

        if (_platformTouchTimer > -Mathf.Epsilon) {
            _platformTouchTimer -= Time.deltaTime;
        }

        // path finding
        StartCoroutine(MapTransformsToCoordinates());
    }

    private void OnDestroy() {
        
        // lock (BirdAI.MutexStopCheck) {
        //     BirdAI.StopCheckThread = true;
        // }
        
    }

    public void CollectCoin() {
        _collectedCoin++;
    }
}