using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Unity Editor에서 UI 리소스를 자동 생성하는 도구
/// 메뉴: Tools > Generate UI Resources
/// </summary>
public class UIResourceGenerator : EditorWindow
{
    private string savePath = "Assets/UI/Generated";

    [MenuItem("Tools/Generate UI Resources")]
    public static void ShowWindow()
    {
        GetWindow<UIResourceGenerator>("UI Resource Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("UI 리소스 자동 생성", EditorStyles.boldLabel);
        GUILayout.Space(10);

        savePath = EditorGUILayout.TextField("저장 경로:", savePath);
        GUILayout.Space(10);

        if (GUILayout.Button("모든 리소스 생성", GUILayout.Height(40)))
        {
            GenerateAllResources();
        }

        GUILayout.Space(20);
        GUILayout.Label("개별 생성:", EditorStyles.boldLabel);

        if (GUILayout.Button("카드 배경 생성"))
        {
            GenerateCardBackground();
        }

        if (GUILayout.Button("아이콘 배경 생성"))
        {
            GenerateIconBackground();
        }

        if (GUILayout.Button("버튼 배경 생성"))
        {
            GenerateButtonBackground();
        }

        if (GUILayout.Button("레어도 배지 생성"))
        {
            GenerateRarityBadges();
        }
    }

    private void GenerateAllResources()
    {
        // 폴더 생성
        if (!AssetDatabase.IsValidFolder(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        GenerateCardBackground();
        GenerateIconBackground();
        GenerateButtonBackground();
        GenerateRarityBadges();

        AssetDatabase.Refresh();
        Debug.Log("모든 UI 리소스 생성 완료!");
    }

    /// <summary>
    /// 카드 배경 생성 (둥근 사각형 + 그라데이션)
    /// </summary>
    private void GenerateCardBackground()
    {
        int width = 512;
        int height = 512;
        int cornerRadius = 40;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color topColor = HexToColor("#2a2a3e");
        Color bottomColor = HexToColor("#1f1f2e");

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 모서리 체크
                bool isInCorner = IsInCorner(x, y, width, height, cornerRadius);

                if (isInCorner)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    // 그라데이션
                    float t = (float)y / height;
                    Color gradColor = Color.Lerp(bottomColor, topColor, t);

                    // 약간의 노이즈 추가
                    float noise = Random.Range(-0.02f, 0.02f);
                    gradColor.r += noise;
                    gradColor.g += noise;
                    gradColor.b += noise;

                    texture.SetPixel(x, y, gradColor);
                }
            }
        }

        texture.Apply();
        SaveTexture(texture, "card_background.png");
        Debug.Log("카드 배경 생성 완료: card_background.png");
    }

    /// <summary>
    /// 아이콘 배경 생성 (원형 + 그라데이션)
    /// </summary>
    private void GenerateIconBackground()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color topColor = HexToColor("#3a3a4e");
        Color bottomColor = HexToColor("#2a2a3e");

        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);

                if (dist <= radius)
                {
                    // 그라데이션
                    float t = (float)y / size;
                    Color gradColor = Color.Lerp(bottomColor, topColor, t);

                    // 가장자리 어둡게 (Inner Shadow 효과)
                    float edgeFactor = 1f - (dist / radius);
                    float innerShadow = Mathf.Pow(edgeFactor, 0.5f);
                    gradColor *= Mathf.Lerp(0.7f, 1f, innerShadow);

                    texture.SetPixel(x, y, gradColor);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        SaveTexture(texture, "icon_background.png");
        Debug.Log("아이콘 배경 생성 완료: icon_background.png");
    }

    /// <summary>
    /// 버튼 배경 생성 (둥근 사각형 + 골드 그라데이션)
    /// </summary>
    private void GenerateButtonBackground()
    {
        int width = 512;
        int height = 128;
        int cornerRadius = 20;

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Color topColor = HexToColor("#ffd700");
        Color bottomColor = HexToColor("#ffed4e");

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isInCorner = IsInCorner(x, y, width, height, cornerRadius);

                if (isInCorner)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float t = (float)y / height;
                    Color gradColor = Color.Lerp(bottomColor, topColor, t);
                    texture.SetPixel(x, y, gradColor);
                }
            }
        }

        texture.Apply();
        SaveTexture(texture, "button_background.png");
        Debug.Log("버튼 배경 생성 완료: button_background.png");
    }

    /// <summary>
    /// 레어도 배지 배경 생성
    /// </summary>
    private void GenerateRarityBadges()
    {
        int width = 256;
        int height = 64;
        int cornerRadius = 32;

        // Common
        GenerateBadge(width, height, cornerRadius, HexToColor("#9e9e9e"), HexToColor("#757575"), "badge_common.png");

        // Rare
        GenerateBadge(width, height, cornerRadius, HexToColor("#4fc3f7"), HexToColor("#2196f3"), "badge_rare.png");

        // Epic
        GenerateBadge(width, height, cornerRadius, HexToColor("#ba68c8"), HexToColor("#9c27b0"), "badge_epic.png");

        // Legendary
        GenerateBadge(width, height, cornerRadius, HexToColor("#ffa726"), HexToColor("#ff9800"), "badge_legendary.png");

        Debug.Log("레어도 배지 생성 완료!");
    }

    private void GenerateBadge(int width, int height, int cornerRadius, Color topColor, Color bottomColor, string filename)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isInCorner = IsInCorner(x, y, width, height, cornerRadius);

                if (isInCorner)
                {
                    texture.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float t = (float)y / height;
                    Color gradColor = Color.Lerp(bottomColor, topColor, t);
                    texture.SetPixel(x, y, gradColor);
                }
            }
        }

        texture.Apply();
        SaveTexture(texture, filename);
    }

    /// <summary>
    /// 둥근 모서리 판정
    /// </summary>
    private bool IsInCorner(int x, int y, int width, int height, int cornerRadius)
    {
        // 좌상단
        if (x < cornerRadius && y < cornerRadius)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius));
            return dist > cornerRadius;
        }
        // 우상단
        if (x > width - cornerRadius - 1 && y < cornerRadius)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, cornerRadius));
            return dist > cornerRadius;
        }
        // 좌하단
        if (x < cornerRadius && y > height - cornerRadius - 1)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, height - cornerRadius - 1));
            return dist > cornerRadius;
        }
        // 우하단
        if (x > width - cornerRadius - 1 && y > height - cornerRadius - 1)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - cornerRadius - 1, height - cornerRadius - 1));
            return dist > cornerRadius;
        }

        return false;
    }

    /// <summary>
    /// Hex 컬러를 Unity Color로 변환
    /// </summary>
    private Color HexToColor(string hex)
    {
        hex = hex.Replace("#", "");

        byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

        return new Color32(r, g, b, 255);
    }

    /// <summary>
    /// 텍스처 저장
    /// </summary>
    private void SaveTexture(Texture2D texture, string filename)
    {
        byte[] bytes = texture.EncodeToPNG();
        string path = Path.Combine(savePath, filename);

        // 폴더가 없으면 생성
        string directory = Path.GetDirectoryName(path);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);

        // Sprite로 설정
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