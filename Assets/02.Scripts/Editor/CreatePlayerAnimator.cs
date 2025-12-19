using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class CreatePlayerAnimator
{
    [MenuItem("Tools/Create Player Animator Controller")]
    public static void Create()
    {
        // Animator Controller 생성
        string path = "Assets/07.Animations/PlayerAnimator.controller";
        
        // 기존 파일 있으면 삭제
        if (File.Exists(path))
        {
            AssetDatabase.DeleteAsset(path);
        }
        
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        
        // 파라미터 추가
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsShooting", AnimatorControllerParameterType.Bool);
        
        // 애니메이션 클립 로드
        var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Soldier_demo/demo_combat_idle.anim");
        var runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Soldier_demo/demo_combat_run.anim");
        var shootClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Soldier_demo/demo_combat_shoot.anim");
        
        // 레이어 가져오기
        var rootStateMachine = controller.layers[0].stateMachine;
        
        // 스테이트 생성
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
        var runState = rootStateMachine.AddState("Run", new Vector3(300, 100, 0));
        var shootState = rootStateMachine.AddState("Shoot", new Vector3(500, 50, 0));
        
        // 클립 할당
        if (idleClip != null) idleState.motion = idleClip;
        if (runClip != null) runState.motion = runClip;
        if (shootClip != null) shootState.motion = shootClip;
        
        // 기본 스테이트 설정
        rootStateMachine.defaultState = idleState;
        
        // 트랜지션 생성: Idle -> Run (Speed > 0.1)
        var idleToRun = idleState.AddTransition(runState);
        idleToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToRun.hasExitTime = false;
        idleToRun.duration = 0.1f;
        
        // 트랜지션: Run -> Idle (Speed < 0.1)
        var runToIdle = runState.AddTransition(idleState);
        runToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        runToIdle.hasExitTime = false;
        runToIdle.duration = 0.1f;
        
        // 트랜지션: Any -> Shoot (IsShooting = true)
        var anyToShoot = rootStateMachine.AddAnyStateTransition(shootState);
        anyToShoot.AddCondition(AnimatorConditionMode.If, 0, "IsShooting");
        anyToShoot.hasExitTime = false;
        anyToShoot.duration = 0.05f;
        
        // 트랜지션: Shoot -> Idle (IsShooting = false)
        var shootToIdle = shootState.AddTransition(idleState);
        shootToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsShooting");
        shootToIdle.hasExitTime = false;
        shootToIdle.duration = 0.1f;
        
        // 저장
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("PlayerAnimator.controller 생성 완료!");
    }
}
