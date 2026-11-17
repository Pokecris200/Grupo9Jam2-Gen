// CharacterSpriteImporter: configura automaticamente las tiras de animacion de personajes en Assets/Art/Personaje/.
//  - Sprite Mode Multiple, corte en frames de 128x192 (una fila, de izquierda a derecha)
//  - Pivote abajo al centro (los pies del personaje)
//  - PPU 128, Filter Point, Compression None, Mesh Type Full Rect, sin mipmaps
// Los sprites se llaman <archivo>_<n> (p. ej. walk_0 ... walk_5). No toca nada fuera de Assets/Art/Personaje/.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class CharacterSpriteImporter : AssetPostprocessor
{
    public const string Root = "Assets/Art/Personaje/";
    const int FrameW = 128, FrameH = 192, PixelsPerUnit = 128;

    public override uint GetVersion() { return 1; }

    static bool IsCharacterPng(string p)
    {
        p = p.Replace('\\', '/');
        return p.StartsWith(Root, StringComparison.OrdinalIgnoreCase) && p.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
    }

    void OnPreprocessTexture()
    {
        if (!IsCharacterPng(assetPath)) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = PixelsPerUnit;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.maxTextureSize = 4096;
        var ps = ti.GetDefaultPlatformTextureSettings();
        ps.maxTextureSize = 4096; ps.textureCompression = TextureImporterCompression.Uncompressed; ps.crunchedCompression = false;
        ti.SetPlatformTextureSettings(ps);
        var s = new TextureImporterSettings(); ti.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect; s.spriteExtrude = 0; s.spriteGenerateFallbackPhysicsShape = false;
        ti.SetTextureSettings(s);

        var size = ReadPngSize(assetPath); if (size.x <= 0) return;
        int cols = Mathf.Max(1, size.x / FrameW), rows = Mathf.Max(1, size.y / FrameH);
        string baseName = Path.GetFileNameWithoutExtension(assetPath);

        var factory = new SpriteDataProviderFactories(); factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti); dp.InitSpriteEditorDataProvider();
        var old = new Dictionary<string, SpriteRect>(); foreach (var r in dp.GetSpriteRects()) old[r.name] = r;
        var rects = new List<SpriteRect>(); int n = 0;
        for (int row = 0; row < rows; row++) for (int col = 0; col < cols; col++, n++)
            {
                string name = baseName + "_" + n; SpriteRect sr;
                if (!old.TryGetValue(name, out sr)) sr = new SpriteRect { spriteID = GUID.Generate() };   // conserva IDs al reimportar
                sr.name = name;
                sr.rect = new Rect(col * FrameW, size.y - (row + 1) * FrameH, FrameW, FrameH);   // Unity cuenta Y desde abajo
                sr.alignment = SpriteAlignment.BottomCenter; sr.pivot = new Vector2(0.5f, 0f); sr.border = Vector4.zero;
                rects.Add(sr);
            }
        dp.SetSpriteRects(rects.ToArray());
        var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        dp.Apply();
    }

    static Vector2Int ReadPngSize(string path)
    {
        try
        {
            using (var f = File.OpenRead(path))
            {
                var b = new byte[24]; if (f.Read(b, 0, 24) < 24) return Vector2Int.zero;
                return new Vector2Int((b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19], (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23]);
            }
        }
        catch { return Vector2Int.zero; }
    }

    [MenuItem("Tools/Personaje/Reimportar sprites del personaje")]
    static void ReimportAll()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }
}
