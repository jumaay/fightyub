using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Fightyub.Editor
{
    public static class SetupCharacterAudio
    {
        [MenuItem("Fightyub/Setup Character Audio (Jump & Special)")]
        public static void Setup()
        {
            AssetDatabase.Refresh();

            var sfxMgr = UnityEngine.Object.FindFirstObjectByType<SoundEffectsManager>();
            if (sfxMgr == null)
            {
                var fmmObj = UnityEngine.Object.FindFirstObjectByType<FightMatchManager>();
                if (fmmObj != null)
                {
                    sfxMgr = fmmObj.GetComponent<SoundEffectsManager>();
                    if (sfxMgr == null) sfxMgr = fmmObj.gameObject.AddComponent<SoundEffectsManager>();
                }
            }

            AudioClip luliJump = LoadClip("luli_salto");
            AudioClip luliSpec = LoadClip("luli_ataque_especial");
            AudioClip pipiJump = LoadClip("pipi_salto");
            AudioClip pipiSpec = LoadClip("pipi_ataque_especial");
            AudioClip jumaayJump = LoadClip("jumaay_salto");
            AudioClip jumaaySpec = LoadClip("jumaay_ataque_especial");
            AudioClip tatoonJump = LoadClip("tatoon_salto");
            AudioClip tatoonSpec = LoadClip("tatoon_ataque_especial");
            AudioClip jere7emJump = LoadClip("jere7em_salto");
            AudioClip jere7emSpec = LoadClip("jere7em_ataque_especial");

            AudioClip uiMove = LoadSfxClip("sfx_ui_move");
            AudioClip uiConfirm = LoadSfxClip("sfx_ui_confirm");
            AudioClip attackWhiff = LoadSfxClip("sfx_attack_whiff");

            if (sfxMgr != null)
            {
                Undo.RecordObject(sfxMgr, "Assign Audio Clips to SoundEffectsManager");
                sfxMgr.EnsureAudioSource();
                sfxMgr.luliJumpClip = luliJump;
                sfxMgr.luliSpecialClip = luliSpec;
                sfxMgr.pipiJumpClip = pipiJump;
                sfxMgr.pipiSpecialClip = pipiSpec;
                sfxMgr.jumaayJumpClip = jumaayJump;
                sfxMgr.jumaaySpecialClip = jumaaySpec;
                sfxMgr.tatoonJumpClip = tatoonJump;
                sfxMgr.tatoonSpecialClip = tatoonSpec;
                sfxMgr.jere7emJumpClip = jere7emJump;
                sfxMgr.jere7emSpecialClip = jere7emSpec;
                sfxMgr.uiMoveClip = uiMove;
                sfxMgr.uiConfirmClip = uiConfirm;
                sfxMgr.attackWhiffClip = attackWhiff;
                sfxMgr.LoadClipsIfMissing();
                EditorUtility.SetDirty(sfxMgr);
            }

            var fmm = UnityEngine.Object.FindFirstObjectByType<FightMatchManager>();
            if (fmm != null && fmm.roster != null)
            {
                Undo.RecordObject(fmm, "Assign Character Audio Clips to Roster");
                foreach (var entry in fmm.roster)
                {
                    if (entry == null || entry.anims == null) continue;
                    string n = (entry.characterName ?? "").Trim();

                    // Clear first so other characters never receive these 10 clips
                    entry.anims.jumpAudioClip = null;
                    entry.anims.specialAttackAudioClip = null;

                    if (n.Equals("Luli", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.anims.jumpAudioClip = luliJump;
                        entry.anims.specialAttackAudioClip = luliSpec;
                    }
                    else if (n.Equals("Pipi", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.anims.jumpAudioClip = pipiJump;
                        entry.anims.specialAttackAudioClip = pipiSpec;
                    }
                    else if (n.Equals("Jumaay", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.anims.jumpAudioClip = jumaayJump;
                        entry.anims.specialAttackAudioClip = jumaaySpec;
                    }
                    else if (n.Equals("Tatoon", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.anims.jumpAudioClip = tatoonJump;
                        entry.anims.specialAttackAudioClip = tatoonSpec;
                    }
                    else if (n.Equals("Jere7em", StringComparison.OrdinalIgnoreCase))
                    {
                        entry.anims.jumpAudioClip = jere7emJump;
                        entry.anims.specialAttackAudioClip = jere7emSpec;
                    }
                }

                if (fmm.player1 != null && sfxMgr != null)
                {
                    Undo.RecordObject(fmm.player1, "Assign P1 Audio Clips");
                    fmm.player1.jumpClip = sfxMgr.GetCharacterJumpClip(fmm.player1.fighterName);
                    fmm.player1.specialAttackClip = sfxMgr.GetCharacterSpecialClip(fmm.player1.fighterName);
                    EditorUtility.SetDirty(fmm.player1);
                }

                if (fmm.player2 != null && sfxMgr != null)
                {
                    Undo.RecordObject(fmm.player2, "Assign P2 Audio Clips");
                    fmm.player2.jumpClip = sfxMgr.GetCharacterJumpClip(fmm.player2.fighterName);
                    fmm.player2.specialAttackClip = sfxMgr.GetCharacterSpecialClip(fmm.player2.fighterName);
                    EditorUtility.SetDirty(fmm.player2);
                }

                EditorUtility.SetDirty(fmm);
                EditorSceneManager.MarkSceneDirty(fmm.gameObject.scene);
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log("<color=green>[SetupCharacterAudio] Assigned UI SFX, Whiff SFX, and Character Jump/Special audio clips successfully!</color>");
        }

        private static AudioClip LoadSfxClip(string baseName)
        {
            string p = $"Assets/Audio/SFX/{baseName}.wav";
            var importer = AssetImporter.GetAtPath(p) as AudioImporter;
            if (importer != null)
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = AudioCompressionFormat.PCM;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(p);
        }

        private static AudioClip LoadClip(string baseName)
        {
            string[] paths = new string[]
            {
                $"Assets/Audio/{baseName}_trimmed.ogg",
                $"Assets/Audio/{baseName}.wav"
            };

            foreach (var p in paths)
            {
                var importer = AssetImporter.GetAtPath(p) as AudioImporter;
                if (importer != null)
                {
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.PCM;
                    importer.defaultSampleSettings = settings;
                    importer.forceToMono = true;
                    importer.SaveAndReimport();
                }
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{baseName}_trimmed.ogg");
            if (clip == null) clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{baseName}.wav");
            return clip;
        }
    }
}
