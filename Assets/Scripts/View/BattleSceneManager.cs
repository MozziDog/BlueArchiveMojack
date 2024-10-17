using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using System;

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
    public CharacterGroup CharactersActive;      // 아군
    List<Character> _charactersToRemove = new List<Character>();
    public CharacterGroup EnemiesActive;          // 적군
    List<Character> _enemiesToRemove = new List<Character>();
    public List<Obstacle> Obstacles;
    public List<Bullet> BulletsActive;
    List<Bullet> _bulletsToRemove = new List<Bullet>();

    [Title("EX 스킬 관련")]
    public int ExCostCount = 0;         // 현재 코스트 갯수. Ex 스킬 사용에 필요.
    public int ExCostRecharging = 0;    // 현재 코스트 충전량. 이 값이 최대치가 되면 ExCostCount가 1 증가.
    public int ExCostGaugePerCount;     // 코스트 갯수 1개를 증가시키기 위해 필요한 충전량.
    public int ExCostRegen = 0;         // 틱 당 코스트 회복량. 캐릭터 코스트 회복량의 총합.
    public List<Character> skillCardHand = new List<Character>();       // 패. 최대 3장
    public LinkedList<Character> skillCardDeck = new LinkedList<Character>(); // 덱. 패에 들고 있지 않은 모든 스킬카드.

    // 전투 중 발생하는 이벤트들
    public Action OnBattleBegin;
    public delegate void CharacterDieEvent(Character deadCharacter);
    public CharacterDieEvent OnAllyDie;
    public CharacterDieEvent OnEnemyDie;

    void Start()
    {
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
        foreach(int i in Enumerable.Range(0, CharactersActive.Count).OrderBy(x => UnityEngine.Random.Range(0,1)))
        {
            skillCardDeck.AddLast(CharactersActive[i]);
        }
        for(int i=0; i< Mathf.Min(skillCardDeck.Count, 3); i++)
        {
            DrawSkillCard();
        }
        OnAllyDie += RemoveSkillCardFromDeck;

        // 초기화 완료 후, 게임 루프 시작 전에 이벤트 호출
        OnBattleBegin();

        // 게임루프 수행
        while (BattleState == BattleSceneState.InBattle)
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

    /// <summary>
    /// Tick 안의 루프에서 엔티티가 삭제/추가되면 안되므로 삭제해야 할 엔티티들은 Tick 후에 따로 삭제
    /// </summary>
    void RemoveInactiveEntities()
    {
        // 순회에 문제 없도록 인덱스 뒤쪽부터 삭제
        if (_charactersToRemove.Count > 0)
        {
            for (int i = _charactersToRemove.Count - 1; i >= 0; i--)
            {
                CharactersActive.Remove(_charactersToRemove[i]);
                Destroy(_charactersToRemove[i].gameObject);
            }
            _charactersToRemove.Clear();
        }
        if (_enemiesToRemove.Count > 0)
        {
            for(int i = _enemiesToRemove.Count - 1; i>=0; i--)
            {
                EnemiesActive.Remove(_enemiesToRemove[i]);
                Destroy(_enemiesToRemove[i].gameObject);
            }
            _enemiesToRemove.Clear();
        }
        if (_bulletsToRemove.Count > 0)
        {
            for (int i = _bulletsToRemove.Count - 1; i >= 0; i--)
            {
                BulletsActive.Remove(_bulletsToRemove[i]);
                Destroy(_bulletsToRemove[i].gameObject);
            }
            _bulletsToRemove.Clear();
        }
    }

    /// <summary>
    /// 아군 적군 상관없이 임의의 캐릭터 사망 시 이벤트
    /// </summary>
    public void OnCharacterDie(Character deadCharacter)
    {
        if(CharactersActive.Contains(deadCharacter))
        {
            if (OnAllyDie != null)
            {
                OnAllyDie(deadCharacter);
            }
            _charactersToRemove.Add(deadCharacter);
        }
        else if(EnemiesActive.Contains(deadCharacter))
        {
            if (OnEnemyDie != null)
            {
                OnEnemyDie(deadCharacter);
            }
            _enemiesToRemove.Add(deadCharacter);
        }
    }

    public void AddBullet(Bullet bullet)
    {
        BulletsActive.Add(bullet);
        bullet.Init(this);
    }

    public void RemoveBullet(Bullet bullet)
    {
        _bulletsToRemove.Add(bullet);
    }
}
