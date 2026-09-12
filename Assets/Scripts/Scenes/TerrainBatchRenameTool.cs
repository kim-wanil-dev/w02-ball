using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TerrainBatchRenameTool
{
    [MenuItem("Tools/Rename Selected Terrains")]
    private static void RenameSelectedTerrains()
    {
        GameObject[] objects = Selection.gameObjects
            .OrderBy(GetHierarchyPath)
            .ToArray();

        for (int i = 0; i < objects.Length; i++)
        {
            Terrain terrain = objects[i].GetComponent<Terrain>();

            if (terrain == null)
                continue;

            string newName = $"Outline_Terrain_{(char)('A' + i)}";

            // GameObject 이름 변경
            Undo.RecordObject(objects[i], "Rename Terrain");
            objects[i].name = newName;

            // TerrainData Asset 이름 변경
            TerrainData terrainData = terrain.terrainData;

            if (terrainData == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(terrainData);

            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogWarning(
                    $"{objects[i].name}: TerrainData is not an asset.");
                continue;
            }

            string error = AssetDatabase.RenameAsset(
                assetPath,
                newName
            );

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(
                    $"Failed to rename TerrainData: {error}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static string GetHierarchyPath(GameObject obj)
    {
        string path = obj.transform.GetSiblingIndex().ToString("D4");

        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path =
                parent.GetSiblingIndex().ToString("D4")
                + "/"
                + path;

            parent = parent.parent;
        }

        return path;
    }
}
