using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Sirenix.OdinInspector;

namespace Logic
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PathFinder : MonoBehaviour
    {
        public CharacterLogic CharacterLogic;

        public bool isOnOffMeshLink
        {
            get { return naviAgent.currentOffMeshLinkData.valid; }
        }

        [SerializeField, ReadOnly] NavMeshAgent naviAgent;
        public NavMeshPath path;       // 기즈모 드로잉을 위해 값 보관

        // Start is called before the first frame update
        void Awake()
        {
            if (naviAgent == null)
            {
                naviAgent = GetComponent<NavMeshAgent>();
            }
            path = new NavMeshPath();
            naviAgent.speed = 0;
            // naviAgent.updatePosition = false;
        }

        public bool CalculatePath(Position2 destPos)
        {
            if (isOnOffMeshLink)
            {
                naviAgent.CompleteOffMeshLink();
            }
            Position2 curPosition = CharacterLogic.Position;
            transform.position = new Vector3(curPosition.x, 0, curPosition.y);
            return naviAgent.CalculatePath(new Vector3(destPos.x, 0, destPos.y), path);
        }

        public Position2 FollowPath(float stepLength)
        {
            Vector3 curPosition = transform.position;
            curPosition.y = 0;          // xz 평면에서만 계산할 수 있도록 y값 무시
            for (int i = 0; i < path.corners.Length; i++)
            {
                path.corners[i].y = 0;  // xz 평면에서만 계산할 수 있도록 y값 무시
                float distToNextPoint = Vector3.Distance(curPosition, path.corners[i]);
                if (distToNextPoint < stepLength)
                {
                    stepLength -= distToNextPoint;
                    curPosition = path.corners[i];
                }
                else
                {
                    curPosition = Vector3.MoveTowards(curPosition, path.corners[i], stepLength);
                    break;
                }
            }
            // isOnOffMeshLink 정보를 위해 naviAgent 강제 업데이트
            naviAgent.SetPath(path);
            return new Position2(curPosition.x, curPosition.z);
        }

        public ObstacleLogic GetOccupyingObstacle()
        {
            ObstacleVisual obstacleVisual 
                = naviAgent.currentOffMeshLinkData.offMeshLink.GetComponent<ObstacleVisual>();
            return obstacleVisual.ObstacleLogic;
        }

        public Position2 GetObstacleJumpEndPos()
        {
            Vector3 point = naviAgent.currentOffMeshLinkData.endPos;
            return new Position2(point.x, point.z);
        }

        public void CompleteObstacleJump()
        {
            naviAgent.CompleteOffMeshLink();
        }

        private void OnDrawGizmos()
        {
            if (path != null)
            {
                Gizmos.color = Color.red;
                for (int i = 1; i < path.corners.Length; i++)
                {
                    Gizmos.DrawLine(path.corners[i - 1], path.corners[i]);
                }
            }
        }
    }
}