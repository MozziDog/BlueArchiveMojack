using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class BattleSceneManager : MonoBehaviour
{
    public BattleData battleData;
    public BattleSceneState battleState;
    public int logicTickPerSecond = 30;

    // 디버그용
    [SerializeField] Battle battle;

    [Title("Debug")]
    public GameObject characterView;

    // Start is called before the first frame update
    void Start()
    {
        // 원래는 로딩 끝나면 호출해줘야 하는데 지금은 일단 Start에서 호출하는 것으로...
        StartGame(battleData);
    }

    public void StartGame(BattleData battleData)
    {
        this.battleData = battleData;
        StartCoroutine(GameCoroutine(battleData));
    }

    protected IEnumerator GameCoroutine(BattleData battleData)
    {
        battle = new Battle(battleData);
        // 디버그용
        characterView.GetComponent<CharacterView>().character = battle.characters[0];
        // 디버그용 끝
        while(battleState == BattleSceneState.InBattle)
        {
            battle.tick();
            yield return new WaitForSeconds(1f / logicTickPerSecond);
        }
    }

    /// <summary>
    /// 아군 사망 시 이벤트
    /// </summary>
    public void OnCharacterDie()
    {

    }

    /// <summary>
    /// 적군 사망 시 이벤트
    /// </summary>
    public void OnEnemyDie()
    {

    }
}
