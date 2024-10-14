using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

[RequireComponent(typeof(NavMeshAgent))]
public class PathFinder : MonoBehaviour
{
    public bool isOnOffMeshLink
    {
        get { return naviAgent.currentOffMeshLinkData.valid;}
    }

    [SerializeField, ReadOnly] NavMeshAgent naviAgent;
    public NavMeshPath path;       // 기즈모 드로잉을 위해 값 보관

    // Start is called before the first frame update
    void Awake()
    {
        if(naviAgent == null)
        {
            naviAgent = GetComponent<NavMeshAgent>();
        }
        path = new NavMeshPath();
        naviAgent.speed = 0;
        // naviAgent.updatePosition = false;
    }

    public Vector3[] CalculatePath(Vector3 destPos)
    {
        if(isOnOffMeshLink)
        {
            naviAgent.CompleteOffMeshLink();
        }
        naviAgent.CalculatePath(destPos, path);
        return path.corners;
    }

    public void FollowPath(float stepLength)
    {
        Debug.Log($"stepLength: {stepLength}");
        Vector3 charPosition = transform.position;
        for(int i=0; i<path.corners.Length; i++)
        {
            float distToNextPoint = Vector3.Distance(charPosition, path.corners[i]);
            if(distToNextPoint < stepLength)
            {
                stepLength -= distToNextPoint;
                charPosition = path.corners[i];
            }
            else
            {
                charPosition = Vector3.MoveTowards(charPosition, path.corners[i], stepLength);
                break;
            }
        }
        // isOnOffMeshLink 정보를 위해 naviAgent 강제 업데이트
        naviAgent.SetPath(path);
        naviAgent.Move(charPosition - transform.position);
    }

    public Obstacle GetOccupyingObstacle()
    {
        return naviAgent.currentOffMeshLinkData.offMeshLink.GetComponent<Obstacle>();
    }

    public Vector3 GetObstacleJumpEndPos()
    {
        return naviAgent.currentOffMeshLinkData.endPos;
    }

    public void CompleteObstacleJump()
    {
        naviAgent.CompleteOffMeshLink();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for(int i=1; i<path.corners.Length; i++)
        {
            Gizmos.DrawLine(path.corners[i-1], path.corners[i]);
        }
    }
}