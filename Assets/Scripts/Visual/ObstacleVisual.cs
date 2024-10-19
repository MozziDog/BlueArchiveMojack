using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Logic;
using Sirenix.OdinInspector;
using UnityEngine.AI;

public class ObstacleVisual : MonoBehaviour
{
    public Obstacle ObstacleLogic;
    public OffMeshLink offMeshLink;

    void Awake()
    {
        // TODO: 길찾기 로직 Logic으로 완전히 옮기기
        ObstacleLogic.OnObstacleOccupied += DeactivateOffmeshLink;
        ObstacleLogic.OnObstacleUnoccupied += ActivateOffmeshLink;
    }

    /// <summary>
    /// 테스트용. Visual에서 설정된 장애물 엄폐 위치를 Logic에 반영함
    /// </summary>
    [Button]
    void SetObstacleLogicCoveringPoint(Transform[] transforms)
    {
        if(ObstacleLogic == null)
        {
            ObstacleLogic = new Obstacle();
        }
        List<Position2> coveringPoints = new List<Position2>();
        foreach(var point in transforms)
        {
            coveringPoints.Add(new Position2(point.position.x, point.position.z));
        }
        ObstacleLogic.CoveringPoint = coveringPoints.ToArray();
    }

    void ActivateOffmeshLink()
    {
        if(offMeshLink != null)
        {
            offMeshLink.activated = true;
        }
    }

    void DeactivateOffmeshLink()
    {
        if(offMeshLink != null)
        {
            offMeshLink.activated = false;
        }
    }
}
