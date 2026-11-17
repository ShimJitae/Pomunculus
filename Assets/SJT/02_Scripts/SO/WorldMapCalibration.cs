using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WorldMapCalibration", menuName = "Scriptable Objects/World Map_Calibration")]
public sealed class WorldMapCalibration : ScriptableObject
{
    [Serializable]
    private struct CalibrationPoint
    {
        [SerializeField] private Vector2 worldXZ;
        [SerializeField] private Vector2 imageUV;

        public Vector2 WorldXZ => worldXZ;
        public Vector2 ImageUV => imageUV;
    }

    private const float c_minDoubleArea = 0.00001f;
    private const float c_edgeTolerance = 0.00001f;

    [SerializeField]
    private CalibrationPoint[] points =
        Array.Empty<CalibrationPoint>();

    [SerializeField]
    private Vector3Int[] triangles =
        Array.Empty<Vector3Int>();

    public bool TryGetMapUV(Vector2 worldXZ, out Vector2 imageUV)
    {
        foreach (Vector3Int triangle in triangles)
        {
            if (!IsValidIndex(triangle.x) ||
                !IsValidIndex(triangle.y) ||
                !IsValidIndex(triangle.z))
            {
                continue;
            }

            CalibrationPoint a = points[triangle.x];
            CalibrationPoint b = points[triangle.y];
            CalibrationPoint c = points[triangle.z];

            Vector2 ab = b.WorldXZ - a.WorldXZ;
            Vector2 ac = c.WorldXZ - a.WorldXZ;
            Vector2 offset = worldXZ - a.WorldXZ;

            float doubleArea = Cross(ab, ac);
            if (Mathf.Abs(doubleArea) < c_minDoubleArea)
            {
                continue;
            }

            // 실제 삼각형 안에서 각 꼭짓점에 대한 비율을 구한다.
            float weightB = Cross(offset, ac) / doubleArea;
            float weightC = Cross(ab, offset) / doubleArea;
            float weightA = 1f - weightB - weightC;

            if (weightA < -c_edgeTolerance ||
                weightB < -c_edgeTolerance ||
                weightC < -c_edgeTolerance)
            {
                continue;
            }

            // 경계에서 생긴 작은 계산 오차를 보정한다.
            weightA = Mathf.Max(0f, weightA);
            weightB = Mathf.Max(0f, weightB);
            weightC = Mathf.Max(0f, weightC);
            float total = weightA + weightB + weightC;

            imageUV = (a.ImageUV * weightA +
                       b.ImageUV * weightB +
                       c.ImageUV * weightC) / total;
            return true;
        }

        imageUV = default;
        return false;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < points.Length;
    }

    private static float Cross(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }
}