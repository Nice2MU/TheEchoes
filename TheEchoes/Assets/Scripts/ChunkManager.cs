using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform blocksParent;

    [Header("Grid Size (World Units)")]
    public float blockWidth = 20f;
    public float blockHeight = 20f;

    [Header("Active Radius (in blocks)")]
    public int radiusX = 1;
    public int radiusY = 1;

    [Header("Discovery")]
    public bool scanRecursively = true;
    public bool autoRebuildOnStart = true;

    private readonly Dictionary<Vector2Int, GameObject> grid = new Dictionary<Vector2Int, GameObject>();
    private readonly List<GameObject> allBlocks = new List<GameObject>();
    private Vector2Int lastCenter = new Vector2Int(int.MinValue, int.MinValue);
    private readonly List<KeyValuePair<Vector2Int, GameObject>> _iter = new List<KeyValuePair<Vector2Int, GameObject>>();

    void Awake()
    {
        if (autoRebuildOnStart) RebuildIndexMap();
        foreach (var go in allBlocks) if (go) go.SetActive(false);
        ForceRefresh();
    }

    void Update()
    {
        if (!player || !blocksParent || blockWidth <= 0f || blockHeight <= 0f || allBlocks.Count == 0) return;

        var center = new Vector2Int(
            Mathf.FloorToInt(player.position.x / blockWidth),
            Mathf.FloorToInt(player.position.y / blockHeight)
        );

        if (center != lastCenter)
        {
            lastCenter = center;

            HashSet<Vector2Int> want = new HashSet<Vector2Int>();
            for (int iy = center.y - radiusY; iy <= center.y + radiusY; iy++)
                for (int ix = center.x - radiusX; ix <= center.x + radiusX; ix++)
                    want.Add(new Vector2Int(ix, iy));

            _iter.Clear();
            foreach (var kv in grid) _iter.Add(kv);
            foreach (var kv in _iter)
            {
                bool shouldOn = want.Contains(kv.Key);
                if (kv.Value && kv.Value.activeSelf != shouldOn)
                    kv.Value.SetActive(shouldOn);
            }

            foreach (var go in allBlocks)
            {
                if (!go) continue;
                Vector3 p = go.transform.position;
                int ix = Mathf.FloorToInt(p.x / blockWidth);
                int iy = Mathf.FloorToInt(p.y / blockHeight);
                bool shouldOn = (ix >= center.x - radiusX && ix <= center.x + radiusX) &&
                                (iy >= center.y - radiusY && iy <= center.y + radiusY);
                if (go.activeSelf != shouldOn) go.SetActive(shouldOn);
            }
        }
    }

    public void RebuildIndexMap()
    {
        grid.Clear();
        allBlocks.Clear();
        if (!blocksParent) return;

        if (scanRecursively)
            CollectChildrenRecursive(blocksParent, allBlocks);
        else
            for (int i = 0; i < blocksParent.childCount; i++)
                allBlocks.Add(blocksParent.GetChild(i).gameObject);

        foreach (var go in allBlocks)
        {
            if (!go) continue;
            int ix = Mathf.FloorToInt(go.transform.position.x / blockWidth);
            int iy = Mathf.FloorToInt(go.transform.position.y / blockHeight);
            var key = new Vector2Int(ix, iy);

            if (grid.ContainsKey(key))

            grid[key] = go;
        }
    }

    private void CollectChildrenRecursive(Transform root, List<GameObject> outList)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            outList.Add(child.gameObject);
            if (child.childCount > 0)
                CollectChildrenRecursive(child, outList);
        }
    }

    public void ForceRefresh()
    {
        lastCenter = new Vector2Int(int.MinValue, int.MinValue);
        Update();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!player || blockWidth <= 0f || blockHeight <= 0f) return;
        var center = new Vector2Int(
            Mathf.FloorToInt(player.position.x / blockWidth),
            Mathf.FloorToInt(player.position.y / blockHeight)
        );
        Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
        for (int iy = center.y - radiusY; iy <= center.y + radiusY; iy++)
            for (int ix = center.x - radiusX; ix <= center.x + radiusX; ix++)
            {
                Vector3 c = new Vector3((ix + 0.5f) * blockWidth, (iy + 0.5f) * blockHeight, 0f);
                Gizmos.DrawWireCube(c, new Vector3(blockWidth, blockHeight, 0.05f));
            }
    }
#endif
}