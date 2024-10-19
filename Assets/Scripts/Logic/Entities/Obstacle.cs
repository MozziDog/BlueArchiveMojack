using System;

/// <summary>
/// 엄폐물.
/// 캐릭터가 엄폐하거나 뛰어넘는 중에는 엄폐물을 '점유'한 것으로 취급,
/// '점유'된 중에는 다른 캐릭터의 길찾기 알고리즘에 경로로 고려되지 않도록 OffMeshLink 비활성화
/// </summary>

namespace Logic
{
    [Serializable]
    public class Obstacle
    {
        public Position2[] CoveringPoint;
        public bool canJumpOver;
        bool _isOccupied = false;

        public bool isOccupied
        {
            get
            {
                return _isOccupied;
            }
            set
            {
                _isOccupied = value;
                if(_isOccupied)
                {
                    if(OnObstacleOccupied != null)
                    {
                        OnObstacleOccupied();
                    }
                }
                else
                {
                    if(OnObstacleUnoccupied != null)
                    {
                        OnObstacleUnoccupied();
                    }
                }
            }
        }

        // 이벤트
        public Action OnObstacleOccupied;
        public Action OnObstacleUnoccupied;
    }
}