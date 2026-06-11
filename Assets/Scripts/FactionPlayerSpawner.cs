using FishNet.Connection;
using FishNet.Managing;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Spawns the correct player prefab based on faction and gender.
/// Four prefabs total: Slayer Male/Female, Vampire Male/Female.
/// </summary>
public class FactionPlayerSpawner : MonoBehaviour
{
    [Header("Slayer Prefabs")]
    public NetworkObject slayerMalePrefab;
    public NetworkObject slayerFemalePrefab;

    [Header("Vampire Prefabs")]
    public NetworkObject vampireMalePrefab;
    public NetworkObject vampireFemalePrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private NetworkManager _networkManager;
    private int _nextSpawn = 0;

    // Set from GameSessionBridge before the network scene loads
    private static PlayerFaction _pendingFaction  = PlayerFaction.Slayer;
    private static PlayerGender  _pendingGender   = PlayerGender.Male;
    private static string        _pendingClassName = "";
    private static bool          _factionSet       = false;

    public static void SetPendingFaction(PlayerFaction faction, string className = "",
                                         PlayerGender gender = PlayerGender.Male)
    {
        _pendingFaction   = faction;
        _pendingClassName = className;
        _pendingGender    = gender;
        _factionSet       = true;
        Debug.Log($"[FactionPlayerSpawner] Pending faction={faction} gender={gender} class={className}");
    }

    void Start()
    {
        _networkManager = GetComponentInParent<NetworkManager>();
        if (_networkManager == null)
            _networkManager = FishNet.InstanceFinder.NetworkManager;

        if (_networkManager == null)
        {
            Debug.LogError("[FactionPlayerSpawner] NetworkManager not found.");
            return;
        }

        if (_networkManager.SceneManager == null)
        {
            Debug.LogError("[FactionPlayerSpawner] SceneManager not ready yet.");
            return;
        }

        _networkManager.SceneManager.OnClientLoadedStartScenes += OnClientLoaded;
    }

    void OnDestroy()
    {
        if (_networkManager != null && _networkManager.SceneManager != null)
            _networkManager.SceneManager.OnClientLoadedStartScenes -= OnClientLoaded;
    }

    void OnClientLoaded(NetworkConnection conn, bool asServer)
    {
        if (!asServer) return;

        CharacterData character = GameSession.Instance?.SelectedCharacter;

        PlayerFaction faction = PlayerFaction.Slayer;
        PlayerGender  gender  = PlayerGender.Male;
        string        className = "";

        if (character != null)
        {
            faction   = character.faction;
            gender    = character.gender;
            className = character.GetClassName();
        }
        else if (_factionSet)
        {
            faction   = _pendingFaction;
            gender    = _pendingGender;
            className = _pendingClassName;
        }

        NetworkObject prefabToSpawn = GetPrefab(faction, gender);

        Debug.Log($"[FactionPlayerSpawner] Spawning {prefabToSpawn?.name ?? "NULL"} " +
                  $"faction={faction} gender={gender} class={className}");

        if (prefabToSpawn == null)
        {
            Debug.LogError("[FactionPlayerSpawner] No prefab found — assign all 4 prefabs in the Inspector.");
            return;
        }

        Vector3 spawnPos = GetNextSpawnPosition();
        NetworkObject nob = _networkManager.GetPooledInstantiated(prefabToSpawn, spawnPos, Quaternion.identity, true);
        _networkManager.ServerManager.Spawn(nob, conn);
        _networkManager.SceneManager.AddOwnerToDefaultScene(nob);
    }

    NetworkObject GetPrefab(PlayerFaction faction, PlayerGender gender)
    {
        if (faction == PlayerFaction.Vampire)
            return gender == PlayerGender.Female ? vampireFemalePrefab : vampireMalePrefab;
        else
            return gender == PlayerGender.Female ? slayerFemalePrefab  : slayerMalePrefab;
    }

    Vector3 GetNextSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return Vector3.zero;

        Transform point = spawnPoints[_nextSpawn];
        _nextSpawn = (_nextSpawn + 1) % spawnPoints.Length;
        return point != null ? point.position : Vector3.zero;
    }
}
