using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility to create sample ScriptableObject data for the survival game
/// </summary>
public class SampleDataCreator : EditorWindow
{
    [MenuItem("Tools/Survival Game/Create All Sample Data")]
    public static void CreateAllSampleData()
    {
        CreateSquadMemberData();
        CreateResourceData();
        CreateUpgradeData();
        CreateWaveConfigData();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[SampleDataCreator] All sample data created successfully!");
    }

    [MenuItem("Tools/Survival Game/Create Squad Member Data")]
    public static void CreateSquadMemberData()
    {
        string basePath = "Assets/04.Data/Squad/";
        
        // Rifleman - 기본 분대원
        var rifleman = ScriptableObject.CreateInstance<SquadMemberData>();
        rifleman.memberName = "Rifleman";
        rifleman.description = "Basic squad member with balanced stats";
        rifleman.maxHealth = 100f;
        rifleman.damage = 15f;
        rifleman.attackRange = 10f;
        rifleman.attackCooldown = 0.8f;
        rifleman.moveSpeed = 4f;
        rifleman.rotationSpeed = 8f;
        rifleman.followDistance = 3f;
        rifleman.maxFollowDistance = 15f;
        rifleman.detectionRange = 12f;
        rifleman.attackType = AttackType.Ranged;
        rifleman.meleeRange = 2f;
        rifleman.projectileSpeed = 30f;
        rifleman.recruitCost = 100;
        rifleman.unlockWave = 1;
        rifleman.damagePerLevel = 3f;
        rifleman.healthPerLevel = 15f;
        rifleman.speedPerLevel = 0.2f;
        rifleman.attackSpeedPerLevel = 0.05f;
        rifleman.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Squad/SquadMember_Rifleman.prefab");
        rifleman.enemyLayer = LayerMask.GetMask("Enemy");
        AssetDatabase.CreateAsset(rifleman, basePath + "SquadMember_Rifleman.asset");
        
        // Tank - 탱커
        var tank = ScriptableObject.CreateInstance<SquadMemberData>();
        tank.memberName = "Tank";
        tank.description = "Heavy soldier with high health and melee damage";
        tank.maxHealth = 250f;
        tank.damage = 25f;
        tank.attackRange = 3f;
        tank.attackCooldown = 1.2f;
        tank.moveSpeed = 3f;
        tank.rotationSpeed = 6f;
        tank.followDistance = 2f;
        tank.maxFollowDistance = 12f;
        tank.detectionRange = 8f;
        tank.attackType = AttackType.Melee;
        tank.meleeRange = 3f;
        tank.projectileSpeed = 0f;
        tank.recruitCost = 200;
        tank.unlockWave = 2;
        tank.damagePerLevel = 5f;
        tank.healthPerLevel = 30f;
        tank.speedPerLevel = 0.1f;
        tank.attackSpeedPerLevel = 0.03f;
        tank.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Squad/SquadMember_Tank.prefab");
        tank.enemyLayer = LayerMask.GetMask("Enemy");
        AssetDatabase.CreateAsset(tank, basePath + "SquadMember_Tank.asset");
        
        // Sniper - 저격수
        var sniper = ScriptableObject.CreateInstance<SquadMemberData>();
        sniper.memberName = "Sniper";
        sniper.description = "Long range specialist with high damage";
        sniper.maxHealth = 60f;
        sniper.damage = 50f;
        sniper.attackRange = 25f;
        sniper.attackCooldown = 2f;
        sniper.moveSpeed = 3.5f;
        sniper.rotationSpeed = 5f;
        sniper.followDistance = 5f;
        sniper.maxFollowDistance = 20f;
        sniper.detectionRange = 30f;
        sniper.attackType = AttackType.Ranged;
        sniper.meleeRange = 2f;
        sniper.projectileSpeed = 50f;
        sniper.recruitCost = 300;
        sniper.unlockWave = 3;
        sniper.damagePerLevel = 10f;
        sniper.healthPerLevel = 10f;
        sniper.speedPerLevel = 0.15f;
        sniper.attackSpeedPerLevel = 0.08f;
        sniper.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Squad/SquadMember_Sniper.prefab");
        sniper.enemyLayer = LayerMask.GetMask("Enemy");
        AssetDatabase.CreateAsset(sniper, basePath + "SquadMember_Sniper.asset");
        
        Debug.Log("[SampleDataCreator] Squad Member Data created!");
    }

    [MenuItem("Tools/Survival Game/Create Resource Data")]
    public static void CreateResourceData()
    {
        string basePath = "Assets/04.Data/Resource/";
        
        // Gold
        var gold = ScriptableObject.CreateInstance<ResourceData>();
        gold.resourceName = "Gold";
        gold.type = ResourceType.Gold;
        gold.baseAmount = 10;
        gold.minAmount = 5;
        gold.maxAmount = 20;
        gold.randomizeAmount = true;
        gold.magnetSpeed = 15f;
        gold.bobHeight = 0.2f;
        gold.bobSpeed = 2f;
        gold.rotateSpeed = 90f;
        gold.glowColor = new Color(1f, 0.84f, 0f);
        gold.lifetime = 30f;
        gold.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Resource/ResourceDrop_Gold.prefab");
        AssetDatabase.CreateAsset(gold, basePath + "Resource_Gold.asset");
        
        // Gem
        var gem = ScriptableObject.CreateInstance<ResourceData>();
        gem.resourceName = "Gem";
        gem.type = ResourceType.Gem;
        gem.baseAmount = 1;
        gem.minAmount = 1;
        gem.maxAmount = 3;
        gem.randomizeAmount = false;
        gem.magnetSpeed = 12f;
        gem.bobHeight = 0.3f;
        gem.bobSpeed = 3f;
        gem.rotateSpeed = 120f;
        gem.glowColor = new Color(0.5f, 0f, 1f);
        gem.lifetime = 60f;
        gem.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Resource/ResourceDrop_Gem.prefab");
        AssetDatabase.CreateAsset(gem, basePath + "Resource_Gem.asset");
        
        // Health
        var health = ScriptableObject.CreateInstance<ResourceData>();
        health.resourceName = "Health Pack";
        health.type = ResourceType.Health;
        health.baseAmount = 25;
        health.minAmount = 20;
        health.maxAmount = 50;
        health.randomizeAmount = true;
        health.magnetSpeed = 10f;
        health.bobHeight = 0.15f;
        health.bobSpeed = 1.5f;
        health.rotateSpeed = 0f;
        health.glowColor = new Color(0f, 1f, 0.3f);
        health.lifetime = 45f;
        health.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Resource/ResourceDrop_Health.prefab");
        AssetDatabase.CreateAsset(health, basePath + "Resource_Health.asset");
        
        // Experience
        var exp = ScriptableObject.CreateInstance<ResourceData>();
        exp.resourceName = "Experience";
        exp.type = ResourceType.Experience;
        exp.baseAmount = 5;
        exp.minAmount = 1;
        exp.maxAmount = 10;
        exp.randomizeAmount = true;
        exp.magnetSpeed = 20f;
        exp.bobHeight = 0.1f;
        exp.bobSpeed = 4f;
        exp.rotateSpeed = 180f;
        exp.glowColor = new Color(0f, 0.8f, 1f);
        exp.lifetime = 20f;
        exp.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Resource/ResourceDrop_Exp.prefab");
        AssetDatabase.CreateAsset(exp, basePath + "Resource_Experience.asset");
        
        Debug.Log("[SampleDataCreator] Resource Data created!");
    }

    [MenuItem("Tools/Survival Game/Create Upgrade Data")]
    public static void CreateUpgradeData()
    {
        string basePath = "Assets/04.Data/Upgrade/";

        // Damage Upgrade
        var damage = ScriptableObject.CreateInstance<UpgradeData>();
        damage.upgradeName = "Firepower";
        damage.description = "Increases damage dealt by squad members";
        damage.statType = UpgradeType.Damage;
        damage.target = UpgradeTarget.Squad;
        damage.rarity = UpgradeRarity.Common;
        damage.category = UpgradeCategory.Offensive;
        damage.maxLevel = 5;
        damage.useManualValues = true;
        damage.manualValuePerLevel = new float[] { 5f, 8f, 12f, 18f, 25f };
        damage.useManualCosts = true;
        damage.manualCostPerLevel = new int[] { 100, 200, 400, 800, 1500 };
        damage.unlockWave = 1;
        damage.upgradeColor = Color.red;
        AssetDatabase.CreateAsset(damage, basePath + "Upgrade_Damage.asset");

        // Health Upgrade
        var health = ScriptableObject.CreateInstance<UpgradeData>();
        health.upgradeName = "Vitality";
        health.description = "Increases maximum health";
        health.statType = UpgradeType.Health;
        health.target = UpgradeTarget.All;
        health.rarity = UpgradeRarity.Common;
        health.category = UpgradeCategory.Defensive;
        health.maxLevel = 5;
        health.useManualValues = true;
        health.manualValuePerLevel = new float[] { 20f, 30f, 50f, 80f, 120f };
        health.useManualCosts = true;
        health.manualCostPerLevel = new int[] { 100, 200, 400, 800, 1500 };
        health.unlockWave = 1;
        health.upgradeColor = Color.green;
        AssetDatabase.CreateAsset(health, basePath + "Upgrade_Health.asset");

        // Speed Upgrade
        var speed = ScriptableObject.CreateInstance<UpgradeData>();
        speed.upgradeName = "Agility";
        speed.description = "Increases movement speed";
        speed.statType = UpgradeType.Speed;
        speed.target = UpgradeTarget.All;
        speed.rarity = UpgradeRarity.Uncommon;
        speed.category = UpgradeCategory.Utility;
        speed.maxLevel = 3;
        speed.useManualValues = true;
        speed.manualValuePerLevel = new float[] { 0.5f, 0.8f, 1.2f };
        speed.useManualCosts = true;
        speed.manualCostPerLevel = new int[] { 150, 350, 700 };
        speed.unlockWave = 1;
        speed.upgradeColor = Color.cyan;
        AssetDatabase.CreateAsset(speed, basePath + "Upgrade_Speed.asset");

        // Attack Speed Upgrade
        var attackSpeed = ScriptableObject.CreateInstance<UpgradeData>();
        attackSpeed.upgradeName = "Rapid Fire";
        attackSpeed.description = "Increases attack speed";
        attackSpeed.statType = UpgradeType.AttackSpeed;
        attackSpeed.target = UpgradeTarget.Squad;
        attackSpeed.rarity = UpgradeRarity.Uncommon;
        attackSpeed.category = UpgradeCategory.Offensive;
        attackSpeed.maxLevel = 4;
        attackSpeed.useManualValues = true;
        attackSpeed.manualValuePerLevel = new float[] { 0.1f, 0.15f, 0.2f, 0.3f };
        attackSpeed.useManualCosts = true;
        attackSpeed.manualCostPerLevel = new int[] { 200, 400, 800, 1600 };
        attackSpeed.unlockWave = 2;
        attackSpeed.upgradeColor = Color.yellow;
        AssetDatabase.CreateAsset(attackSpeed, basePath + "Upgrade_AttackSpeed.asset");

        // Magnet Range Upgrade
        var magnet = ScriptableObject.CreateInstance<UpgradeData>();
        magnet.upgradeName = "Magnetism";
        magnet.description = "Increases resource collection range";
        magnet.statType = UpgradeType.MagnetRange;
        magnet.target = UpgradeTarget.Player;
        magnet.rarity = UpgradeRarity.Rare;
        magnet.category = UpgradeCategory.Utility;
        magnet.maxLevel = 3;
        magnet.useManualValues = true;
        magnet.manualValuePerLevel = new float[] { 2f, 4f, 8f };
        magnet.useManualCosts = true;
        magnet.manualCostPerLevel = new int[] { 300, 600, 1200 };
        magnet.unlockWave = 1;
        magnet.upgradeColor = new Color(1f, 0.5f, 0f);
        AssetDatabase.CreateAsset(magnet, basePath + "Upgrade_MagnetRange.asset");

        // Squad Size Upgrade
        var squadSize = ScriptableObject.CreateInstance<UpgradeData>();
        squadSize.upgradeName = "Reinforcements";
        squadSize.description = "Increases maximum squad size";
        squadSize.statType = UpgradeType.SquadSize;
        squadSize.target = UpgradeTarget.Squad;
        squadSize.rarity = UpgradeRarity.Epic;
        squadSize.category = UpgradeCategory.Squad;
        squadSize.maxLevel = 3;
        squadSize.useManualValues = true;
        squadSize.manualValuePerLevel = new float[] { 1f, 1f, 2f };
        squadSize.useManualCosts = true;
        squadSize.manualCostPerLevel = new int[] { 500, 1000, 2000 };
        squadSize.unlockWave = 3;
        squadSize.upgradeColor = Color.magenta;
        AssetDatabase.CreateAsset(squadSize, basePath + "Upgrade_SquadSize.asset");

        // Critical Chance Upgrade
        var critChance = ScriptableObject.CreateInstance<UpgradeData>();
        critChance.upgradeName = "Precision";
        critChance.description = "Increases critical hit chance";
        critChance.statType = UpgradeType.CriticalChance;
        critChance.target = UpgradeTarget.Squad;
        critChance.rarity = UpgradeRarity.Rare;
        critChance.category = UpgradeCategory.Offensive;
        critChance.isPercentage = true;
        critChance.maxLevel = 5;
        critChance.useManualValues = true;
        critChance.manualValuePerLevel = new float[] { 5f, 7f, 10f, 12f, 15f };
        critChance.useManualCosts = true;
        critChance.manualCostPerLevel = new int[] { 250, 500, 1000, 2000, 4000 };
        critChance.unlockWave = 2;
        critChance.upgradeColor = new Color(1f, 0.3f, 0.3f);
        AssetDatabase.CreateAsset(critChance, basePath + "Upgrade_CriticalChance.asset");

        Debug.Log("[SampleDataCreator] Upgrade Data created!");
    }

    [MenuItem("Tools/Survival Game/Create Wave Config Data")]
    public static void CreateWaveConfigData()
    {
        string basePath = "Assets/04.Data/Wave/";
        
        // 기존 EnemyData 찾기
        var basicEnemy = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/04.Data/Enemy/Enemy_Basic.asset");
        
        // Wave 1
        var wave1 = ScriptableObject.CreateInstance<WaveConfig>();
        wave1.waveNumber = 1;
        wave1.waveName = "First Wave";
        wave1.preparationTime = 5f;
        wave1.waveDuration = 60f;
        wave1.hasBoss = false;
        wave1.goldReward = 50;
        wave1.gemReward = 0;
        wave1.experienceReward = 25;
        wave1.healthMultiplier = 1f;
        wave1.damageMultiplier = 1f;
        wave1.spawnRateMultiplier = 1f;
        wave1.spawnGroups = new WaveSpawnGroup[]
        {
            new WaveSpawnGroup
            {
                enemyType = basicEnemy,
                count = 5,
                startDelay = 0f,
                spawnInterval = 2f,
                groupDelay = 3f,
                spawnAllAtOnce = false,
                pattern = SpawnPattern.Random
            }
        };
        AssetDatabase.CreateAsset(wave1, basePath + "Wave_01.asset");
        
        // Wave 2
        var wave2 = ScriptableObject.CreateInstance<WaveConfig>();
        wave2.waveNumber = 2;
        wave2.waveName = "Growing Threat";
        wave2.preparationTime = 8f;
        wave2.waveDuration = 90f;
        wave2.hasBoss = false;
        wave2.goldReward = 100;
        wave2.gemReward = 1;
        wave2.experienceReward = 50;
        wave2.healthMultiplier = 1.1f;
        wave2.damageMultiplier = 1.05f;
        wave2.spawnRateMultiplier = 1.1f;
        wave2.spawnGroups = new WaveSpawnGroup[]
        {
            new WaveSpawnGroup
            {
                enemyType = basicEnemy,
                count = 8,
                startDelay = 0f,
                spawnInterval = 1.5f,
                groupDelay = 5f,
                spawnAllAtOnce = false,
                pattern = SpawnPattern.Random
            },
            new WaveSpawnGroup
            {
                enemyType = basicEnemy,
                count = 5,
                startDelay = 0f,
                spawnInterval = 1f,
                groupDelay = 0f,
                spawnAllAtOnce = false,
                pattern = SpawnPattern.Surrounding
            }
        };
        AssetDatabase.CreateAsset(wave2, basePath + "Wave_02.asset");
        
        // Wave 3
        var wave3 = ScriptableObject.CreateInstance<WaveConfig>();
        wave3.waveNumber = 3;
        wave3.waveName = "Swarm";
        wave3.preparationTime = 10f;
        wave3.waveDuration = 120f;
        wave3.hasBoss = false;
        wave3.goldReward = 150;
        wave3.gemReward = 2;
        wave3.experienceReward = 75;
        wave3.healthMultiplier = 1.2f;
        wave3.damageMultiplier = 1.1f;
        wave3.spawnRateMultiplier = 1.2f;
        wave3.spawnGroups = new WaveSpawnGroup[]
        {
            new WaveSpawnGroup
            {
                enemyType = basicEnemy,
                count = 15,
                startDelay = 0f,
                spawnInterval = 1f,
                groupDelay = 5f,
                spawnAllAtOnce = false,
                pattern = SpawnPattern.Random
            },
            new WaveSpawnGroup
            {
                enemyType = basicEnemy,
                count = 10,
                startDelay = 0f,
                spawnInterval = 0.5f,
                groupDelay = 0f,
                spawnAllAtOnce = true,
                pattern = SpawnPattern.Surrounding
            }
        };
        AssetDatabase.CreateAsset(wave3, basePath + "Wave_03.asset");
        
        // Wave 5 - Boss Wave
        var wave5 = ScriptableObject.CreateInstance<WaveConfig>();
        wave5.waveNumber = 5;
        wave5.waveName = "Boss Battle";
        wave5.preparationTime = 15f;
        wave5.waveDuration = 180f;
        wave5.hasBoss = true;
        wave5.bossEnemy = basicEnemy; // Replace with actual boss EnemyData
        wave5.bossSpawnTime = 30f;
        wave5.bossCount = 1;
        wave5.goldReward = 500;
        wave5.gemReward = 10;
        wave5.experienceReward = 200;
        wave5.healthMultiplier = 1.5f;
        wave5.damageMultiplier = 1.3f;
        wave5.spawnRateMultiplier = 1.5f;
        wave5.spawnGroups = new WaveSpawnGroup[]
        {
            new WaveSpawnGroup
            {
                enemyType = basicEnemy,
                count = 20,
                startDelay = 0f,
                spawnInterval = 0.8f,
                groupDelay = 10f,
                spawnAllAtOnce = false,
                pattern = SpawnPattern.Random
            },
            new WaveSpawnGroup
            {
                enemyType = basicEnemy,
                count = 15,
                startDelay = 0f,
                spawnInterval = 0.5f,
                groupDelay = 0f,
                spawnAllAtOnce = false,
                pattern = SpawnPattern.Surrounding
            }
        };
        AssetDatabase.CreateAsset(wave5, basePath + "Wave_05_Boss.asset");
        
        Debug.Log("[SampleDataCreator] Wave Config Data created!");
    }

    [MenuItem("Tools/Survival Game/Create Resource Prefabs")]
    public static void CreateResourcePrefabs()
    {
        string basePath = "Assets/03.Prefabs/Resource/";
        
        // Gold Prefab
        CreateResourcePrefab("ResourceDrop_Gold", basePath, Color.yellow, PrimitiveType.Sphere, 0.3f);
        
        // Gem Prefab
        CreateResourcePrefab("ResourceDrop_Gem", basePath, new Color(0.8f, 0.2f, 1f), PrimitiveType.Cube, 0.25f);
        
        // Health Prefab
        CreateResourcePrefab("ResourceDrop_Health", basePath, Color.green, PrimitiveType.Capsule, 0.3f);
        
        // Experience Prefab
        CreateResourcePrefab("ResourceDrop_Exp", basePath, Color.cyan, PrimitiveType.Sphere, 0.2f);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[SampleDataCreator] Resource Prefabs created!");
    }

    private static void CreateResourcePrefab(string name, string path, Color color, PrimitiveType primitiveType, float scale)
    {
        GameObject obj = GameObject.CreatePrimitive(primitiveType);
        obj.name = name;
        obj.transform.localScale = Vector3.one * scale;
        
        // Remove default collider and add trigger
        Object.DestroyImmediate(obj.GetComponent<Collider>());
        SphereCollider col = obj.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.5f / scale; // Normalize radius
        
        // Add ResourceDrop component
        obj.AddComponent<ResourceDrop>();
        
        // Set material color
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.5f);
            renderer.sharedMaterial = mat;
            
            // Save material
            string matPath = path + name + "_Material.mat";
            AssetDatabase.CreateAsset(mat, matPath);
        }
        
        // Save as prefab
        string prefabPath = path + name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(obj, prefabPath);
        
        // Cleanup scene object
        Object.DestroyImmediate(obj);
    }
}// Force recompile
