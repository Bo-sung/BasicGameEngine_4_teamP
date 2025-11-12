using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 간단한 아이콘 제너레이터 (임시용)
/// 메뉴: Tools > Generate Simple Icons
/// </summary>
public class SimpleIconGenerator : EditorWindow
{
    private string savePath = "Assets/UI/Generated/Icons";

    [MenuItem("Tools/Generate Simple Icons")]
    public static void ShowWindow()
    {
        GetWindow<SimpleIconGenerator>("Simple Icon Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("간단한 아이콘 생성 (임시용)", EditorStyles.boldLabel);
        GUILayout.Space(10);

        savePath = EditorGUILayout.TextField("저장 경로:", savePath);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("실제 게임에는 전문 아이콘을 사용하는 것을 권장합니다.\n이 스크립트는 프로토타입용 임시 아이콘을 생성합니다.", MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("모든 기본 아이콘 생성", GUILayout.Height(40)))
        {
            GenerateAllIcons();
        }

        GUILayout.Space(20);
        GUILayout.Label("개별 생성:", EditorStyles.boldLabel);

        if (GUILayout.Button("공격력 아이콘 (검)"))
        {
            GenerateSwordIcon();
        }

        if (GUILayout.Button("공격속도 아이콘 (번개)"))
        {
            GenerateLightningIcon();
        }

        if (GUILayout.Button("체력 아이콘 (하트)"))
        {
            GenerateHeartIcon();
        }

        if (GUILayout.Button("발사체 아이콘 (별)"))
        {
            GenerateStarIcon();
        }

        if (GUILayout.Button("이동속도 아이콘 (신발)"))
        {
            GenerateBootIcon();
        }
    }

    private void GenerateAllIcons()
    {
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        GenerateSwordIcon();
        GenerateLightningIcon();
        GenerateHeartIcon();
        GenerateStarIcon();
        GenerateBootIcon();

        AssetDatabase.Refresh();
        Debug.Log("모든 아이콘 생성 완료!");
    }

    /// <summary>
    /// 검 아이콘 (공격력)
    /// </summary>
    private void GenerateSwordIcon()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color iconColor = Color.white;

        // 배경 투명
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        // 검 그리기 (간단한 형태)
        int centerX = size / 2;

        // 칼날
        for (int y = 30; y < 90; y++)
        {
            int width = 8;
            for (int x = centerX - width / 2; x < centerX + width / 2; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    texture.SetPixel(x, y, iconColor);
                }
            }
        }

        // 손잡이
        for (int y = 90; y < 110; y++)
        {
            int width = 12;
            for (int x = centerX - width / 2; x < centerX + width / 2; x++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    texture.SetPixel(x, y, new Color(0.8f, 0.6f, 0.3f)); // 갈색
                }
            }
        }

        // 가드
        for (int x = centerX - 15; x < centerX + 15; x++)
        {
            int width = 6;
            for (int y = 88; y < 88 + width; y++)
            {
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    texture.SetPixel(x, y, new Color(0.7f, 0.7f, 0.7f));
                }
            }
        }

        texture.Apply();
        SaveIconTexture(texture, "icon_damage.png");
        Debug.Log("공격력 아이콘 생성: icon_damage.png");
    }

    /// <summary>
    /// 번개 아이콘 (공격속도)
    /// </summary>
    private void GenerateLightningIcon()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color iconColor = new Color(1f, 1f, 0.3f); // 노란색

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        // 번개 모양 그리기
        int[] points = new int[]
        {
            70, 30,  // 상단
            60, 60,
            70, 60,
            50, 100, // 하단
            60, 70,
            50, 70,
            70, 30   // 다시 상단으로
        };

        for (int i = 0; i < points.Length - 2; i += 2)
        {
            DrawLine(texture, points[i], points[i + 1], points[i + 2], points[i + 3], iconColor, 8);
        }

        texture.Apply();
        SaveIconTexture(texture, "icon_attackspeed.png");
        Debug.Log("공격속도 아이콘 생성: icon_attackspeed.png");
    }

    /// <summary>
    /// 하트 아이콘 (체력)
    /// </summary>
    private void GenerateHeartIcon()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color iconColor = new Color(1f, 0.2f, 0.3f); // 빨간색

        int centerX = size / 2;
        int centerY = size / 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // 하트 모양 수식 (간단한 버전)
                float dx = (x - centerX) / 30f;
                float dy = (centerY - y) / 30f;

                float heart = Mathf.Pow(dx * dx + dy * dy - 1, 3) - dx * dx * dy * dy * dy;

                if (heart < 0 && y < centerY + 20)
                {
                    texture.SetPixel(x, y, iconColor);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        SaveIconTexture(texture, "icon_health.png");
        Debug.Log("체력 아이콘 생성: icon_health.png");
    }

    /// <summary>
    /// 별 아이콘 (발사체)
    /// </summary>
    private void GenerateStarIcon()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color iconColor = new Color(1f, 0.9f, 0.3f); // 금색

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        // 5각 별
        Vector2 center = new Vector2(size / 2, size / 2);
        float outerRadius = 40f;
        float innerRadius = 20f;
        int points = 5;

        Vector2[] starPoints = new Vector2[points * 2];

        for (int i = 0; i < points * 2; i++)
        {
            float angle = (i * Mathf.PI / points) - Mathf.PI / 2;
            float radius = (i % 2 == 0) ? outerRadius : innerRadius;

            starPoints[i] = center + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius
            );
        }

        // 별 채우기
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (IsPointInPolygon(new Vector2(x, y), starPoints))
                {
                    texture.SetPixel(x, y, iconColor);
                }
            }
        }

        texture.Apply();
        SaveIconTexture(texture, "icon_projectile.png");
        Debug.Log("발사체 아이콘 생성: icon_projectile.png");
    }

    /// <summary>
    /// 신발 아이콘 (이동속도)
    /// </summary>
    private void GenerateBootIcon()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color iconColor = new Color(0.3f, 0.7f, 1f); // 파란색

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        // 간단한 신발 형태
        // 앞부분
        DrawFilledCircle(texture, 80, 70, 20, iconColor);

        // 뒷부분
        DrawFilledRectangle(texture, 40, 60, 50, 25, iconColor);

        // 밑창
        DrawFilledRectangle(texture, 35, 85, 60, 8, new Color(0.2f, 0.2f, 0.2f));

        texture.Apply();
        SaveIconTexture(texture, "icon_speed.png");
        Debug.Log("이동속도 아이콘 생성: icon_speed.png");
    }

    // === 유틸리티 함수들 ===

    private void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawCircle(texture, x0, y0, thickness / 2, color);

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radius * radius)
                {
                    int px = centerX + x;
                    int py = centerY + y;
                    if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }
    }

    private void DrawFilledCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        DrawCircle(texture, centerX, centerY, radius, color);
    }

    private void DrawFilledRectangle(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int py = y; py < y + height; py++)
        {
            for (int px = x; px < x + width; px++)
            {
                if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                {
                    texture.SetPixel(px, py, color);
                }
            }
        }
    }

    private bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
    {
        bool inside = false;
        int j = polygon.Length - 1;

        for (int i = 0; i < polygon.Length; i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
            j = i;
        }

        return inside;
    }

    private void SaveIconTexture(Texture2D texture, string filename)
    {
        byte[] bytes = texture.EncodeToPNG();
        string path = Path.Combine(savePath, filename);

        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}