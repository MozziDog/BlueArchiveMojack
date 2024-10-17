using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraTargetGroupControl : MonoBehaviour
{
    [SerializeField] BattleSceneManager battleManager;
    [SerializeField] CinemachineTargetGroup targetGroup;

    // Start is called before the first frame update
    void Awake()
    {
        battleManager.OnBattleBegin += InitCameraTargetGroup;
        battleManager.OnAllyDie += RemoveCameraTargetGroupElement;
        battleManager.OnEnemyDie += RemoveCameraTargetGroupElement;
    }

    void InitCameraTargetGroup()
    {
        foreach (var ally in battleManager.CharactersActive)
        {
            targetGroup.AddMember(ally.transform, 1, 0);
        }
    }

    void RemoveCameraTargetGroupElement(Character character)
    {
        targetGroup.RemoveMember(character.transform);
    }

    void AddEnemyWaveToMember(Character[] character)
    {
        // TODO: 적 웨이브 생성되었을 때 카메라 타겟으로 추가하는 것 구현
    }
}
