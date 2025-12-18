using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyAI 컴포넌트 유닛 테스트
/// 상태 머신 및 기본 동작 검증
/// </summary>
[TestFixture]
public class EnemyAITests
{
    private GameObject _enemyObject;
    private EnemyAI _enemyAI;
    private GameObject _targetObject;

    [SetUp]
    public void SetUp()
    {
        // NavMeshAgent가 필요하므로 먼저 추가
        _enemyObject = new GameObject("TestEnemy");
        _enemyObject.AddComponent<NavMeshAgent>();
        _enemyAI = _enemyObject.AddComponent<EnemyAI>();

        // 테스트용 타겟
        _targetObject = new GameObject("TestTarget");
        _targetObject.tag = "Player";
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_enemyObject);
        Object.DestroyImmediate(_targetObject);
    }

    #region 초기 상태 테스트

    [Test]
    public void EnemyAI_InitialState_HasNoTarget()
    {
        // Assert
        Assert.IsFalse(_enemyAI.HasTarget);
    }

    [Test]
    public void EnemyAI_InitialState_TargetIsNull()
    {
        // Assert
        Assert.IsNull(_enemyAI.Target);
    }

    [Test]
    public void EnemyAI_InitialDistanceToTarget_IsMaxValue()
    {
        // Assert - 타겟이 없으면 float.MaxValue 반환
        Assert.AreEqual(float.MaxValue, _enemyAI.DistanceToTarget);
    }

    #endregion

    #region 타겟 설정 테스트

    [Test]
    public void SetTarget_WithValidTarget_SetsHasTargetTrue()
    {
        // Act
        _enemyAI.SetTarget(_targetObject.transform);

        // Assert
        Assert.IsTrue(_enemyAI.HasTarget);
        Assert.AreEqual(_targetObject.transform, _enemyAI.Target);
    }

    [Test]
    public void SetTarget_WithNull_SetsHasTargetFalse()
    {
        // Arrange
        _enemyAI.SetTarget(_targetObject.transform);
        Assert.IsTrue(_enemyAI.HasTarget);

        // Act
        _enemyAI.SetTarget(null);

        // Assert
        Assert.IsFalse(_enemyAI.HasTarget);
        Assert.IsNull(_enemyAI.Target);
    }

    [Test]
    public void DistanceToTarget_WithTarget_ReturnsCorrectDistance()
    {
        // Arrange
        _targetObject.transform.position = new Vector3(10f, 0f, 0f);
        _enemyObject.transform.position = Vector3.zero;
        _enemyAI.SetTarget(_targetObject.transform);

        // Assert
        Assert.AreEqual(10f, _enemyAI.DistanceToTarget, 0.001f);
    }

    [Test]
    public void DistanceToTarget_WithMovingTarget_UpdatesDistance()
    {
        // Arrange
        _enemyObject.transform.position = Vector3.zero;
        _enemyAI.SetTarget(_targetObject.transform);

        // Act & Assert - 거리 변경 확인
        _targetObject.transform.position = new Vector3(5f, 0f, 0f);
        Assert.AreEqual(5f, _enemyAI.DistanceToTarget, 0.001f);

        _targetObject.transform.position = new Vector3(20f, 0f, 0f);
        Assert.AreEqual(20f, _enemyAI.DistanceToTarget, 0.001f);
    }

    #endregion

    #region 순찰 포인트 테스트

    [Test]
    public void SetPatrolPoints_SetsPatrolPoints()
    {
        // Arrange
        Transform[] patrolPoints = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject point = new GameObject($"PatrolPoint{i}");
            patrolPoints[i] = point.transform;
            patrolPoints[i].position = new Vector3(i * 5f, 0f, 0f);
        }

        // Act
        _enemyAI.SetPatrolPoints(patrolPoints);

        // Assert - 순찰 포인트가 설정되었는지 간접 확인
        // (private 필드이므로 직접 확인 불가, 예외 없이 설정됨을 확인)
        Assert.DoesNotThrow(() => _enemyAI.SetPatrolPoints(patrolPoints));

        // Cleanup
        foreach (var point in patrolPoints)
        {
            Object.DestroyImmediate(point.gameObject);
        }
    }

    [Test]
    public void SetPatrolPoints_NullArray_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _enemyAI.SetPatrolPoints(null));
    }

    [Test]
    public void SetPatrolPoints_EmptyArray_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _enemyAI.SetPatrolPoints(new Transform[0]));
    }

    #endregion

    #region 알림 테스트

    [Test]
    public void Alert_DoesNotThrow()
    {
        // Arrange
        Vector3 alertPosition = new Vector3(15f, 0f, 10f);

        // Act & Assert
        Assert.DoesNotThrow(() => _enemyAI.Alert(alertPosition));
    }

    [Test]
    public void Alert_MultipleAlerts_DoesNotThrow()
    {
        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            Vector3 alertPos = new Vector3(Random.Range(-10f, 10f), 0f, Random.Range(-10f, 10f));
            Assert.DoesNotThrow(() => _enemyAI.Alert(alertPos));
        }
    }

    #endregion

    #region Initialize 테스트

    [Test]
    public void Initialize_SetsParameters()
    {
        // Arrange
        float detectionRange = 20f;
        float fieldOfView = 90f;
        float detectionHeight = 3f;
        float hearingRange = 15f;
        float patrolSpeed = 3f;
        float patrolWaitTime = 1.5f;
        bool randomPatrol = true;
        float chaseSpeed = 7f;
        float chaseRange = 25f;
        float loseTargetTime = 3f;
        float attackRange = 3f;
        float attackCooldown = 2f;
        float maxChaseDistance = 40f;
        bool returnToSpawn = false;
        float rotationSpeed = 15f;
        AudioClip alertSound = null;

        // Act & Assert - 예외 없이 초기화됨
        Assert.DoesNotThrow(() => _enemyAI.Initialize(
            detectionRange,
            fieldOfView,
            detectionHeight,
            hearingRange,
            patrolSpeed,
            patrolWaitTime,
            randomPatrol,
            chaseSpeed,
            chaseRange,
            loseTargetTime,
            attackRange,
            attackCooldown,
            maxChaseDistance,
            returnToSpawn,
            rotationSpeed,
            alertSound
        ));
    }

    #endregion

    #region ResetAI 테스트

    [Test]
    public void ResetAI_ResetsTargetState()
    {
        // Arrange
        _enemyAI.SetTarget(_targetObject.transform);
        Assert.IsTrue(_enemyAI.HasTarget);

        // Act
        _enemyAI.ResetAI();

        // Assert
        Assert.IsFalse(_enemyAI.HasTarget);
    }

    [Test]
    public void ResetAI_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _enemyAI.ResetAI());
    }

    #endregion

    #region 상태 열거형 테스트

    [Test]
    public void EnemyState_HasAllExpectedValues()
    {
        // Assert - 모든 예상 상태 존재 확인
        Assert.IsTrue(System.Enum.IsDefined(typeof(EnemyAI.EnemyState), "Idle"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(EnemyAI.EnemyState), "Patrol"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(EnemyAI.EnemyState), "Chase"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(EnemyAI.EnemyState), "Attack"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(EnemyAI.EnemyState), "Return"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(EnemyAI.EnemyState), "Hit"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(EnemyAI.EnemyState), "Jump"));
        Assert.IsTrue(System.Enum.IsDefined(typeof(EnemyAI.EnemyState), "Dead"));
    }

    [Test]
    public void EnemyState_CountIsEight()
    {
        // Assert
        var values = System.Enum.GetValues(typeof(EnemyAI.EnemyState));
        Assert.AreEqual(8, values.Length);
    }

    #endregion

    #region 컴포넌트 의존성 테스트

    [Test]
    public void EnemyAI_RequiresNavMeshAgent()
    {
        // Assert - RequireComponent 확인
        var requireComponentAttrs = typeof(EnemyAI).GetCustomAttributes(typeof(RequireComponent), true);
        Assert.Greater(requireComponentAttrs.Length, 0);

        var requireComponent = (RequireComponent)requireComponentAttrs[0];
        Assert.AreEqual(typeof(NavMeshAgent), requireComponent.m_Type0);
    }

    [Test]
    public void EnemyAI_HasNavMeshAgentComponent()
    {
        // Assert
        NavMeshAgent agent = _enemyObject.GetComponent<NavMeshAgent>();
        Assert.IsNotNull(agent);
    }

    #endregion

    #region 경계 조건 테스트

    [Test]
    public void DistanceToTarget_WithSamePosition_ReturnsZero()
    {
        // Arrange
        _enemyObject.transform.position = Vector3.zero;
        _targetObject.transform.position = Vector3.zero;
        _enemyAI.SetTarget(_targetObject.transform);

        // Assert
        Assert.AreEqual(0f, _enemyAI.DistanceToTarget, 0.001f);
    }

    [Test]
    public void DistanceToTarget_WithVerticalDifference_IncludesYAxis()
    {
        // Arrange
        _enemyObject.transform.position = Vector3.zero;
        _targetObject.transform.position = new Vector3(0f, 10f, 0f);
        _enemyAI.SetTarget(_targetObject.transform);

        // Assert - 3D 거리 계산 확인
        Assert.AreEqual(10f, _enemyAI.DistanceToTarget, 0.001f);
    }

    [Test]
    public void DistanceToTarget_3DDiagonal_CalculatesCorrectly()
    {
        // Arrange
        _enemyObject.transform.position = Vector3.zero;
        _targetObject.transform.position = new Vector3(3f, 4f, 0f); // 3-4-5 삼각형
        _enemyAI.SetTarget(_targetObject.transform);

        // Assert
        Assert.AreEqual(5f, _enemyAI.DistanceToTarget, 0.001f);
    }

    #endregion
}
