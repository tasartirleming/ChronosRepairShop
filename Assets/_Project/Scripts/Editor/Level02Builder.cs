using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static ChronosRepairShop.EditorTools.BuilderUtil;

namespace ChronosRepairShop.EditorTools
{
    /// <summary>
    /// Menu: Chronos > Build Level 02. Spring puzzle: the cyan gear is far above the floor, so the ball has to be launched into it.
    /// Tuned with a ballistic simulation: launch speed 12, spring tilted clockwise by 15 or 30 degrees works; straight up or tilted left does not.
    /// </summary>
    public static class Level02Builder
    {
        [MenuItem("Chronos/Build Level 02 (Egypt)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Chronos", "Önce Play modundan çıkın (■ tuşu), sonra bu menüyü çalıştırın.", "Tamam");
                return;
            }

            LevelKit.PrepareAssets();

            var level = LoadOrCreate<LevelData>($"{Root}/ScriptableObjects/Levels/Level_Egypt_02.asset");
            level.id = "egypt_02";
            level.displayName = "Kum Saati II";
            level.sceneName = "Level_Egypt_02";
            level.parts = new List<PartAllotment> { new PartAllotment { part = LevelKit.Part("Spring"), count = 2 } };
            level.placementTimeLimit = 60f;
            level.runTimeLimit = 30f;
            level.rules = new EraRules();
            level.storyFragment = "Kum, yerçekimine meydan okuyarak yukarı doğru akmaya başladı. Saatin tepesindeki çark bir kez daha uyandı.";
            EditorUtility.SetDirty(level);
            LevelKit.RegisterLevel(level);

            var kit = LevelKit.BeginScene();
            LevelKit.AddFloor(kit, new Vector2(0f, -4.9f), 6.8f, -8f);
            var g0 = LevelKit.AddRequiredGear(kit, new Vector2(1.8f, 0.6f), 1.0f);
            var mech = LevelKit.AddGateAndMechanism(kit, out var gate, g0);
            LevelKit.AddZone(kit, "PlacementZone", new Vector2(-1.6f, -3.9f), new Vector2(2.6f, 2.0f));
            LevelKit.AddHintGhost(ArtFactory.Get(ArtFactory.Spring), "HintSpring", new Vector2(-1.75f, -4.2f), 1.2f, -22f, 0f);
            var spawn = LevelKit.AddBallSpawn(kit, new Vector2(-1.75f, 5.2f));
            LevelKit.Finish(kit, level, spawn, mech, gate);

            Debug.Log("Chronos: Level 02 built (scene Level_Egypt_02).");
        }
    }
}
