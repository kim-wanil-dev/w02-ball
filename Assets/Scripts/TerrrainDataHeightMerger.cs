using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 두 TerrainData의 heightmap을 샘플별로 비교하여
/// 더 높은 height를 가진 값을 선택해서 결과 TerrainData에 적용한다.
///
/// result[x,z] = Max(sourceA[x,z], sourceB[x,z])
///
/// 주의:
/// - 두 TerrainData의 heightmapResolution이 같아야 한다.
/// - size가 다르더라도 height의 "정규화 값" 자체를 비교한다.
/// </summary>
public class TerrainDataHeightMerger : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private TerrainData sourceA;
    [SerializeField] private TerrainData sourceB;

    [Header("Result")]
    [SerializeField] private TerrainData resultTerrainData;

    [Header("Options")]
    [SerializeField] private bool preserveExistingResultHeight = false;

#if UNITY_EDITOR

    [ContextMenu("Merge Higher Heights")]
    public void MergeHigherHeights()
    {
        if (sourceA == null)
        {
            Debug.LogError("Source A가 지정되지 않았습니다.", this);
            return;
        }

        if (sourceB == null)
        {
            Debug.LogError("Source B가 지정되지 않았습니다.", this);
            return;
        }

        if (resultTerrainData == null)
        {
            Debug.LogError("Result TerrainData가 지정되지 않았습니다.", this);
            return;
        }

        int resolutionA = sourceA.heightmapResolution;
        int resolutionB = sourceB.heightmapResolution;
        int resolutionResult = resultTerrainData.heightmapResolution;

        if (resolutionA != resolutionB)
        {
            Debug.LogError(
                $"Source A/B의 heightmapResolution이 다릅니다. " +
                $"A={resolutionA}, B={resolutionB}",
                this);

            return;
        }

        if (resolutionResult != resolutionA)
        {
            Debug.LogError(
                $"Result TerrainData의 heightmapResolution이 다릅니다. " +
                $"Source={resolutionA}, Result={resolutionResult}",
                this);

            return;
        }

        Undo.RegisterCompleteObjectUndo(
            resultTerrainData,
            "Merge Terrain Higher Heights");

        float[,] heightsA = sourceA.GetHeights(
            0,
            0,
            resolutionA,
            resolutionA);

        float[,] heightsB = sourceB.GetHeights(
            0,
            0,
            resolutionB,
            resolutionB);

        float[,] result;

        if (preserveExistingResultHeight)
        {
            result = resultTerrainData.GetHeights(
                0,
                0,
                resolutionResult,
                resolutionResult);
        }
        else
        {
            result = new float[resolutionResult, resolutionResult];
        }

        int replacedByA = 0;
        int replacedByB = 0;

        for (int z = 0; z < resolutionA; z++)
        {
            for (int x = 0; x < resolutionA; x++)
            {
                float heightA = heightsA[z, x];
                float heightB = heightsB[z, x];

                float higherHeight;

                if (heightA >= heightB)
                {
                    higherHeight = heightA;
                    replacedByA++;
                }
                else
                {
                    higherHeight = heightB;
                    replacedByB++;
                }

                if (preserveExistingResultHeight)
                {
                    if (result[z, x] < higherHeight)
                    {
                        result[z, x] = higherHeight;
                    }
                }
                else
                {
                    result[z, x] = higherHeight;
                }
            }
        }

        resultTerrainData.SetHeights(
            0,
            0,
            result);

        EditorUtility.SetDirty(resultTerrainData);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"Terrain 높이 병합 완료\n" +
            $"Resolution: {resolutionA} x {resolutionA}\n" +
            $"A가 더 높은 샘플: {replacedByA}\n" +
            $"B가 더 높은 샘플: {replacedByB}\n" +
            $"Result: {resultTerrainData.name}",
            this);
    }

#endif
}
