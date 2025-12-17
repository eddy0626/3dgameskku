using UnityEngine;
using UnityEditor;

public static class WaveConfigSetup
{
    [MenuItem("Tools/Wave/Setup WaveConfig_01")]
    public static void SetupWaveConfig01()
    {
        // Load WaveConfig
        var waveConfig = AssetDatabase.LoadAssetAtPath<WaveConfig>("Assets/09.Data/Waves/WaveConfig_01.asset");
        if (waveConfig == null)
        {
            Debug.LogError("WaveConfig_01 not found!");
            return;
        }

        // Load EnemyData
        var enemyBasic = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Prefabs/Enemy/EnemyData_Basic.asset");
        if (enemyBasic == null)
        {
            Debug.LogError("EnemyData_Basic not found!");
            return;
        }

        // Setup spawn groups
        waveConfig.spawnGroups = new WaveSpawnGroup[]
        {
            new WaveSpawnGroup
            {
                enemyType = enemyBasic,
                count = 5,
                startDelay = 0f,
                spawnInterval = 2f,
                groupDelay = 3f,
                spawnAllAtOnce = false,
                pattern = SpawnPattern.Random,
                specificSpawnPointIndex = -1
            },
            new WaveSpawnGroup
            {
                enemyType = enemyBasic,
                count = 5,
                startDelay = 0f,
                spawnInterval = 1.5f,
                groupDelay = 0f,
                spawnAllAtOnce = false,
                pattern = SpawnPattern.Random,
                specificSpawnPointIndex = -1
            }
        };

        // Mark dirty and save
        EditorUtility.SetDirty(waveConfig);
        AssetDatabase.SaveAssets();

        Debug.Log($"WaveConfig_01 setup complete! Total enemies: {waveConfig.TotalEnemyCount}");
    }

    [MenuItem("Tools/Wave/Create All WaveConfigs")]
    public static void CreateAllWaveConfigs()
    {
        var enemyBasic = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/03.Prefabs/Enemy/EnemyData_Basic.asset");
        if (enemyBasic == null)
        {
            Debug.LogError("EnemyData_Basic not found!");
            return;
        }

        // Wave 1 - Tutorial
        CreateWaveConfig(1, "첫 번째 습격", "약한 적들이 출현합니다.", enemyBasic, 10, 60, 100, 1f, 1f);

        // Wave 2 - Getting Serious
        CreateWaveConfig(2, "본격적인 전투", "적들이 더 많이 몰려옵니다.", enemyBasic, 15, 75, 150, 1.1f, 1.05f);

        // Wave 3 - Challenge
        CreateWaveConfig(3, "강화된 적", "적들이 강해졌습니다!", enemyBasic, 20, 90, 200, 1.25f, 1.1f);

        // Wave 4 - Swarm
        CreateWaveConfig(4, "대규모 습격", "수많은 적들이 밀려옵니다!", enemyBasic, 30, 120, 300, 1.4f, 1.15f);

        // Wave 5 - Boss Wave
        var wave5 = CreateWaveConfig(5, "보스 웨이브", "강력한 보스가 나타납니다!", enemyBasic, 15, 150, 500, 1.5f, 1.2f);
        if (wave5 != null)
        {
            wave5.hasBoss = true;
            wave5.bossEnemy = enemyBasic;
            wave5.bossSpawnTime = 60f;
            wave5.bossCount = 1;
            wave5.gemReward = 5;
            EditorUtility.SetDirty(wave5);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("All WaveConfigs created successfully!");
    }

    private static WaveConfig CreateWaveConfig(int waveNum, string name, string desc, EnemyData enemy, 
        int enemyCount, float duration, int gold, float healthMult, float damageMult)
    {
        string path = $"Assets/09.Data/Waves/WaveConfig_{waveNum:D2}.asset";
        
        var existing = AssetDatabase.LoadAssetAtPath<WaveConfig>(path);
        WaveConfig config;
        
        if (existing != null)
        {
            config = existing;
        }
        else
        {
            config = ScriptableObject.CreateInstance<WaveConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        config.waveNumber = waveNum;
        config.waveName = name;
        config.description = desc;
        config.preparationTime = 5f + waveNum;
        config.waveDuration = duration;
        config.goldReward = gold;
        config.experienceReward = 25 + waveNum * 15;
        config.healthMultiplier = healthMult;
        config.damageMultiplier = damageMult;
        config.spawnRateMultiplier = 1f + (waveNum - 1) * 0.05f;

        int groupCount = Mathf.Min(waveNum, 3);
        int enemiesPerGroup = enemyCount / groupCount;
        
        config.spawnGroups = new WaveSpawnGroup[groupCount];
        for (int i = 0; i < groupCount; i++)
        {
            config.spawnGroups[i] = new WaveSpawnGroup
            {
                enemyType = enemy,
                count = enemiesPerGroup + (i == groupCount - 1 ? enemyCount % groupCount : 0),
                startDelay = i * 5f,
                spawnInterval = Mathf.Max(0.5f, 2f - waveNum * 0.2f),
                groupDelay = 3f,
                spawnAllAtOnce = false,
                pattern = SpawnPattern.Random,
                specificSpawnPointIndex = -1
            };
        }

        EditorUtility.SetDirty(config);
        Debug.Log($"WaveConfig_{waveNum:D2} created/updated: {name} ({enemyCount} enemies)");
        
        return config;
    }
}
