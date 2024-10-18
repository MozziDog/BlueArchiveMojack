using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 엄폐물.
/// 캐릭터가 엄폐하거나 뛰어넘는 중에는 엄폐물을 '점유'한 것으로 취급,
/// '점유'된 중에는 다른 캐릭터의 길찾기 알고리즘에 경로로 고려되지 않도록 OffMeshLink 비활성화
/// </summary>

namespace Logic
{
    public class Obstacle : MonoBehaviour
    {
        public Transform[] CoveringPointTransform;

        public Position2[] CoveringPoint {
            get
            {
                return Array.ConvertAll(CoveringPointTransform, el => { return new Position2(el.position.x, el.position.z); });
            }
        }

        public bool isOccupied
        {
            get
            {
                return _isOccupied;
            }
            set
            {
                _isOccupied = value;
                if (canJumpOver)
                    offMeshLink.activated = !value;
            }
        }

        [SerializeField] OffMeshLink offMeshLink;
        [SerializeField] bool canJumpOver;
        [SerializeField] bool _isOccupied = false;
    }
}