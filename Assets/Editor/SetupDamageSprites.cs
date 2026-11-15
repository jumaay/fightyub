using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fightyub.Editor
{
    public static class SetupDamageSprites
    {
        [System.Serializable]
        public class SpriteEntry
        {
            public string name;
            public int x, y, width, height;
            public float pivot_x, pivot_y;
        }

        [System.Serializable]
        public class SpriteList
        {
            public SpriteEntry[] items;
        }

        [MenuItem("Fightyub/Configure Damage Sprites")]
        public static void Configure()
        {
            string texturePath = "Assets/Chars/Fightyub_Todos_Personajes_Dano_Recuperacion_16bit.png";
            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError("TextureImporter not found at " + texturePath);
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "damage_sprites_manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("Manifest not found: " + manifestPath);
                return;
            }

            string json = File.ReadAllText(manifestPath);
            string wrapped = "{\"items\":" + json + "}";
            var list = JsonUtility.FromJson<SpriteList>(wrapped);

            var metaList = new List<SpriteMetaData>();
            foreach (var s in list.items)
            {
                var meta = new SpriteMetaData
                {
                    name = s.name,
                    rect = new Rect(s.x, s.y, s.width, s.height),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(s.pivot_x, s.pivot_y)
                };
                metaList.Add(meta);
            }

            importer.spritesheet = metaList.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            Debug.Log($"Successfully configured {metaList.Count} damage/recovery sprites on {texturePath}!");
        }
    }
}
