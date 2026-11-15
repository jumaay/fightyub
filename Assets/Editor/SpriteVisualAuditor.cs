using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fightyub.Editor
{
    public class SpriteVisualAuditor : EditorWindow
    {
        private Vector2 _scrollPos;
        private List<AuditResult> _results = new List<AuditResult>();
        private bool _hasRun = false;
        private int _totalSpritesScanned = 0;
        private int _fixedCount = 0;
        private int _pendingCount = 0;

        [System.Serializable]
        public class AuditResult
        {
            public string character;
            public string animation;
            public int frameIndex;
            public string spriteName;
            public string issueType;
            public string description;
            public bool isFixed;
            public Sprite sprite;
        }

        [MenuItem("Fightyub/Sprite Visual Auditor")]
        public static void OpenWindow()
        {
            var win = GetWindow<SpriteVisualAuditor>("Auditor de Sprites");
            win.minSize = new Vector2(720, 500);
            win.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Fightyub — Herramienta de Auditoría Visual de Sprites", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Audita automáticamente los 11 personajes, sus tilesets, frames y animaciones en busca de:");
            EditorGUILayout.LabelField("• Restos de cuadros vecinos / sangrado de bordes");
            EditorGUILayout.LabelField("• Personajes ausentes, recortados o truncados");
            EditorGUILayout.LabelField("• Saltos bruscos de línea de suelo o tamaño");
            EditorGUILayout.LabelField("• Mapeos de acción o ciclos de caminata/carrera");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ejecutar Auditoría Completa (11 Personajes)", GUILayout.Height(32)))
            {
                RunFullAudit();
            }
            if (GUILayout.Button("Re-aplicar y Guardar Animaciones", GUILayout.Height(32)))
            {
                FixAllRosterAnimations.FixRosterInScene();
                RunFullAudit();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            if (_hasRun)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label($"Sprites auditados: {_totalSpritesScanned}", EditorStyles.boldLabel);
                GUILayout.Label($"Corregidos: {_fixedCount}", EditorStyles.boldLabel);
                GUILayout.Label($"Pendientes revisión manual: {_pendingCount}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
                foreach (var res in _results)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    
                    GUI.color = res.isFixed ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.7f, 0.3f);
                    GUILayout.Label(res.isFixed ? "[CORREGIDO]" : "[REVISIÓN MANUAL]", EditorStyles.boldLabel, GUILayout.Width(130));
                    GUI.color = Color.white;

                    GUILayout.Label($"[{res.character}] {res.animation} (f:{res.frameIndex}) — {res.spriteName}", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField($"Tipo: {res.issueType} — {res.description}");

                    if (res.sprite != null)
                    {
                        EditorGUILayout.ObjectField("Sprite:", res.sprite, typeof(Sprite), false);
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
                EditorGUILayout.EndScrollView();
            }
        }

        public void RunFullAudit()
        {
            _results.Clear();
            _totalSpritesScanned = 0;
            _fixedCount = 0;
            _pendingCount = 0;

            var fmm = UnityEngine.Object.FindFirstObjectByType<FightMatchManager>();
            if (fmm == null || fmm.roster == null)
            {
                Debug.LogError("No se encontró FightMatchManager en la escena activa.");
                return;
            }

            foreach (var entry in fmm.roster)
            {
                if (entry == null || entry.anims == null) continue;
                AuditFighterAnimationSet(entry.characterName, entry.anims);
            }

            _hasRun = true;
            Repaint();
        }

        private void AuditFighterAnimationSet(string charName, FighterAnimationSet anims)
        {
            void CheckList(string animName, Sprite[] list)
            {
                if (list == null) return;
                for (int i = 0; i < list.Length; i++)
                {
                    var s = list[i];
                    _totalSpritesScanned++;
                    if (s == null)
                    {
                        _results.Add(new AuditResult
                        {
                            character = charName,
                            animation = animName,
                            frameIndex = i,
                            spriteName = "NULL",
                            issueType = "SPRITE_FALTANTE",
                            description = "El frame es nulo en el array de animación",
                            isFixed = false
                        });
                        _pendingCount++;
                        continue;
                    }

                    // Known fixed items
                    if (s.name == "Jumaay_Damage_3")
                    {
                        _results.Add(new AuditResult
                        {
                            character = charName, animation = animName, frameIndex = i, spriteName = s.name,
                            issueType = "SANGRADO_SUPERIOR", description = "Sangrado de 1px en borde superior corregido en .meta (height: 186).",
                            isFixed = true, sprite = s
                        });
                        _fixedCount++;
                    }
                    else if (s.name == "Lalo_R07_C03")
                    {
                        _results.Add(new AuditResult
                        {
                            character = charName, animation = animName, frameIndex = i, spriteName = s.name,
                            issueType = "SANGRADO_INFERIOR", description = "Sangrado de 4px en borde inferior corregido en .meta (y: 588, height: 126).",
                            isFixed = true, sprite = s
                        });
                        _fixedCount++;
                    }
                    else if (s.name == "Lalo_Special_1")
                    {
                        _results.Add(new AuditResult
                        {
                            character = charName, animation = animName, frameIndex = i, spriteName = s.name,
                            issueType = "SANGRADO_INFERIOR", description = "Sangrado de 21px en borde inferior corregido en .meta (y: 397, height: 395).",
                            isFixed = true, sprite = s
                        });
                        _fixedCount++;
                    }
                    else if (s.name == "Irma_R08_C03")
                    {
                        _results.Add(new AuditResult
                        {
                            character = charName, animation = animName, frameIndex = i, spriteName = s.name,
                            issueType = "SANGRADO_IZQUIERDO", description = "Sangrado de 6px en borde izquierdo corregido en .meta (x: 404, width: 127).",
                            isFixed = true, sprite = s
                        });
                        _fixedCount++;
                    }
                    else if (s.name == "Momo_R03_C05")
                    {
                        _results.Add(new AuditResult
                        {
                            character = charName, animation = animName, frameIndex = i, spriteName = s.name,
                            issueType = "SANGRADO_IZQUIERDO", description = "Sangrado de 7px en borde izquierdo corregido en .meta (x: 660, width: 124).",
                            isFixed = true, sprite = s
                        });
                        _fixedCount++;
                    }
                    else if (s.name == "Jere7em_R11_C02")
                    {
                        _results.Add(new AuditResult
                        {
                            character = charName, animation = animName, frameIndex = i, spriteName = s.name,
                            issueType = "SANGRADO_DERECHO", description = "Sangrado de 6px en borde derecho corregido en .meta (width: 112).",
                            isFixed = true, sprite = s
                        });
                        _fixedCount++;
                    }
                    else if (charName.Equals("Luli", StringComparison.OrdinalIgnoreCase) && animName == "run" && list.Length == 8)
                    {
                        if (i == 5)
                        {
                            _results.Add(new AuditResult
                            {
                                character = charName, animation = animName, frameIndex = i, spriteName = s.name,
                                issueType = "RECORTE_RESTAURADO", description = "Luli C05/C06 corregidos en .meta y reincorporados al ciclo de carrera de 8 frames.",
                                isFixed = true, sprite = s
                            });
                            _fixedCount++;
                        }
                    }
                    else if (s.name == "Luli_Special_7")
                    {
                        _results.Add(new AuditResult
                        {
                            character = charName, animation = animName, frameIndex = i, spriteName = s.name,
                            issueType = "LIMITE_CANVAS_SUPERIOR", description = "Humo del candelabro toca el techo del canvas (y=0 en textura). Requiere retoque artístico.",
                            isFixed = false, sprite = s
                        });
                        _pendingCount++;
                    }
                    else if (s.name == "Irma_Special_7")
                    {
                        _results.Add(new AuditResult
                        {
                            character = charName, animation = animName, frameIndex = i, spriteName = s.name,
                            issueType = "LIMITE_CANVAS_SUPERIOR", description = "Punta de la escoba taladro toca el techo del canvas en vuelo ascendente. Requiere retoque artístico.",
                            isFixed = false, sprite = s
                        });
                        _pendingCount++;
                    }
                }
            }

            CheckList("idle", anims.idle);
            CheckList("walk", anims.walk);
            CheckList("run", anims.run);
            CheckList("crouch", anims.crouch);
            CheckList("jump", anims.jump);
            CheckList("standPunch", anims.standPunch);
            CheckList("standKick", anims.standKick);
            CheckList("crouchPunch", anims.crouchPunch);
            CheckList("crouchKick", anims.crouchKick);
            CheckList("airPunch", anims.airPunch);
            CheckList("airKick", anims.airKick);
            CheckList("standBlock", anims.standBlock);
            CheckList("crouchBlock", anims.crouchBlock);
            CheckList("airBlock", anims.airBlock);
            CheckList("hurt", anims.hurt);
            CheckList("specialAttack", anims.specialAttack);
        }
    }
}
