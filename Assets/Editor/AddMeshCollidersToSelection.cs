using UnityEngine;
using UnityEditor;

public static class AddMeshCollidersToSelection
{
    [MenuItem("Tools/Collision/Add/Fix MeshColliders On Selection")]
    public static void AddOrFix()
    {
        var selected = Selection.gameObjects;
        int fixedCount = 0;

        foreach (var go in selected)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            var mc = go.GetComponent<MeshCollider>();
            if (mc == null) mc = go.AddComponent<MeshCollider>();

            if (mc.sharedMesh != mf.sharedMesh)
            {
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
                mc.isTrigger = false;
                fixedCount++;
            }
        }

        Debug.Log($"Added/fixed MeshColliders on {fixedCount} objects.");
    }
}