using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAutoSkillCheck
{
    /// <summary>
    /// 매 프레임 한 번만 호출되는 스킬 사용 조건 업데이트 함수
    /// </summary>
    public void CheckSkillCondition();

    /// <summary>
    /// 매 프레임 n번 호출될 수 있는 스킬 사용 가능 여부 조회 함수
    /// </summary>
    /// <returns></returns>
    public bool CanUseSkill();

    /// <summary>
    /// 스킬 사용되었을 때 호출되는 함수. 다음 스킬 사용을 위한 조건 초기화 등 진행.
    /// </summary>
    public void ResetSkillCondition();
}
