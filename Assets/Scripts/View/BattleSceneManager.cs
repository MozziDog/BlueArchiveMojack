using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class BattleSceneManager : MonoBehaviour
{
    [Title("전투 초기화 정보")]
    public BattleData BattleData;
    public CharacterPrefabDatabase CharacterViewDatabase;
    public List<Transform> SpawnPoint;

    [Title("전투 씬 상태")]
    [SerializeField] int _logicTickPerSecond = 30;
    public int BaseLogicTickrate = 30;
    public BattleSceneState BattleState;

    [Title("관리중인 엔티티들")]
    public List<Character> CharactersActive;      // 아군
    public List<Character> EnemiesActive;          // 적군
    public List<Obstacle> Obstacles;
    [SerializeField] List<Bullet> BulletsActive;
    [SerializeField] List<Bullet> BulletsToRemove;

    // Start is called before the first frame update
    void Start()
    {
        // 원래는 로딩 끝나면 호출해줘야 하는데 지금은 일단 Start에서 호출하는 것으로...
        StartGame(BattleData);
    }

    public void StartGame(BattleData battleData)
    {
        this.BattleData = battleData;
        StartCoroutine(GameCoroutine(battleData));
    }

    protected IEnumerator GameCoroutine(BattleData battleData)
    {
        // battleData에 기록된 캐릭터들을 스폰
        // 아군
        for(int i=0; i<battleData.characters.Count; i++)
        {
            GameObject instance = Instantiate(CharacterViewDatabase.CharacterViews[battleData.characters[i].Name]);
            instance.transform.position = SpawnPoint[i].position;
            Character characterComponent = instance.GetComponent<Character>();
            characterComponent.Init(this, battleData.characters[i], battleData.characterStats[i]);
            CharactersActive.Add(characterComponent);
        }

        // 게임루프 수행
        while(BattleState == BattleSceneState.InBattle)
        {
            if(CharactersActive.Count <= 0)
            {
                Debug.Log("게임 오버");
                yield break;
            }
            Tick();
            // 삭제해야할 엔티티 정리
            RemoveInactiveEntities();
            yield return new WaitForSeconds(1f / _logicTickPerSecond);
        }
    }

    protected void Tick()
    {
        foreach(var character in CharactersActive)
        {
            character.Tick();
        }
        foreach(var bullet in BulletsActive)
        {
            bullet.Tick();
        }
    }

    // Tick 안의 foreach에서 엔티티가 삭제/추가되면 안되므로
    // 삭제해야 할 엔티티들은 Tick 후에 따로 삭제
    void RemoveInactiveEntities()
    {
        foreach(var bullet in BulletsToRemove)
        {
            BulletsActive.Remove(bullet);
            Destroy(bullet.gameObject);
        }
        BulletsToRemove.Clear();
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

    public void AddBullet(Bullet bullet)
    {
        BulletsActive.Add(bullet);
        bullet.Init(this);
    }

    public void RemoveBullet(Bullet bullet)
    {
        BulletsToRemove.Add(bullet);
    }
}
