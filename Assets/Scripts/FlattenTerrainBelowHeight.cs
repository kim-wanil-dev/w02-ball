using UnityEngine;

public class FlattenTerrainBelowHeight : MonoBehaviour
{
    [Header("설정")]
    [Tooltip("이 높이(World Unit) 이하인 영역을 0으로 flattening합니다.")]
    public float thresholdHeight = 60f;

    [ContextMenu("Process Child Terrain Height")]
    public void ProcessChildTerrain()
    {
        // 1. 자식 오브젝트에서 Terrain 컴포넌트 찾기
        Terrain childTerrain = GetComponentInChildren<Terrain>();

        if (childTerrain == null)
        {
            Debug.LogError("자식 오브젝트에서 Terrain 컴포넌트를 찾을 수 없습니다.");
            return;
        }

        TerrainData terrainData = childTerrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogError("TerrainData가 존재하지 않습니다.");
            return;
        }

        // 2. Terrain의 전체 높이(Y축 크기) 가져오기
        float maxTerrainHeight = terrainData.size.y;

        // World 높이(60)를 TerrainData의 정규화된 값(0.0 ~ 1.0)으로 변환
        float normalizedThreshold = thresholdHeight / maxTerrainHeight;

        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;

        // 3. 현재 높이 맵 데이터 가져오기 (0, 0 위치부터 전체 해상도 크기)
        float[,] heights = terrainData.GetHeights(0, 0, width, height);

        int modifiedCount = 0;

        // 4. 높이값 순회 및 조건에 맞게 변경
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (heights[y, x] <= normalizedThreshold)
                {
                    heights[y, x] = 0f;
                    modifiedCount++;
                }
            }
        }

        // 5. 변경된 높이 맵을 TerrainData에 다시 적용
        terrainData.SetHeights(0, 0, heights);

        Debug.Log($"Terrain 높이 수정 완료! 기준 높이: {thresholdHeight}m 이하 영역 ({modifiedCount}개 지점)이 0으로 변경되었습니다.");
    }
}
