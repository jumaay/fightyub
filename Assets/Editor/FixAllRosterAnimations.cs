using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Fightyub.Editor
{
    public static class FixAllRosterAnimations
    {
        private static readonly Dictionary<string, string> SpecialFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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

        [MenuItem("Fightyub/Fix All Roster Animations")]
        public static void FixRosterInScene()
        {
            var fmm = UnityEngine.Object.FindFirstObjectByType<FightMatchManager>();
            if (fmm == null || fmm.roster == null)
            {
                Debug.LogError("FightMatchManager not found in current scene!");
                return;
            }

            Undo.RecordObject(fmm, "Fix All Roster Animations");

            foreach (var entry in fmm.roster)
            {
                if (entry == null || string.IsNullOrEmpty(entry.characterName) || entry.anims == null) continue;
                FixCharacterAnimations(entry.characterName, entry.anims);
            }

            if (fmm.player1 != null && fmm.player1.anims != null)
            {
                Undo.RecordObject(fmm.player1, "Fix Player 1 Animations");
                FixCharacterAnimations(fmm.player1.fighterName, fmm.player1.anims);
                EditorUtility.SetDirty(fmm.player1);
            }

            if (fmm.player2 != null && fmm.player2.anims != null)
            {
                Undo.RecordObject(fmm.player2, "Fix Player 2 Animations");
                FixCharacterAnimations(fmm.player2.fighterName, fmm.player2.anims);
                EditorUtility.SetDirty(fmm.player2);
            }

            EditorUtility.SetDirty(fmm);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(fmm.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=green>Successfully fixed animations (including Special & Hitstun) for all 11 roster characters and saved scene!</color>");
        }

        public static void FixCharacterAnimations(string charName, FighterAnimationSet anims)
        {
            if (string.IsNullOrEmpty(charName) || anims == null) return;

            string prefix = charName;
            string tilesetPath = $"Assets/Chars/{charName}_02_Tileset.png";
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(tilesetPath);
            if (allAssets == null || allAssets.Length == 0) return;

            var spriteDict = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (var a in allAssets)
            {
                if (a is Sprite s) spriteDict[s.name] = s;
            }

            Sprite[] GetSprites(string rowStr, int[] cols)
            {
                var list = new List<Sprite>();
                foreach (int c in cols)
                {
                    string key = $"{prefix}_{rowStr}_C{c:D2}";
                    if (spriteDict.TryGetValue(key, out var s)) list.Add(s);
                }
                return list.ToArray();
            }

            Sprite[] GetRange(string rowStr, int startCol, int endCol)
            {
                var list = new List<Sprite>();
                for (int c = startCol; c <= endCol; c++)
                {
                    string key = $"{prefix}_{rowStr}_C{c:D2}";
                    if (spriteDict.TryGetValue(key, out var s)) list.Add(s);
                }
                return list.ToArray();
            }

            // Common base rows R01..R05 across all characters (overridden below where specialized)
            anims.idle = GetRange("R01", 0, 7);
            anims.crouch = GetRange("R02", 0, 7);
            anims.jump = GetRange("R03", 0, 7);
            anims.standPunch = GetRange("R04", 0, 7);
            anims.standKick = GetRange("R05", 0, 7);

            string n = charName.ToLowerInvariant();

            if (n.Contains("luli"))
            {
                // Luli (11 rows): R05 C02 is a punch so exclude from StandKick; R06 CrouchPunch(0,1,6,7)/CrouchKick(0,2,3,4,5,7); R07 AirPunch(1,2,3,4,6)/AirKick(1,2,5,6); R11 Run (skip C05/C06 edge artifact)
                anims.standKick = GetSprites("R05", new int[] { 0, 1, 3, 4, 5, 6, 7 });
                anims.crouchPunch = GetSprites("R06", new int[] { 0, 1, 6, 7 });
                anims.crouchKick = GetSprites("R06", new int[] { 0, 2, 3, 4, 5, 7 });
                anims.airPunch = GetSprites("R07", new int[] { 1, 2, 3, 4, 6 });
                anims.airKick = GetSprites("R07", new int[] { 1, 2, 5, 6 });
                anims.standBlock = GetRange("R08", 0, 3);
                anims.airBlock = GetRange("R08", 4, 7);
                anims.crouchBlock = GetRange("R09", 0, 7);
                anims.walk = GetRange("R10", 0, 7);
                anims.run = GetRange("R11", 0, 7);
            }
            else if (n.Contains("jere7em"))
            {
                // Jere7em (12 rows): clean airborne jump (0..4); stand punch (0,1,5,6); stand kick (0,2,3,6); crouch kick (0..3,5..7); crouch punch (0..4); air punch (0,1,2,4,6); air kick (0,1,3,5,6); stand block (1,2,3,5)
                anims.jump = GetSprites("R03", new int[] { 0, 1, 2, 3, 4 });
                anims.standPunch = GetSprites("R04", new int[] { 0, 1, 5, 6 });
                anims.standKick = GetSprites("R04", new int[] { 0, 2, 3, 6 });
                anims.crouchKick = GetSprites("R05", new int[] { 0, 1, 2, 3, 5, 6, 7 });
                anims.crouchPunch = GetRange("R06", 0, 4);
                anims.airPunch = GetSprites("R07", new int[] { 0, 1, 2, 4, 6 });
                anims.airKick = GetSprites("R07", new int[] { 0, 1, 3, 5, 6 });
                anims.standBlock = GetSprites("R08", new int[] { 1, 2, 3, 5 });
                anims.crouchBlock = GetRange("R09", 0, 7);
                anims.airBlock = GetRange("R10", 0, 7);
                anims.walk = GetRange("R11", 0, 7);
                anims.run = GetRange("R12", 0, 7);
            }
            else if (n.Contains("jumaay"))
            {
                // Jumaay (11 rows): R05 C03 is a low crouch pose so exclude C03 from StandKick; R06 Air, R07 StandBlock, R08 Crouch, R09 AirBlock
                anims.standKick = GetSprites("R05", new int[] { 0, 1, 2, 4, 5, 6, 7 });
                anims.airPunch = GetRange("R06", 0, 2);
                anims.airKick = GetRange("R06", 3, 6);
                anims.standBlock = GetRange("R07", 0, 7);
                anims.crouchPunch = GetSprites("R08", new int[] { 0, 1, 3 });
                anims.crouchKick = GetSprites("R08", new int[] { 0, 2, 3 });
                anims.crouchBlock = GetRange("R08", 4, 7);
                anims.airBlock = GetRange("R09", 0, 7);
                anims.walk = GetRange("R10", 0, 7);
                anims.run = GetRange("R11", 0, 7);
            }
            else if (n.Contains("lalo"))
            {
                // Lalo (11 rows): R05 skip C04 edge artifact; R06 CrouchPunch(0,1,2,5,6)/CrouchKick(0,1,3,4,6); R07 AirPunch(0,1,4,5,6)/AirKick(0,1,2,3,6)
                anims.standKick = GetSprites("R05", new int[] { 0, 1, 2, 3, 5, 6, 7 });
                anims.crouchPunch = GetSprites("R06", new int[] { 0, 1, 2, 5, 6 });
                anims.crouchKick = GetSprites("R06", new int[] { 0, 1, 3, 4, 6 });
                anims.airPunch = GetSprites("R07", new int[] { 0, 1, 4, 5, 6 });
                anims.airKick = GetSprites("R07", new int[] { 0, 1, 2, 3, 6 });
                anims.standBlock = GetRange("R08", 0, 7);
                anims.crouchBlock = GetRange("R09", 0, 3);
                anims.airBlock = GetRange("R09", 4, 7);
                anims.walk = GetRange("R10", 0, 7);
                anims.run = GetRange("R11", 0, 7);
            }
            else if (n.Contains("pipi"))
            {
                // Pipi (12 rows): R06 CrouchPunch(0,1,4,5,6)/CrouchKick(0,2,3,6,7); R07 AirPunch(0..4,7); R08 AirKick(0..4,7)
                anims.crouchPunch = GetSprites("R06", new int[] { 0, 1, 4, 5, 6 });
                anims.crouchKick = GetSprites("R06", new int[] { 0, 2, 3, 6, 7 });
                anims.airPunch = GetSprites("R07", new int[] { 0, 1, 2, 3, 4, 7 });
                anims.airKick = GetSprites("R08", new int[] { 0, 1, 2, 3, 4, 7 });
                anims.standBlock = GetRange("R09", 0, 7);
                anims.crouchBlock = GetRange("R10", 0, 3);
                anims.airBlock = GetRange("R10", 4, 7);
                anims.walk = GetRange("R11", 0, 7);
                anims.run = GetRange("R12", 0, 7);
            }
            else
            {
                // 13-row characters: Irma, Lila, Perla, Pachi, Tatoon, Momo
                anims.crouchPunch = GetRange("R06", 0, 3);
                anims.crouchKick = GetRange("R06", 4, 7);
                anims.airPunch = GetRange("R07", 0, 7);
                anims.airKick = GetRange("R08", 0, 7);
                anims.standBlock = GetRange("R09", 0, 7);
                anims.crouchBlock = GetRange("R10", 0, 7);
                anims.airBlock = GetRange("R11", 0, 7);
                anims.walk = GetRange("R12", 0, 7);
                anims.run = GetRange("R13", 0, 7);

                if (n.Contains("irma"))
                {
                    // Irma (13 rows): clean jump airborne frames (0..5); stand punch (0..4); crouch punch (0,1,4); crouch kick (0,2,3,4); air punch (1,2,3,5); air kick (1,2,3,5)
                    anims.standPunch = GetRange("R04", 0, 4);
                    anims.jump = GetSprites("R03", new int[] { 0, 1, 2, 3, 4, 5 });
                    anims.crouchPunch = GetSprites("R06", new int[] { 0, 1, 4 });
                    anims.crouchKick = GetSprites("R06", new int[] { 0, 2, 3, 4 });
                    anims.airPunch = GetSprites("R07", new int[] { 1, 2, 3, 5 });
                    anims.airKick = GetSprites("R08", new int[] { 1, 2, 3, 5 });
                }
                else if (n.Contains("pachi"))
                {
                    // R03 C05 is a flat crouch pose; R06 C02 is low lunge attack
                    anims.jump = GetSprites("R03", new int[] { 0, 1, 2, 3, 4, 6, 7 });
                    anims.crouchPunch = GetSprites("R06", new int[] { 0, 1, 2, 3 });
                    anims.crouchKick = GetSprites("R06", new int[] { 0, 2, 3, 4 });
                }
                else if (n.Contains("perla"))
                {
                    // R06 C01/C02/C04 are front-paw crouch punches; C03 is hind-leg crouch kick
                    anims.crouchPunch = GetSprites("R06", new int[] { 0, 1, 2, 4, 5 });
                    anims.crouchKick = GetSprites("R06", new int[] { 0, 2, 3, 5, 6 });
                }
                else if (n.Contains("tatoon"))
                {
                    // R06 C03 is crouch punch (fist) and C02 is crouch kick (foot); R10 C02/C03 are crouch kicks; R08 C04 is low crouch
                    anims.crouchPunch = GetSprites("R06", new int[] { 0, 1, 3, 4 });
                    anims.crouchKick = GetSprites("R06", new int[] { 0, 1, 2, 4 });
                    anims.crouchBlock = GetSprites("R10", new int[] { 0, 1, 4, 5, 6, 7 });
                    anims.airKick = GetSprites("R08", new int[] { 0, 1, 2, 3, 5, 6, 7 });
                }
                else if (n.Contains("lila"))
                {
                    // R08 is jumping front-paw swipe (airPunch), R07 is hind-leg kick (airKick); R13 C03..C07 is clean run cycle
                    anims.airPunch = GetRange("R08", 0, 5);
                    anims.airKick = GetRange("R07", 0, 5);
                    anims.run = GetSprites("R13", new int[] { 3, 4, 5, 6, 7 });
                }
                else if (n.Contains("momo"))
                {
                    // R06 C01/C02 are crouch attacks; R08 skip C04/C05 slice-edge noise; R09 use 4-legged guard frames C00, C05..C07
                    anims.crouchPunch = GetSprites("R06", new int[] { 0, 1, 2, 3 });
                    anims.crouchKick = GetSprites("R06", new int[] { 0, 1, 2, 4 });
                    anims.airKick = GetSprites("R08", new int[] { 0, 1, 2, 3, 6, 7 });
                    anims.standBlock = GetSprites("R09", new int[] { 0, 5, 6, 7 });
                }
            }

            // Refresh Special Attack (10 frames) and Damage/Hurt (4 frames) on FighterAnimationSet
            anims.hurt = null;
            anims.specialAttack = null;
            FighterController.EnsureDamageAnimation(charName, anims);
            FighterController.EnsureSpecialAttackAnimation(charName, anims);
        }
    }
}
