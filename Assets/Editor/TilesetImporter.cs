// TilesetImporter: configura automaticamente los PNG de Assets/Art/Tilesets/ al importarlos
// y genera los Rule Tile (.asset) de los autotiles blob de 47 tiles.
//
//  - Spritesheets (terreno/, plataformas/, ruinas/, decoracion/):
//      Sprite Mode Multiple, corte en grid de 128x128 respetando 4 px de padding (2 px de borde
//      extruido por lado), PPU 128, Point, sin compresion, Mesh Type Full Rect, sin mipmaps.
//      Si existe un .json con el mismo nombre del PNG, se usan sus nombres y posiciones.
//  - Fondos (fondos/): Sprite Mode Single, Wrap U = Repeat (parallax horizontal).
//  - Rule Tiles: se crean en <carpeta del sheet>/RuleTiles/ a partir de los "sets" del .json.
//      Requieren el paquete 2D Tilemap Extras (se usa por reflexion: el script compila sin el).
//
// Menu: Tools > Tilesets > Reimportar tilesets / Generar Rule Tiles
// Requiere el paquete com.unity.2d.sprite (ya agregado en Packages/manifest.json).
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilesetImporter : AssetPostprocessor
{
    public const string Root = "Assets/Art/Tilesets/";
    const int PixelsPerUnit = 128;

    // Sube este numero si cambias la configuracion: Unity reimporta los PNG afectados.
    public override uint GetVersion() { return 2; }

    // ---------- datos del .json que acompana a cada spritesheet ----------
    [Serializable] public class SheetInfo { public int tileSize = 128, padding = 4, extrude = 2, columns, rows; public TileDef[] tiles; public SetDef[] sets; }
    [Serializable] public class TileDef { public int index, col, row, mask = -1; public string name, set; }
    // kind: "blob" (47 tiles, 3x3 vecinos), "row" (vecinos izq/der: corta, inicio, medio, fin),
    //       "column" (vecinos arriba/abajo: suelta, arriba, medio, abajo), "random" (sin vecinos)
    [Serializable] public class SetDef { public string name, kind, collider, single, start, mid, end; public string[] fullVariants; }

    static string Norm(string p) { return p.Replace('\\', '/'); }
    static bool IsTilesetPng(string p) { p = Norm(p); return p.StartsWith(Root, StringComparison.OrdinalIgnoreCase) && p.EndsWith(".png", StringComparison.OrdinalIgnoreCase); }
    static bool IsBackground(string p) { return Norm(p).IndexOf("/fondos/", StringComparison.OrdinalIgnoreCase) >= 0; }

    // ---------- importacion ----------
    void OnPreprocessTexture()
    {
        if (!IsTilesetPng(assetPath)) return;
        var ti = (TextureImporter)assetImporter;
        bool bg = IsBackground(assetPath);

        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = bg ? SpriteImportMode.Single : SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = PixelsPerUnit;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.sRGBTexture = true;
        ti.isReadable = false;
        ti.anisoLevel = 0;
        ti.maxTextureSize = 8192;
        ti.wrapModeU = bg ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        ti.wrapModeV = TextureWrapMode.Clamp;

        var ps = ti.GetDefaultPlatformTextureSettings();
        ps.maxTextureSize = 8192; ps.textureCompression = TextureImporterCompression.Uncompressed; ps.crunchedCompression = false;
        ti.SetPlatformTextureSettings(ps);

        var s = new TextureImporterSettings();
        ti.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spriteExtrude = 0;
        s.spriteGenerateFallbackPhysicsShape = false;
        if (bg) { s.spriteAlignment = (int)SpriteAlignment.Center; s.spritePivot = new Vector2(0.5f, 0.5f); }
        ti.SetTextureSettings(s);

        if (!bg) Slice(ti, assetPath);
    }

    static void Slice(TextureImporter ti, string path)
    {
        var info = ReadInfo(path);
        if (info == null) { Debug.LogWarning("[TilesetImporter] No se pudo leer el tamano de " + path); return; }
        int h = ReadPngSize(path).y, cell = info.tileSize + info.padding;

        var factory = new SpriteDataProviderFactories(); factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();

        // Se conserva el spriteID de los sprites existentes para no romper referencias (Rule Tiles, escenas).
        var old = new Dictionary<string, SpriteRect>();
        foreach (var r in dp.GetSpriteRects()) old[r.name] = r;

        var rects = new List<SpriteRect>();
        foreach (var t in info.tiles)
        {
            SpriteRect sr;
            if (!old.TryGetValue(t.name, out sr)) sr = new SpriteRect { spriteID = GUID.Generate() };
            sr.name = t.name;
            // Unity cuenta Y desde abajo
            sr.rect = new Rect(t.col * cell + info.extrude, h - (t.row * cell + info.extrude) - info.tileSize, info.tileSize, info.tileSize);
            sr.alignment = SpriteAlignment.Center;
            sr.pivot = new Vector2(0.5f, 0.5f);
            sr.border = Vector4.zero;
            rects.Add(sr);
        }
        dp.SetSpriteRects(rects.ToArray());
        var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null) names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        dp.Apply();
    }

    static SheetInfo ReadInfo(string pngPath)
    {
        string json = Path.ChangeExtension(pngPath, ".json");
        if (File.Exists(json))
        {
            var info = JsonUtility.FromJson<SheetInfo>(File.ReadAllText(json));
            if (info != null && info.tiles != null && info.tiles.Length > 0) return info;
        }
        // Sin .json: grid completo con los valores por defecto (128 px, padding 4, borde extruido 2)
        var size = ReadPngSize(pngPath); if (size.x <= 0) return null;
        var d = new SheetInfo(); int cell = d.tileSize + d.padding;
        d.columns = Mathf.Max(1, (size.x + d.padding / 2) / cell); d.rows = Mathf.Max(1, (size.y + d.padding / 2) / cell);
        string baseName = Path.GetFileNameWithoutExtension(pngPath); var list = new List<TileDef>();
        for (int r = 0; r < d.rows; r++) for (int c = 0; c < d.columns; c++)
            list.Add(new TileDef { index = r * d.columns + c, col = c, row = r, name = baseName + "_" + (r * d.columns + c), set = baseName });
        d.tiles = list.ToArray(); d.sets = new SetDef[0];
        return d;
    }

    static Vector2Int ReadPngSize(string path)
    {
        try
        {
            using (var f = File.OpenRead(path))
            {
                var b = new byte[24]; if (f.Read(b, 0, 24) < 24) return Vector2Int.zero;
                int w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
                int h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
                return new Vector2Int(w, h);
            }
        }
        catch { return Vector2Int.zero; }
    }

    // ---------- Rule Tiles ----------
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        var sheets = imported.Where(p => IsTilesetPng(p) && !IsBackground(p)).ToList();
        if (sheets.Count == 0) return;
        EditorApplication.delayCall += () => { foreach (var p in sheets) BuildRuleTiles(p); };
    }

    [MenuItem("Tools/Tilesets/Reimportar tilesets")]
    static void ReimportAll()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }

    [MenuItem("Tools/Tilesets/Generar Rule Tiles")]
    static void BuildAllRuleTiles()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root.TrimEnd('/') }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (IsTilesetPng(p) && !IsBackground(p)) BuildRuleTiles(p);
        }
    }

    static Type FindRuleTileType()
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = a.GetType("UnityEngine.RuleTile", false) ?? a.GetType("RuleTile", false);
            if (t != null && typeof(TileBase).IsAssignableFrom(t)) return t;
        }
        return null;
    }

    // Vecinos en el orden de bits del .json: N=1 NE=2 E=4 SE=8 S=16 SW=32 W=64 NW=128 (Y hacia arriba en Unity)
    static readonly Vector3Int[] NeighborPos = {
        new Vector3Int(0, 1, 0), new Vector3Int(1, 1, 0), new Vector3Int(1, 0, 0), new Vector3Int(1, -1, 0),
        new Vector3Int(0, -1, 0), new Vector3Int(-1, -1, 0), new Vector3Int(-1, 0, 0), new Vector3Int(-1, 1, 0) };
    const int This = 1, NotThis = 2;   // RuleTile.TilingRuleOutput.Neighbor

    public static void BuildRuleTiles(string pngPath)
    {
        var info = ReadInfo(pngPath);
        if (info == null || info.sets == null || info.sets.Length == 0) return;
        var rtType = FindRuleTileType();
        if (rtType == null) { Debug.LogWarning("[TilesetImporter] Instala '2D Tilemap Extras' (Package Manager) para generar los Rule Tile. Luego: Tools > Tilesets > Generar Rule Tiles."); return; }
        var ruleType = rtType.GetNestedType("TilingRule");

        var sprites = new Dictionary<string, Sprite>();
        foreach (var s in AssetDatabase.LoadAllAssetsAtPath(pngPath).OfType<Sprite>()) sprites[s.name] = s;
        if (sprites.Count == 0) return;

        string dir = Norm(Path.GetDirectoryName(pngPath)) + "/RuleTiles";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(Norm(Path.GetDirectoryName(pngPath)), "RuleTiles");

        foreach (var set in info.sets)
        {
            var collider = set.collider == "Grid" ? Tile.ColliderType.Grid : (set.collider == "Sprite" ? Tile.ColliderType.Sprite : Tile.ColliderType.None);
            var variants = (set.fullVariants ?? new string[0]).Where(sprites.ContainsKey).Select(n => sprites[n]).ToArray();
            string assetPath = dir + "/" + set.name + ".asset";
            var rt = AssetDatabase.LoadAssetAtPath(assetPath, rtType);
            bool isNew = rt == null;
            if (isNew) rt = ScriptableObject.CreateInstance(rtType);

            SetField(rt, "m_DefaultColliderType", collider);
            var rules = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(ruleType));
            int id = 0;
            Sprite def = variants.Length > 0 ? variants[0] : null;

            if (set.kind == "blob")
            {
                foreach (var t in info.tiles.Where(t => t.set == set.name && t.mask >= 0))
                {
                    Sprite sp; if (!sprites.TryGetValue(t.name, out sp)) continue;
                    bool rnd = t.mask == 255 && variants.Length > 1;
                    List<Vector3Int> pos; List<int> nb; BlobNeighbors(t.mask, out pos, out nb);
                    rules.Add(NewRule(ruleType, id++, rnd ? variants : new[] { sp }, collider, rnd, pos, nb));
                }
            }
            else if (set.kind == "row" || set.kind == "column")
            {
                bool row = set.kind == "row";
                var a = row ? new Vector3Int(-1, 0, 0) : new Vector3Int(0, 1, 0);    // izquierda / arriba
                var b = row ? new Vector3Int(1, 0, 0) : new Vector3Int(0, -1, 0);    // derecha / abajo
                string[] names = { set.single, set.start, set.mid, set.end };
                int[] na = { NotThis, NotThis, This, This }, nbv = { NotThis, This, This, NotThis };
                for (int k = 0; k < 4; k++)
                {
                    Sprite sp; if (string.IsNullOrEmpty(names[k]) || !sprites.TryGetValue(names[k], out sp)) continue;
                    if (k == 0 || def == null) def = sp;
                    rules.Add(NewRule(ruleType, id++, new[] { sp }, collider, false, new List<Vector3Int> { a, b }, new List<int> { na[k], nbv[k] }));
                }
            }
            else if (variants.Length > 0)   // "random": sin vecinos, salida aleatoria
                rules.Add(NewRule(ruleType, id++, variants, collider, variants.Length > 1, new List<Vector3Int>(), new List<int>()));

            SetField(rt, "m_DefaultSprite", def);
            SetField(rt, "m_TilingRules", rules);
            if (isNew) AssetDatabase.CreateAsset(rt, assetPath); else EditorUtility.SetDirty(rt);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[TilesetImporter] Rule Tiles actualizados en " + dir);
    }

    static void BlobNeighbors(int mask, out List<Vector3Int> pos, out List<int> nb)
    {
        pos = new List<Vector3Int>(); nb = new List<int>();
        for (int i = 0; i < 8; i++)
        {
            if ((i & 1) == 1)
            {   // la diagonal solo importa si sus dos lados ortogonales son del mismo terreno
                int a = 1 << ((i + 7) % 8), b = 1 << ((i + 1) % 8);
                if ((mask & a) == 0 || (mask & b) == 0) continue;
            }
            pos.Add(NeighborPos[i]); nb.Add((mask & (1 << i)) != 0 ? This : NotThis);
        }
    }

    static object NewRule(Type ruleType, int id, Sprite[] spr, Tile.ColliderType col, bool random, List<Vector3Int> pos, List<int> nb)
    {
        var r = Activator.CreateInstance(ruleType);
        SetField(r, "m_Id", id);
        SetField(r, "m_Sprites", spr);
        SetField(r, "m_ColliderType", col);
        SetEnum(r, "m_Output", random ? 1 : 0);        // OutputSprite: Single = 0, Random = 1
        SetEnum(r, "m_RuleTransform", 0);              // Transform: Fixed
        SetEnum(r, "m_RandomTransform", 0);
        if (random) SetField(r, "m_PerlinScale", 0.5f);
        SetField(r, "m_NeighborPositions", pos);
        SetField(r, "m_Neighbors", nb);
        return r;
    }

    static void SetField(object o, string name, object v)
    {
        var f = o.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null) f.SetValue(o, v); else Debug.LogWarning("[TilesetImporter] Campo no encontrado en " + o.GetType().Name + ": " + name);
    }
    static void SetEnum(object o, string name, int v)
    {
        var f = o.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (f != null && f.FieldType.IsEnum) f.SetValue(o, Enum.ToObject(f.FieldType, v));
    }
}
