using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

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

    [Title("EX 스킬 관련")]
    public int ExCostCount = 0;         // 현재 코스트 갯수. Ex 스킬 사용에 필요.
    public int ExCostRecharging = 0;    // 현재 코스트 충전량. 이 값이 최대치가 되면 ExCostCount가 1 증가.
    public int ExCostGaugePerCount;     // 코스트 갯수 1개를 증가시키기 위해 필요한 충전량.
    public int ExCostRegen = 0;         // 틱 당 코스트 회복량. 캐릭터 코스트 회복량의 총합.
    public List<Character> skillCardHand = new List<Character>();       // 패. 최대 3장
    public LinkedList<Character> skillCardDeck = new LinkedList<Character>(); // 덱. 패에 들고 있지 않은 모든 스킬카드.

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

        // 코스트 회복량 산정
        foreach(var character in CharactersActive)
        {
            ExCostRegen += character.CostRegen;
        }

        // 스킬카드 덱 구성 & 최대 3장 드로우
        foreach(int i in Enumerable.Range(0, CharactersActive.Count).OrderBy(x => Random.Range(0,1)))
        {
            skillCardDeck.AddLast(CharactersActive[i]);
        }
        for(int i=0; i< Mathf.Min(skillCardDeck.Count, 3); i++)
        {
            DrawSkillCard();
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
        // 코스트 회복
        if(ExCostCount < 10)
        {
            ExCostRecharging += ExCostRegen;
            if(ExCostRecharging > ExCostGaugePerCount)
            {
                ExCostRecharging -= ExCostGaugePerCount;
                ExCostCount++;
            }
        }

        // 관리중인 객체들을 모두 틱
        foreach(var character in CharactersActive)
        {
            character.Tick();
        }
        foreach(var bullet in BulletsActive)
        {
            bullet.Tick();
        }
    }

    void DrawSkillCard()
    {
        skillCardHand.Add(skillCardDeck.First.Value);
        skillCardDeck.RemoveFirst();
    }

    void RemoveSkillCardFromDeck(Character toRemove)
    {
        // 삭제할 카드가 패에 있다면, 삭제하고 (가능하다면) 드로우
        if(skillCardHand.Remove(toRemove))
        {
            if(skillCardDeck.Count > 0)
            {
                DrawSkillCard();
            }
        }
        else
        {
            skillCardDeck.Remove(toRemove);
        }
    }

    public void TryUseSkillCard(int index)
    {
        // ex 스킬 사용 가능한지 먼저 체크
        if(skillCardHand.Count <= index)
        {
            Debug.LogError("해당 위치에는 스킬카드가 없음!");
            return;
        }
        Character character = skillCardHand[index];
        if(!character.CanUseExSkill)
        {
            Debug.LogWarning("장애물을 뛰어넘는 중에는 Ex 스킬을 사용할 수 없음");
            return;
        }
        if(ExCostCount < character.ExSkillCost)
        {
            Debug.Log("EX스킬 사용을 위한 코스트가 충분하지 않음!");
            return;
        }

        character.exSkillTrigger = true;

        // 스킬 카드를 덱의 맨 밑으로 넣고 한 장 드로우
        skillCardHand.Remove(character);
        skillCardDeck.AddLast(character);
        DrawSkillCard();

        return;
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
    public void OnCharacterDie(Character deadCharacter)
    {
        RemoveSkillCardFromDeck(deadCharacter);
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
