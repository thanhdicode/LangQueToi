// ──────────────────────────────────────────────
// TheSprouty | Fishing/FishSpawnZone.cs
// Defines a fishing zone: reads spawn positions from a Tilemap layer,
// configures which FishShadow prefabs can appear and how many.
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class FishSpawnZone : MonoBehaviour
{
    // ----------------------------------------------------------
    // Private state
    // ----------------------------------------------------------
    private Tilemap _tilemap;

    // ----------------------------------------------------------
    // Serialized fields
    // ----------------------------------------------------------
    [Header("Shadow Config")]
    [Tooltip("Big / Medium / Small FishShadow prefabs có thể spawn trong zone này.")]
    [SerializeField] private GameObject[] shadowPrefabs;

    [Header("Spawn Settings")]
    [Tooltip("Số shadow tối đa tồn tại cùng lúc trong zone này.")]
    [SerializeField] private int maxFishCount = 3;

    // ----------------------------------------------------------
    // Unity lifecycle
    // ----------------------------------------------------------
    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
    }

    // ----------------------------------------------------------
    // Properties
    // ----------------------------------------------------------
    public int MaxFishCount => maxFishCount;

    // ----------------------------------------------------------
    // Public API
    // ----------------------------------------------------------

    /// <summary>Returns a random world position from valid spawn cells on the tilemap.</summary>
    public Vector3 GetRandomSpawnPosition()
    {
        List<Vector3Int> cells = GetAllCells();
        if (cells.Count == 0) return transform.position;

        Vector3Int cell = cells[Random.Range(0, cells.Count)];
        return _tilemap.GetCellCenterWorld(cell);
    }

    /// <summary>Returns a random shadow prefab (equal chance for each).</summary>
    public GameObject GetRandomShadowPrefab()
    {
        if (shadowPrefabs == null || shadowPrefabs.Length == 0) return null;
        return shadowPrefabs[Random.Range(0, shadowPrefabs.Length)];
    }

    // ----------------------------------------------------------
    // Private helpers
    // ----------------------------------------------------------
    private List<Vector3Int> GetAllCells()
    {
        List<Vector3Int> cells = new();
        if (_tilemap == null) return cells;

        BoundsInt bounds = _tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (_tilemap.GetTile(pos) != null)
                cells.Add(pos);
        }
        return cells;
    }
}
