using UnityEngine;

/// <summary>
/// 오브젝트의 표면 타입을 지정하는 컴포넌트
/// 충돌 시 해당 표면 타입에 맞는 임팩트 이펙트 재생
/// </summary>
public class SurfaceIdentifier : MonoBehaviour
{
    [SerializeField] private SurfaceType _surfaceType = SurfaceType.Default;
    
    /// <summary>
    /// 표면 타입 반환
    /// </summary>
    public SurfaceType SurfaceType => _surfaceType;
    
    /// <summary>
    /// 표면 타입 설정
    /// </summary>
    public void SetSurfaceType(SurfaceType type)
    {
        _surfaceType = type;
    }
}
