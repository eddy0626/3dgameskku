using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// ObjectPoolManager 컴포넌트 유닛 테스트
/// </summary>
[TestFixture]
public class ObjectPoolManagerTests
{
    private GameObject _managerObject;
    private ObjectPoolManager _poolManager;
    private GameObject _testPrefab;

    [SetUp]
    public void SetUp()
    {
        // 기존 인스턴스 정리
        var existingManager = Object.FindFirstObjectByType<ObjectPoolManager>();
        if (existingManager != null)
        {
            Object.DestroyImmediate(existingManager.gameObject);
        }

        _managerObject = new GameObject("TestPoolManager");
        _poolManager = _managerObject.AddComponent<ObjectPoolManager>();

        // 테스트용 프리팹 생성
        _testPrefab = new GameObject("TestPrefab");
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_managerObject);
        Object.DestroyImmediate(_testPrefab);
    }

    #region 싱글톤 테스트

    [Test]
    public void Instance_ReturnsPoolManager()
    {
        // Assert
        Assert.IsNotNull(ObjectPoolManager.Instance);
        Assert.AreEqual(_poolManager, ObjectPoolManager.Instance);
    }

    #endregion

    #region 풀 등록 테스트

    [Test]
    public void RegisterPool_CreatesNewPool()
    {
        // Act
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);

        // Assert
        Assert.IsTrue(_poolManager.HasPool("TestPool"));
    }

    [Test]
    public void RegisterPool_DuplicateKey_DoesNotThrow()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);

        // Act & Assert - 중복 등록 시 에러 없이 처리
        Assert.DoesNotThrow(() => _poolManager.RegisterPool("TestPool", _testPrefab, 10, 30, true));
    }

    [Test]
    public void HasPool_UnregisteredKey_ReturnsFalse()
    {
        // Assert
        Assert.IsFalse(_poolManager.HasPool("NonExistentPool"));
    }

    #endregion

    #region 오브젝트 가져오기 테스트

    [Test]
    public void Get_RegisteredPool_ReturnsGameObject()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);

        // Act
        GameObject obj = _poolManager.Get("TestPool");

        // Assert
        Assert.IsNotNull(obj);
        Assert.IsTrue(obj.activeInHierarchy);
    }

    [Test]
    public void Get_WithPosition_SetsCorrectPosition()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);
        Vector3 targetPosition = new Vector3(10f, 5f, 3f);

        // Act
        GameObject obj = _poolManager.Get("TestPool", targetPosition, Quaternion.identity);

        // Assert
        Assert.AreEqual(targetPosition, obj.transform.position);
    }

    [Test]
    public void Get_WithRotation_SetsCorrectRotation()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);
        Quaternion targetRotation = Quaternion.Euler(45f, 90f, 0f);

        // Act
        GameObject obj = _poolManager.Get("TestPool", Vector3.zero, targetRotation);

        // Assert
        Assert.AreEqual(targetRotation.eulerAngles, obj.transform.rotation.eulerAngles);
    }

    [Test]
    public void Get_UnregisteredPool_ReturnsNull()
    {
        // Act
        GameObject obj = _poolManager.Get("NonExistentPool");

        // Assert
        Assert.IsNull(obj);
    }

    [Test]
    public void Get_EmptyPool_ExpandsAndReturns()
    {
        // Arrange - 초기 크기 1, 확장 가능
        _poolManager.RegisterPool("SmallPool", _testPrefab, 1, 10, true);

        // Act - 초기 크기보다 많이 요청
        GameObject obj1 = _poolManager.Get("SmallPool");
        GameObject obj2 = _poolManager.Get("SmallPool");

        // Assert - 둘 다 유효해야 함 (풀이 확장됨)
        Assert.IsNotNull(obj1);
        Assert.IsNotNull(obj2);
    }

    #endregion

    #region 오브젝트 반환 테스트

    [Test]
    public void Return_DeactivatesObject()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);
        GameObject obj = _poolManager.Get("TestPool");

        // Act
        _poolManager.Return("TestPool", obj);

        // Assert
        Assert.IsFalse(obj.activeInHierarchy);
    }

    [Test]
    public void Return_ObjectCanBeReused()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 1, 10, true);
        GameObject obj1 = _poolManager.Get("TestPool");
        _poolManager.Return("TestPool", obj1);

        // Act
        GameObject obj2 = _poolManager.Get("TestPool");

        // Assert - 반환된 오브젝트가 재사용됨
        Assert.AreEqual(obj1, obj2);
    }

    [Test]
    public void Return_NullObject_DoesNotThrow()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);

        // Act & Assert
        Assert.DoesNotThrow(() => _poolManager.Return("TestPool", null));
    }

    [Test]
    public void Return_UnregisteredPool_DestroysObject()
    {
        // Arrange
        GameObject orphanObject = new GameObject("Orphan");

        // EditMode에서 Destroy() 사용 시 발생하는 에러 예상
        LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");

        // Act
        _poolManager.Return("NonExistentPool", orphanObject);

        // Assert - EditMode에서는 Destroy가 즉시 적용되지 않으므로 수동 정리
        Object.DestroyImmediate(orphanObject);
    }

    #endregion

    #region 풀 크기 테스트

    [Test]
    public void GetPoolSize_ReturnsCorrectSize()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);

        // Act
        int size = _poolManager.GetPoolSize("TestPool");

        // Assert
        Assert.AreEqual(5, size);
    }

    [Test]
    public void GetPoolSize_AfterGet_DecreasesSize()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);
        int initialSize = _poolManager.GetPoolSize("TestPool");

        // Act
        _poolManager.Get("TestPool");
        int sizeAfterGet = _poolManager.GetPoolSize("TestPool");

        // Assert
        Assert.AreEqual(initialSize - 1, sizeAfterGet);
    }

    [Test]
    public void GetPoolSize_AfterReturn_IncreasesSize()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);
        GameObject obj = _poolManager.Get("TestPool");
        int sizeAfterGet = _poolManager.GetPoolSize("TestPool");

        // Act
        _poolManager.Return("TestPool", obj);
        int sizeAfterReturn = _poolManager.GetPoolSize("TestPool");

        // Assert
        Assert.AreEqual(sizeAfterGet + 1, sizeAfterReturn);
    }

    [Test]
    public void GetPoolSize_UnregisteredPool_ReturnsZero()
    {
        // Act
        int size = _poolManager.GetPoolSize("NonExistentPool");

        // Assert
        Assert.AreEqual(0, size);
    }

    #endregion

    #region 풀 정리 테스트

    [Test]
    public void ClearPool_RemovesAllObjects()
    {
        // Arrange
        _poolManager.RegisterPool("TestPool", _testPrefab, 5, 20, true);

        // EditMode에서 Destroy() 사용 시 발생하는 에러 예상 (풀 크기만큼)
        for (int i = 0; i < 5; i++)
        {
            LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
        }

        // Act
        _poolManager.ClearPool("TestPool");

        // Assert
        Assert.AreEqual(0, _poolManager.GetPoolSize("TestPool"));
    }

    [Test]
    public void ClearPool_UnregisteredPool_DoesNotThrow()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _poolManager.ClearPool("NonExistentPool"));
    }

    [Test]
    public void ClearAllPools_ClearsAllRegisteredPools()
    {
        // Arrange
        _poolManager.RegisterPool("Pool1", _testPrefab, 3, 10, true);
        _poolManager.RegisterPool("Pool2", _testPrefab, 5, 15, true);

        // EditMode에서 Destroy() 사용 시 발생하는 에러 예상 (총 8개 오브젝트)
        for (int i = 0; i < 8; i++)
        {
            LogAssert.Expect(LogType.Error, "Destroy may not be called from edit mode! Use DestroyImmediate instead.\nDestroying an object in edit mode destroys it permanently.");
        }

        // Act
        _poolManager.ClearAllPools();

        // Assert
        Assert.AreEqual(0, _poolManager.GetPoolSize("Pool1"));
        Assert.AreEqual(0, _poolManager.GetPoolSize("Pool2"));
    }

    #endregion

    #region GetByPrefab 테스트

    [Test]
    public void GetByPrefab_AutoRegistersPool()
    {
        // Act
        GameObject obj = _poolManager.GetByPrefab(_testPrefab, Vector3.zero, Quaternion.identity);

        // Assert
        Assert.IsNotNull(obj);
        Assert.IsTrue(_poolManager.HasPool(_testPrefab.name));
    }

    [Test]
    public void GetByPrefab_NullPrefab_ReturnsNull()
    {
        // Act
        GameObject obj = _poolManager.GetByPrefab(null, Vector3.zero, Quaternion.identity);

        // Assert
        Assert.IsNull(obj);
    }

    [Test]
    public void ReturnByPrefab_ReturnsToCorrectPool()
    {
        // Arrange
        GameObject obj = _poolManager.GetByPrefab(_testPrefab, Vector3.zero, Quaternion.identity);
        int sizeBeforeReturn = _poolManager.GetPoolSize(_testPrefab.name);

        // Act
        _poolManager.ReturnByPrefab(obj);

        // Assert
        Assert.IsFalse(obj.activeInHierarchy);
        Assert.AreEqual(sizeBeforeReturn + 1, _poolManager.GetPoolSize(_testPrefab.name));
    }

    #endregion
}
