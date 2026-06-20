using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives the minimap RawImage display and player/party markers.
/// Attach to the minimap RawImage GameObject on the Canvas.
///
/// Only the local player and party members appear as markers.
/// Enemies and non-party players are never shown — the minimap camera
/// already excludes their layers via MinimapCamera.excludeLayers.
/// </summary>
public class MinimapUI : MonoBehaviour
{
    [Header("Player Marker")]
    [Tooltip("Arrow/dot Image centered on the minimap — rotates to match player facing")]
    public RectTransform playerMarker;

    [Header("Party Markers")]
    [Tooltip("Prefab for a party member dot — spawned at runtime per member")]
    public GameObject partyMarkerPrefab;
    [Tooltip("Parent RectTransform that holds spawned party markers")]
    public RectTransform partyMarkerContainer;

    [Header("Minimap Camera (needed for world→minimap projection)")]
    public MinimapCamera minimapCamera;

    Transform _player;

    // Party member transforms registered at runtime
    readonly List<(Transform t, RectTransform marker)> _partyMarkers = new List<(Transform, RectTransform)>();

    void LateUpdate()
    {
        // Resolve local player lazily
        if (_player == null)
        {
            PlayerStats ps = PlayerStats.LocalInstance;
            if (ps != null) _player = ps.transform;
        }

        if (_player == null) return;

        // Rotate player arrow to match world facing
        if (playerMarker != null)
            playerMarker.localEulerAngles = new Vector3(0f, 0f, -_player.eulerAngles.y);

        // Update party member dot positions
        if (minimapCamera == null || partyMarkerContainer == null) return;

        Camera cam = minimapCamera.GetComponent<Camera>();
        if (cam == null) return;

        RectTransform mapRect = GetComponent<RectTransform>();
        if (mapRect == null) return;

        foreach (var (memberTransform, markerRect) in _partyMarkers)
        {
            if (memberTransform == null || markerRect == null) continue;

            // Project world position into minimap UV space
            Vector3 viewportPos = cam.WorldToViewportPoint(memberTransform.position);

            // Map [0,1] viewport to minimap rect
            float halfW = mapRect.rect.width  * 0.5f;
            float halfH = mapRect.rect.height * 0.5f;

            markerRect.anchoredPosition = new Vector2(
                (viewportPos.x - 0.5f) * mapRect.rect.width,
                (viewportPos.y - 0.5f) * mapRect.rect.height);
        }
    }

    /// <summary>Call this when a player joins your party to show their dot on the minimap.</summary>
    public void AddPartyMember(Transform memberTransform)
    {
        if (partyMarkerPrefab == null || partyMarkerContainer == null) return;

        GameObject go = Instantiate(partyMarkerPrefab, partyMarkerContainer);
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt != null)
            _partyMarkers.Add((memberTransform, rt));
    }

    /// <summary>Call this when a player leaves your party.</summary>
    public void RemovePartyMember(Transform memberTransform)
    {
        for (int i = _partyMarkers.Count - 1; i >= 0; i--)
        {
            if (_partyMarkers[i].t == memberTransform)
            {
                if (_partyMarkers[i].marker != null)
                    Destroy(_partyMarkers[i].marker.gameObject);
                _partyMarkers.RemoveAt(i);
            }
        }
    }
}
