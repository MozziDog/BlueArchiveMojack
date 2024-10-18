using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using Logic;

public class CameraTargetGroupControl : MonoBehaviour
{
    [SerializeField] BattleSceneManager battleManager;
    [SerializeField] CinemachineTargetGroup targetGroup;

    // Start is called before the first frame update
    void Awake()
    {
        battleManager.OnAllySpawn += AddCameraTargetGroupElement;
        battleManager.OnAllyDie += RemoveCameraTargetGroupElement;
        battleManager.OnEnemyDie += RemoveCameraTargetGroupElement;
    }
    
    void AddCameraTargetGroupElement(Character newCharacter, CharacterVisual characterVisual)
    {
        targetGroup.AddMember(characterVisual.transform, 1, 0);
    }

    void RemoveCameraTargetGroupElement(Character character, CharacterVisual characterVisual)
    {
        targetGroup.RemoveMember(characterVisual.transform);
    }
}
