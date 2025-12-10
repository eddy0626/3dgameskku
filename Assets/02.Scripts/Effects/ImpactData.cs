using UnityEngine;

/// <summary>
/// 임팩트 이펙트 데이터 (표면 타입별 이펙트 정보)
/// </summary>
[CreateAssetMenu(fileName = "ImpactData", menuName = "FPS/Impact Data")]
public class ImpactData : ScriptableObject
{
    [Header("표면 타입")]
    public SurfaceType surfaceType = SurfaceType.Default;
    
    [Header("파티클 이펙트")]
    [Tooltip("스파크/파편 파티클")]
    public GameObject particlePrefab;
    
    [Tooltip("탄흔 데칼")]
    public GameObject decalPrefab;
    
    [Header("이펙트 설정")]
    [Tooltip("파티클 지속 시간")]
    public float particleLifetime = 1f;
    
    [Tooltip("탄흔 지속 시간")]
    public float decalLifetime = 10f;
    
    [Tooltip("탄흔 크기")]
    public float decalSize = 0.1f;
    
    [Header("사운드")]
    public AudioClip[] impactSounds;
    
    [Range(0f, 1f)]
    public float volume = 1f;
    
    [Header("물리 효과")]
    [Tooltip("피격 오브젝트에 가할 힘")]
    public float impactForce = 10f;
    
    /// <summary>
    /// 랜덤 임팩트 사운드 반환
    /// </summary>
    public AudioClip GetRandomSound()
    {
        if (impactSounds == null || impactSounds.Length == 0)
        {
            return null;
        }
        
        return impactSounds[Random.Range(0, impactSounds.Length)];
    }
}
