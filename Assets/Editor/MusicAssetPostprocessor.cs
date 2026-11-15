#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Fightyub.Editor
{
    /// <summary>
    /// Configura automáticamente cualquier pista de música (actual o futura)
    /// importada en Assets/Audio/Music o con prefijo bgm_ para streaming y loop continuo.
    /// </summary>
    public class MusicAssetPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (string.IsNullOrEmpty(assetPath)) return;

            string normalizedPath = assetPath.Replace('\\', '/');
            string fileName = System.IO.Path.GetFileName(normalizedPath);

            bool isMusicTrack = normalizedPath.IndexOf("/Music/", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                               fileName.StartsWith("bgm_", System.StringComparison.OrdinalIgnoreCase);

            if (isMusicTrack)
            {
                var audioImporter = (AudioImporter)assetImporter;
                if (audioImporter != null)
                {
                    audioImporter.loadInBackground = false;

                    var settings = audioImporter.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.CompressedInMemory;
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    settings.quality = 0.85f;
                    audioImporter.defaultSampleSettings = settings;

                    // Configuraciones específicas por plataforma si existen
                    var standaloneSettings = audioImporter.GetOverrideSampleSettings("Standalone");
                    standaloneSettings.loadType = AudioClipLoadType.CompressedInMemory;
                    standaloneSettings.compressionFormat = AudioCompressionFormat.Vorbis;
                    standaloneSettings.quality = 0.85f;
                    audioImporter.SetOverrideSampleSettings("Standalone", standaloneSettings);

                    Debug.Log($"[MusicAssetPostprocessor] Pista de música configurada para loop continuo (CompressedInMemory): {fileName}");
                }
            }
        }
    }
}
#endif
