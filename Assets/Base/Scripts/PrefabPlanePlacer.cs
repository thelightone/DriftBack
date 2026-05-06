using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class PrefabPlanePlacer : MonoBehaviour
{
    private enum SpawnMode
    {
        Grid,
        AlongRoadSides
    }

    [Header("Prefabs")]
    [SerializeField] private List<GameObject> prefabs = new List<GameObject>();

    [Header("Placement")]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.Grid;
    [SerializeField] private int count = 20;
    [SerializeField] private int columns = 5;
    [SerializeField] private int rows = 4;
    [SerializeField] private Vector2 spacing = new Vector2(2f, 2f);
    [SerializeField] private Transform parentContainer;
    [SerializeField] private Transform roadTransform;
    [SerializeField] private float sideOffsetFromRoad = 1.5f;

    [Header("Transform")]
    [SerializeField] private Vector3 itemScale = Vector3.one;

    [Header("Behavior")]
    [SerializeField] private bool clearBeforeGenerate = true;

#if UNITY_EDITOR
    [ContextMenu("Generate In Editor")]
    private void GenerateInEditor()
    {
        if (prefabs == null || prefabs.Count == 0)
        {
            Debug.LogWarning("PrefabPlanePlacer: list of prefabs is empty.", this);
            return;
        }

        if (count <= 0)
        {
            Debug.LogWarning("PrefabPlanePlacer: count must be greater than zero.", this);
            return;
        }

        if (spawnMode == SpawnMode.Grid && columns <= 0)
        {
            Debug.LogWarning("PrefabPlanePlacer: columns must be greater than zero.", this);
            return;
        }
        
        if (spawnMode == SpawnMode.Grid && rows <= 0)
        {
            Debug.LogWarning("PrefabPlanePlacer: rows must be greater than zero.", this);
            return;
        }

        Bounds roadBounds = default;
        if (spawnMode == SpawnMode.AlongRoadSides && !TryGetRoadBounds(out roadBounds))
        {
            Debug.LogWarning("PrefabPlanePlacer: assign road object with Collider or Renderer.", this);
            return;
        }

        EnsureParentContainer();

        if (clearBeforeGenerate)
        {
            ClearContainerInternal();
        }

        int spawnCount = count;
        if (spawnMode == SpawnMode.Grid)
        {
            spawnCount = Mathf.Min(count, columns * rows);
        }

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
            if (prefab == null)
            {
                continue;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parentContainer);
            if (instance == null)
            {
                continue;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Generate prefab on plane");

            Vector3 localSpawnPosition;
            if (spawnMode == SpawnMode.AlongRoadSides)
            {
                Vector3 worldSpawnPosition = GetSpawnPositionAlongRoad(i, roadBounds);
                localSpawnPosition = parentContainer.InverseTransformPoint(worldSpawnPosition);
            }
            else
            {
                localSpawnPosition = GetSpawnPositionOnGrid(i);
            }

            instance.transform.localPosition = localSpawnPosition;

            float yRotation = 90f * Random.Range(0, 4);
            instance.transform.localRotation = Quaternion.Euler(-90f, yRotation, 0f);

            instance.transform.localScale = itemScale;
        }
    }

    private Vector3 GetSpawnPositionOnGrid(int index)
    {
        int col = index % columns;
        int row = index / columns;

        return new Vector3(col * spacing.x, 0f, row * spacing.y);
    }

    private Vector3 GetSpawnPositionAlongRoad(int index, Bounds roadBounds)
    {
        Vector3 roadCenter = roadBounds.center;
        float roadLength = Vector3.Dot(roadBounds.size, AbsVector3(roadTransform.forward.normalized));
        float roadWidth = Vector3.Dot(roadBounds.size, AbsVector3(roadTransform.right.normalized));

        float normalizedIndex = (index + 0.5f) / count;
        float lengthOffset = Mathf.Lerp(-roadLength * 0.5f, roadLength * 0.5f, normalizedIndex);

        int sideSign = Random.value < 0.5f ? -1 : 1;
        float sideOffset = (roadWidth * 0.5f) + sideOffsetFromRoad;

        Vector3 alongRoad = roadTransform.forward.normalized * lengthOffset;
        Vector3 toSide = roadTransform.right.normalized * (sideOffset * sideSign);

        return roadCenter + alongRoad + toSide;
    }

    private bool TryGetRoadBounds(out Bounds bounds)
    {
        bounds = default;
        if (roadTransform == null)
        {
            return false;
        }

        Collider roadCollider = roadTransform.GetComponent<Collider>();
        if (roadCollider != null)
        {
            bounds = roadCollider.bounds;
            return true;
        }

        Renderer roadRenderer = roadTransform.GetComponent<Renderer>();
        if (roadRenderer != null)
        {
            bounds = roadRenderer.bounds;
            return true;
        }

        return false;
    }

    private static Vector3 AbsVector3(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    [ContextMenu("Clear Generated")]
    private void ClearGenerated()
    {
        if (parentContainer == null)
        {
            return;
        }

        ClearContainerInternal();
    }

    private void EnsureParentContainer()
    {
        if (parentContainer != null)
        {
            return;
        }

        GameObject container = new GameObject("Generated Prefabs");
        Undo.RegisterCreatedObjectUndo(container, "Create generated prefabs container");
        container.transform.SetParent(transform, false);
        parentContainer = container.transform;
        EditorUtility.SetDirty(this);
    }

    private void ClearContainerInternal()
    {
        for (int i = parentContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = parentContainer.GetChild(i);
            Undo.DestroyObjectImmediate(child.gameObject);
        }
    }
#endif
}
