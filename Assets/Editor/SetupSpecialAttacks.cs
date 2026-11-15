using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fightyub.Editor
{
    public static class SetupSpecialAttacks
    {
        [System.Serializable]
        public class FrameEntry
        {
            public string name;
            public int x;
            public int y;
            public int width;
            public int height;
            public float pivot_x;
            public float pivot_y;
        }

        [System.Serializable]
        public class CharacterSpecialData
        {
            public float ppu;
            public FrameEntry[] frames;
        }

        public static readonly string[] CharacterNames = new string[]
        {
            "Jumaay", "Pipi", "Irma", "Lila", "Perla", "Pachi", "Jere7em", "Tatoon", "Lalo", "Luli", "Momo"
        };

        private static readonly Dictionary<string, string> FileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Jumaay", "Jumaay_AtaqueEspecial_Guitarra_16bit.png" },
            { "Pipi", "Pipi_AtaqueEspecial_Varita_Burbujas_16bit.png" },
            { "Irma", "Irma_AtaqueEspecial_EscobaTaladro_16bit.png" },
            { "Lila", "Lila_AtaqueEspecial_CacaVoladora_16bit.png" },
            { "Perla", "Perla_AtaqueEspecial_LadridoEnsordecedor_16bit.png" },
            { "Pachi", "Pachi_AtaqueEspecial_RosquillaJuguete_16bit.png" },
            { "Jere7em", "Jere7em_AtaqueEspecial_Monitor_3Golpes_16bit.png" },
            { "Tatoon", "Tatoon_AtaqueEspecial_PizzeraChef_16bit.png" },
            { "Lalo", "Lalo_AtaqueEspecial_Estetoscopio_16bit.png" },
            { "Luli", "Luli_AtaqueEspecial_CandelabroMickey_16bit.png" },
            { "Momo", "Momo_AtaqueEspecial_SoguitaJuguete_3Golpes_16bit.png" }
        };

        [MenuItem("Fightyub/Process Special Attack Sprites")]
        public static void ProcessAllSpecialAttackSprites()
        {
            string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "specials_manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError("specials_manifest.json not found!");
                return;
            }

            string json = File.ReadAllText(manifestPath);

            foreach (var charName in CharacterNames)
            {
                if (!FileMap.TryGetValue(charName, out var fileName)) continue;
                string relPath = "Assets/Chars/" + fileName;

                var importer = AssetImporter.GetAtPath(relPath) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning("No importer for " + relPath);
                    continue;
                }

                // Extract character block from json
                string searchKey = $"\"{charName}\":";
                int idx = json.IndexOf(searchKey, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;

                int openBrace = json.IndexOf('{', idx);
                // find matching closing brace for this char
                int depth = 0;
                int closeBrace = openBrace;
                for (int i = openBrace; i < json.Length; i++)
                {
                    if (json[i] == '{') depth++;
                    else if (json[i] == '}')
                    {
                        depth--;
                        if (depth == 0) { closeBrace = i; break; }
                    }
                }

                string charJson = json.Substring(openBrace, closeBrace - openBrace + 1);
                var charData = JsonUtility.FromJson<CharacterSpecialData>(charJson);
                if (charData == null || charData.frames == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.spritePixelsPerUnit = Mathf.RoundToInt(charData.ppu);
                importer.maxTextureSize = 4096;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Point;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                var metaList = new List<SpriteMetaData>();
                foreach (var f in charData.frames)
                {
                    var meta = new SpriteMetaData
                    {
                        name = f.name,
                        rect = new Rect(f.x, f.y, f.width, f.height),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(f.pivot_x, f.pivot_y)
                    };
                    metaList.Add(meta);
                }

                importer.spritesheet = metaList.ToArray();
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                Debug.Log($"[SetupSpecialAttacks] Configured {charName} ({metaList.Count} frames, PPU={importer.spritePixelsPerUnit})");
            }

            AssetDatabase.Refresh();
            Debug.Log("<color=green>Finished processing all 11 Special Attack textures with normalized PPU and pivots!</color>");
        }
    }
}
