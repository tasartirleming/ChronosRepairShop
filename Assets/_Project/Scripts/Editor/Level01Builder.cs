using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ChronosRepairShop.EditorTools.BuilderUtil;

namespace ChronosRepairShop.EditorTools
{
    /// <summary>Menu: Chronos > Build Level 01. Gear chain tutorial: bridge the ball's energy to the cyan gear.</summary>
    public static class Level01Builder
    {
        [MenuItem("Chronos/Build Level 01 (Egypt)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Chronos", "Önce Play modundan çıkın (■ tuşu), sonra bu menüyü çalıştırın.", "Tamam");
                return;
            }

            LevelKit.PrepareAssets();

            var level = LoadOrCreate<LevelData>($"{Root}/ScriptableObjects/Levels/Level_Egypt_01.asset");
            level.id = "egypt_01";
            level.displayName = "Kum Saati I";
            level.sceneName = "Level_Egypt_01";
            level.parts = new List<PartAllotment>
            {
                new PartAllotment { part = LevelKit.Part("Gear_Small"), count = 2 },
                new PartAllotment { part = LevelKit.Part("Mirror"), count = 1 },
            };
            level.placementTimeLimit = 60f;
            level.runTimeLimit = 30f;
            level.rules = new EraRules();
            level.storyFragment = "Kum saatinin ilk çarkı yeniden dönüyor. Nil kıyısında zaman, uzun bir uykudan sonra derin bir nefes aldı.";
            EditorUtility.SetDirty(level);
            LevelKit.RegisterLevel(level);

            var kit = LevelKit.BeginScene();
            LevelKit.AddFloor(kit, new Vector2(0f, -4.9f), 6.8f, -8f);
            var g0 = LevelKit.AddRequiredGear(kit, new Vector2(1.5f, -2f), 0.8f);
            var mech = LevelKit.AddGateAndMechanism(kit, out var gate, g0);
            LevelKit.AddZone(kit, "PlacementZone", new Vector2(0.8f, 0.5f), new Vector2(4.4f, 4f));
            LevelKit.AddHintGhost(kit.GearAmber, "HintGearA", new Vector2(0.5f, 0f), 1.2f, 0f, 20f);
            LevelKit.AddHintGhost(kit.GearAmber, "HintGearB", new Vector2(1.5f, -0.5f), 1.2f, 0f, -20f);
            var spawn = LevelKit.AddBallSpawn(kit, new Vector2(0.2f, 5.2f));
            LevelKit.Finish(kit, level, spawn, mech, gate);

            Debug.Log("Chronos: Level 01 built (scene Level_Egypt_01).");
        }
    }
}
