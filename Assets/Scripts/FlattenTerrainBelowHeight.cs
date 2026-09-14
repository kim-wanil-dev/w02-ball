using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class FlattenTerrainBelowHeight : MonoBehaviour
{
    [Header("타겟 Terrain 리스트")]
    [SerializeField] private List<Terrain> targetTerrains = new();

    [Header("설정")]
    [Tooltip("이 높이(World Unit, 미터) 이하인 영역을 0으로 변경합니다.")]
    public float thresholdHeight = 60f;

    [ContextMenu("Process Terrains Height")]
    public void ProcessTerrains()
    {
        if (targetTerrains == null || targetTerrains.Count == 0)
        {
            Debug.LogError("Target Terrains 리스트가 비어 있습니다. 인스펙터에서 Terrain들을 넣어주세요.");
            return;
        }

        foreach (Terrain terrain in targetTerrains)
        {
            if (terrain == null) continue;

            TerrainData terrainData = terrain.terrainData;
            if (terrainData == null) continue;

            float maxTerrainHeight = terrainData.size.y;
            float normalizedThreshold = thresholdHeight / maxTerrainHeight;

            int res = terrainData.heightmapResolution;
            float[,] heights = terrainData.GetHeights(0, 0, res, res);

            int modifiedCount = 0;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    if (heights[y, x] <= normalizedThreshold)
                    {
                        heights[y, x] = 0f;
                        modifiedCount++;
                    }
                }
            }

            terrainData.SetHeights(0, 0, heights);

#if UNITY_EDITOR
            EditorUtility.SetDirty(terrainData);
#endif
            Debug.Log($"[{terrain.name}] 높이 수정 완료! {thresholdHeight}m 이하 영역 ({modifiedCount}개 지점) 변경됨.");
        }

#if UNITY_EDITOR
        AssetDatabase.SaveAssets();
#endif
    }
}
