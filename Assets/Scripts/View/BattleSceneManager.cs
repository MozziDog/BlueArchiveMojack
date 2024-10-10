using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class BattleSceneManager : MonoBehaviour
{
    public CharacterPrefabDatabase characterViewDatabase;

    public BattleData battleData;
    public BattleSceneState battleState;
    public int logicTickPerSecond = 30;

    [ReadOnly] public List<Character> activeCharacters;      // 아군
    public List<Character> activeEnemies;          // 적군
    public List<Obstacle> obstacles;

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
        // battleData에 기록된 캐릭터들을 스폰
        // 아군
        for(int i=0; i<battleData.characters.Count; i++)
        {
            GameObject instance = Instantiate(characterViewDatabase.CharacterViews[battleData.characters[i].Name]);
            Character characterComponent = instance.GetComponent<Character>();
            characterComponent.Init(this, battleData.characters[i], battleData.characterStats[i]);
            activeCharacters.Add(characterComponent);
        }

        // 게임루프 수행
        while(battleState == BattleSceneState.InBattle)
        {
            if(activeCharacters.Count <= 0)
            {
                Debug.Log("게임 오버");
                yield break;
            }
            Tick();
            yield return new WaitForSeconds(1f / logicTickPerSecond);
        }
    }

    protected void Tick()
    {
        foreach(var ch in activeCharacters)
        {
            ch.Tick();
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
