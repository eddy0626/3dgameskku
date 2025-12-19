using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using SquadSurvival.Squad;

namespace SquadSurvival.Economy
{
    /// <summary>
    /// 업그레이드 시스템 관리자
    /// 플레이어 및 분대 업그레이드 구매/적용
    /// </summary>
    public class UpgradeSystem : MonoBehaviour
    {
        public static UpgradeSystem Instance { get; private set; }

        [System.Serializable]
        public class UpgradeData
        {
            public string id;
            public string displayName;
            public string description;
            public Sprite icon;
            public UpgradeType type;
            public int baseCost = 100;
            public float costMultiplier = 1.5f;
            public int maxLevel = 5;
            public int currentLevel = 0;
            public float baseValue = 0.1f;
            public float valuePerLevel = 0.1f;

            public int GetCurrentCost()
            {
                return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplier, currentLevel));
            }

            public float GetCurrentValue()
            {
                return baseValue + (valuePerLevel * currentLevel);
            }

            public bool IsMaxLevel()
            {
                return currentLevel >= maxLevel;
            }
        }

        public enum UpgradeType
        {
            // 플레이어 업그레이드
            PlayerHealth,
            PlayerDamage,
            PlayerFireRate,
            PlayerReloadSpeed,
            PlayerMoveSpeed,

            // 분대 업그레이드
            SquadHealth,
            SquadDamage,
            SquadAttackSpeed,
            SquadRevive,

            // 경제 업그레이드
            CoinBonus,
            CoinMagnet,

            // 특수 업그레이드
            Heal,
            ReviveAll,
            Airstrike
        }

        [Header("업그레이드 목록")]
        [SerializeField] private List<UpgradeData> upgrades = new List<UpgradeData>();

        [Header("이벤트")]
        public UnityEvent<UpgradeData> OnUpgradePurchased;
        public UnityEvent<UpgradeType, int> OnUpgradeLevelChanged;
        public UnityEvent OnUpgradeListRefreshed;

        private Dictionary<string, UpgradeData> upgradeDict = new Dictionary<string, UpgradeData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeEvents();
            InitializeUpgrades();
        }

        private void InitializeEvents()
        {
            if (OnUpgradePurchased == null) OnUpgradePurchased = new UnityEvent<UpgradeData>();
            if (OnUpgradeLevelChanged == null) OnUpgradeLevelChanged = new UnityEvent<UpgradeType, int>();
            if (OnUpgradeListRefreshed == null) OnUpgradeListRefreshed = new UnityEvent();
        }

        /// <summary>
        /// 기본 업그레이드 초기화
        /// </summary>
        private void InitializeUpgrades()
        {
            if (upgrades.Count == 0)
            {
                CreateDefaultUpgrades();
            }

            // Dictionary 구축
            upgradeDict.Clear();
            foreach (var upgrade in upgrades)
            {
                upgradeDict[upgrade.id] = upgrade;
            }
        }

        /// <summary>
        /// 기본 업그레이드 생성
        /// </summary>
        private void CreateDefaultUpgrades()
        {
            // 플레이어 업그레이드
            upgrades.Add(new UpgradeData
            {
                id = "player_health",
                displayName = "체력 강화",
                description = "최대 체력 +20%",
                type = UpgradeType.PlayerHealth,
                baseCost = 100,
                maxLevel = 5,
                baseValue = 0f,
                valuePerLevel = 0.2f
            });

            upgrades.Add(new UpgradeData
            {
                id = "player_damage",
                displayName = "공격력 강화",
                description = "무기 데미지 +15%",
                type = UpgradeType.PlayerDamage,
                baseCost = 150,
                maxLevel = 5,
                baseValue = 0f,
                valuePerLevel = 0.15f
            });

            upgrades.Add(new UpgradeData
            {
                id = "player_firerate",
                displayName = "연사력 강화",
                description = "발사 속도 +10%",
                type = UpgradeType.PlayerFireRate,
                baseCost = 200,
                maxLevel = 3,
                baseValue = 0f,
                valuePerLevel = 0.1f
            });

            // 분대 업그레이드
            upgrades.Add(new UpgradeData
            {
                id = "squad_health",
                displayName = "분대 체력",
                description = "분대원 체력 +25%",
                type = UpgradeType.SquadHealth,
                baseCost = 120,
                maxLevel = 5,
                baseValue = 0f,
                valuePerLevel = 0.25f
            });

            upgrades.Add(new UpgradeData
            {
                id = "squad_damage",
                displayName = "분대 화력",
                description = "분대원 공격력 +20%",
                type = UpgradeType.SquadDamage,
                baseCost = 150,
                maxLevel = 5,
                baseValue = 0f,
                valuePerLevel = 0.2f
            });

            // 경제 업그레이드
            upgrades.Add(new UpgradeData
            {
                id = "coin_bonus",
                displayName = "코인 보너스",
                description = "코인 획득량 +10%",
                type = UpgradeType.CoinBonus,
                baseCost = 100,
                maxLevel = 5,
                baseValue = 0f,
                valuePerLevel = 0.1f
            });

            // 즉시 사용 업그레이드
            upgrades.Add(new UpgradeData
            {
                id = "heal",
                displayName = "응급 치료",
                description = "플레이어 체력 50% 회복",
                type = UpgradeType.Heal,
                baseCost = 50,
                maxLevel = 999,
                baseValue = 0.5f,
                valuePerLevel = 0f
            });

            upgrades.Add(new UpgradeData
            {
                id = "revive_all",
                displayName = "분대 부활",
                description = "사망한 분대원 전원 부활",
                type = UpgradeType.ReviveAll,
                baseCost = 200,
                maxLevel = 999,
                baseValue = 0.5f,
                valuePerLevel = 0f
            });
        }

        /// <summary>
        /// 업그레이드 구매
        /// </summary>
        public bool PurchaseUpgrade(string upgradeId)
        {
            if (!upgradeDict.TryGetValue(upgradeId, out var upgrade))
            {
                Debug.LogWarning($"[UpgradeSystem] 업그레이드를 찾을 수 없음: {upgradeId}");
                return false;
            }

            return PurchaseUpgrade(upgrade);
        }

        /// <summary>
        /// 업그레이드 구매
        /// </summary>
        public bool PurchaseUpgrade(UpgradeData upgrade)
        {
            if (upgrade == null) return false;

            // 최대 레벨 체크 (일회성 제외)
            if (upgrade.type != UpgradeType.Heal && 
                upgrade.type != UpgradeType.ReviveAll && 
                upgrade.type != UpgradeType.Airstrike &&
                upgrade.IsMaxLevel())
            {
                Debug.Log($"[UpgradeSystem] {upgrade.displayName} 최대 레벨 도달");
                return false;
            }

            int cost = upgrade.GetCurrentCost();

            // 코인 확인 및 사용
            if (CoinManager.Instance == null || !CoinManager.Instance.SpendCoins(cost, upgrade.displayName))
            {
                return false;
            }

            // 레벨 업
            upgrade.currentLevel++;

            // 효과 적용
            ApplyUpgradeEffect(upgrade);

            OnUpgradePurchased?.Invoke(upgrade);
            OnUpgradeLevelChanged?.Invoke(upgrade.type, upgrade.currentLevel);

#if UNITY_EDITOR
            Debug.Log($"[UpgradeSystem] {upgrade.displayName} 구매 완료! (Lv.{upgrade.currentLevel})");
#endif

            return true;
        }

        /// <summary>
        /// 업그레이드 효과 적용
        /// </summary>
        private void ApplyUpgradeEffect(UpgradeData upgrade)
        {
            switch (upgrade.type)
            {
                case UpgradeType.PlayerHealth:
                    ApplyPlayerHealthUpgrade(upgrade.GetCurrentValue());
                    break;

                case UpgradeType.PlayerDamage:
                    // WeaponManager에서 데미지 배율 적용
                    break;

                case UpgradeType.SquadHealth:
                    ApplySquadHealthUpgrade(upgrade.GetCurrentValue());
                    break;

                case UpgradeType.SquadDamage:
                    // SquadMember에서 데미지 배율 적용
                    break;

                case UpgradeType.Heal:
                    ApplyHeal(upgrade.baseValue);
                    break;

                case UpgradeType.ReviveAll:
                    ApplyReviveAll(upgrade.baseValue);
                    break;
            }
        }

        /// <summary>
        /// 플레이어 체력 업그레이드 적용
        /// </summary>
        private void ApplyPlayerHealthUpgrade(float multiplier)
        {
            var playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // PlayerHealth에 SetMaxHealthMultiplier 메서드가 있다면 호출
                // playerHealth.SetMaxHealthMultiplier(1f + multiplier);
            }
        }

        /// <summary>
        /// 분대 체력 업그레이드 적용
        /// </summary>
        private void ApplySquadHealthUpgrade(float multiplier)
        {
            if (SquadController.Instance != null)
            {
                foreach (var member in SquadController.Instance.SquadMembers)
                {
                    if (member != null)
                    {
                        // SquadMember에 SetHealthMultiplier 메서드가 있다면 호출
                        // member.SetHealthMultiplier(1f + multiplier);
                    }
                }
            }
        }

        /// <summary>
        /// 즉시 회복 적용
        /// </summary>
        private void ApplyHeal(float healPercent)
        {
            var playerHealth = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // playerHealth.HealPercent(healPercent);
            }

#if UNITY_EDITOR
            Debug.Log($"[UpgradeSystem] 플레이어 체력 {healPercent:P0} 회복");
#endif
        }

        /// <summary>
        /// 분대 전원 부활
        /// </summary>
        private void ApplyReviveAll(float healthPercent)
        {
            if (SquadController.Instance != null)
            {
                SquadController.Instance.ReviveAllMembers(healthPercent);
            }

#if UNITY_EDITOR
            Debug.Log($"[UpgradeSystem] 분대 전원 부활 (체력 {healthPercent:P0})");
#endif
        }

        /// <summary>
        /// 업그레이드 정보 가져오기
        /// </summary>
        public UpgradeData GetUpgrade(string id)
        {
            return upgradeDict.TryGetValue(id, out var upgrade) ? upgrade : null;
        }

        /// <summary>
        /// 업그레이드 레벨 가져오기
        /// </summary>
        public int GetUpgradeLevel(UpgradeType type)
        {
            foreach (var upgrade in upgrades)
            {
                if (upgrade.type == type)
                {
                    return upgrade.currentLevel;
                }
            }
            return 0;
        }

        /// <summary>
        /// 업그레이드 값 가져오기
        /// </summary>
        public float GetUpgradeValue(UpgradeType type)
        {
            foreach (var upgrade in upgrades)
            {
                if (upgrade.type == type)
                {
                    return upgrade.GetCurrentValue();
                }
            }
            return 0f;
        }

        /// <summary>
        /// 모든 업그레이드 목록
        /// </summary>
        public List<UpgradeData> GetAllUpgrades()
        {
            return upgrades;
        }

        /// <summary>
        /// 구매 가능한 업그레이드 목록
        /// </summary>
        public List<UpgradeData> GetAvailableUpgrades()
        {
            var available = new List<UpgradeData>();
            
            foreach (var upgrade in upgrades)
            {
                // 일회성이 아니고 최대 레벨이면 제외
                if (upgrade.type != UpgradeType.Heal &&
                    upgrade.type != UpgradeType.ReviveAll &&
                    upgrade.type != UpgradeType.Airstrike &&
                    upgrade.IsMaxLevel())
                {
                    continue;
                }

                available.Add(upgrade);
            }

            return available;
        }

        /// <summary>
        /// 모든 업그레이드 초기화
        /// </summary>
        public void ResetAllUpgrades()
        {
            foreach (var upgrade in upgrades)
            {
                upgrade.currentLevel = 0;
            }

            OnUpgradeListRefreshed?.Invoke();

#if UNITY_EDITOR
            Debug.Log("[UpgradeSystem] 모든 업그레이드 초기화됨");
#endif
        }
    }
}
