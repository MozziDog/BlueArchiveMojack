using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterVisual : MonoBehaviour
{
    public Character characterLogic;
    Vector3 positionBeforeFrame;

    // Start is called before the first frame update
    void Start()
    {
        if(characterLogic == null)
        {
            characterLogic = GetComponent<Character>();
        }
        positionBeforeFrame = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if(characterLogic.isMoving)
        {
            Vector3 positionCurrentFrame = transform.position;
            // (현재 위치 + 이동방향) 바라보기
            // 이동방향은 지난 프레임과의 변위로 계산
            transform.LookAt(2 * positionCurrentFrame - positionBeforeFrame);
            positionBeforeFrame= positionCurrentFrame;
        }
    }
}
