using FishNet.Object;
using FishNet.Object.Synchronizing;
using TMPro;
using UnityEngine;

public class PlayerNameplate : NetworkBehaviour
{
    [Header("References")]
    public TMP_Text nameText;
    public Transform nameplateRoot;

    [Header("Colors")]
    public Color ownNameColor   = new Color(0.4f, 1f, 0.4f); // green for yourself
    public Color otherNameColor = Color.white;

    [Header("Billboard")]
    public float verticalOffset = 2.4f; // height above root

    private readonly SyncVar<string> _playerName = new SyncVar<string>();
    private Camera _mainCamera;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        _playerName.OnChange += OnNameChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();
        _playerName.OnChange -= OnNameChanged;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Read name from GameSession if available (host/listen server)
        string characterName = GameSession.Instance?.SelectedCharacter?.characterName ?? "";
        if (!string.IsNullOrEmpty(characterName))
            _playerName.Value = characterName;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        _mainCamera = Camera.main;

        // Apply color — green for own player, white for others
        if (nameText != null)
            nameText.color = IsOwner ? ownNameColor : otherNameColor;

        // Apply current synced name
        if (nameText != null && !string.IsNullOrEmpty(_playerName.Value))
            nameText.text = _playerName.Value;

        // Owner tells server their name
        if (IsOwner)
        {
            string characterName = GameSession.Instance?.SelectedCharacter?.characterName ?? "Unknown";
            ServerSetName(characterName);
        }
    }

    [ServerRpc]
    void ServerSetName(string characterName)
    {
        _playerName.Value = characterName;
    }

    void OnNameChanged(string prev, string next, bool asServer)
    {
        if (nameText != null)
            nameText.text = next;
    }

    void LateUpdate()
    {
        // Keep nameplate at correct height above player root
        if (nameplateRoot != null)
            nameplateRoot.position = transform.position + Vector3.up * verticalOffset;

        // Billboard — always face the camera
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        if (_mainCamera != null && nameplateRoot != null)
            nameplateRoot.rotation = Quaternion.LookRotation(
                nameplateRoot.position - _mainCamera.transform.position);
    }
}
