using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;
using System;

namespace Logic
{
    public class BattleLogic
    {
        // 상수
        readonly List<Position2> SpawnPoint = new List<Position2>{
            new Position2(-3, 0),
            new Position2(-1, 0),
            new Position2(1, 0),
            new Position2(3, 0)
        };
        public readonly int ExCostGaugePerCount = 100000;     // 코스트 갯수 1개를 증가시키기 위해 필요한 충전량.

        [Title("전투 초기화 정보")]
        public BattleData BattleData;
        public CharacterPrefabDatabase CharacterViewDatabase;
        public GameObject EnemyPrefab;
        public GameObject BulletPrefab;
        public GameObject PathFinderPrefab;

        [Title("전투 씬 상태")]
        public int BaseLogicTickrate = 30;
        public BattleSceneState BattleState;

        [Title("관리중인 엔티티들(로직)")]
        public CharacterGroup CharactersLogic = new CharacterGroup();      // 아군
        List<CharacterLogic> _charactersToRemove = new List<CharacterLogic>();
        public CharacterGroup EnemiesLogic = new CharacterGroup();          // 적군
        List<CharacterLogic> _enemiesToRemove = new List<CharacterLogic>();
        public List<ObstacleLogic> Obstacles = new List<ObstacleLogic>();
        public List<BulletLogic> BulletsActive = new List<BulletLogic>();
        List<BulletLogic> _bulletsToRemove = new List<BulletLogic>();

        [Title("EX 스킬 관련")]
        public int ExCostCount = 0;         // 현재 코스트 갯수. Ex 스킬 사용에 필요.
        public int ExCostRecharging = 0;    // 현재 코스트 충전량. 이 값이 최대치가 되면 ExCostCount가 1 증가.
        public int ExCostRegen = 0;         // 틱 당 코스트 회복량. 캐릭터 코스트 회복량의 총합.
        public List<CharacterLogic> skillCardHand = new List<CharacterLogic>();       // 패. 최대 3장
        public LinkedList<CharacterLogic> skillCardDeck = new LinkedList<CharacterLogic>(); // 덱. 패에 들고 있지 않은 모든 스킬카드.

        [Title("카메라 관리")]
        CameraTargetGroupControl cameraTargetGroup;

        // 전투 중 발생하는 이벤트들
        public Action OnBattleBegin;
        public delegate void CharacterInstanceEvent(CharacterLogic characterLogic);
        public CharacterInstanceEvent OnAllySpawn;
        public CharacterInstanceEvent OnEnemySpawn;
        public CharacterInstanceEvent OnAllyDie;
        public CharacterInstanceEvent OnEnemyDie;
        public delegate void BulletInstacneEvent(BulletLogic bulletLogic);
        public BulletInstacneEvent OnBulletSpawned;
        public BulletInstacneEvent OnBulletExpired;

        public void Init(BattleData battleData)
        {
            this.BattleData = battleData;
            BattleState = BattleSceneState.InBattle;

            // battleData에 기록된 캐릭터들을 스폰
            // 아군
            for(int i=0; i<battleData.characters.Count; i++)
            {
                CharacterLogic newCharacter = AddCharacter(battleData.characters[i], battleData.characterStats[i]);
                newCharacter.SetPosition(SpawnPoint[i]);
            }

            // 적군
            // TODO: battleData에 기록된 적 웨이브 스폰하는 것으로 대체하기
            for(int i=0; i<EnemiesLogic.Count; i++)
            {
                EnemiesLogic[i].SetBattleLogicReference(this);
                if(OnEnemySpawn != null)
                {
                    OnEnemySpawn(EnemiesLogic[i]);
                }
            }

            // 코스트 회복량 산정
            foreach(var character in CharactersLogic)
            {
                ExCostRegen += character.CostRegen;
            }

            // 스킬카드 덱 구성 & 최대 3장 드로우
            foreach(int i in Enumerable.Range(0, CharactersLogic.Count).OrderBy(x => UnityEngine.Random.Range(0,1)))
            {
                skillCardDeck.AddLast(CharactersLogic[i]);
            }
            for(int i=0; i< Mathf.Min(skillCardDeck.Count, 3); i++)
            {
                DrawSkillCard();
            }
            OnAllyDie += RemoveSkillCardFromDeck;

            // 초기화 완료 후, 게임 루프 시작 전에 이벤트 호출
            if (OnBattleBegin != null)
            {
                OnBattleBegin();
            }
        }

        public void Tick()
        {
            // 게임 오버 조건 체크
            if(CharactersLogic.Count <= 0)
            {
                BattleState = BattleSceneState.lose;
                return;
            }

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
            foreach(var character in CharactersLogic)
            {
                character.Tick();
            }
            foreach(var bullet in BulletsActive)
            {
                bullet.Tick();
            }

            // 삭제해야할 엔티티 정리
            RemoveInactiveEntities();
        }

        public bool GetIfSomeAllyDoingAction()
        {
            bool answer = false;
            foreach(var character in CharactersLogic)
            {
                answer |= character.IsDoingSomeAction;
            }
            return answer;
        }

        void DrawSkillCard()
        {
            skillCardHand.Add(skillCardDeck.First.Value);
            skillCardDeck.RemoveFirst();
        }

        void RemoveSkillCardFromDeck(CharacterLogic toRemove)
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
            CharacterLogic character = skillCardHand[index];
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

            ExCostCount -= character.ExSkillCost;
            character.TriggerExSkill();

            // 스킬 카드를 덱의 맨 밑으로 넣고 한 장 드로우
            skillCardHand.Remove(character);
            skillCardDeck.AddLast(character);
            DrawSkillCard();

            return;
        }

        public CharacterLogic AddCharacter(CharacterData characterData, CharacterStatData characterStat)
        {
            // 캐릭터(로직) 생성
            CharacterLogic characterLogic = new CharacterLogic();

            // 나머지 초기화 진행
            characterLogic.Init(this, characterData, characterStat);
            CharactersLogic.Add(characterLogic);

            if(OnAllySpawn != null)
            {
                OnAllySpawn(characterLogic);
            }
            return characterLogic;
        }

        /// <summary>
        /// Character에 의해 스폰된 총알을 관리 대상에 추가
        /// </summary>
        public void AddBullet(BulletLogic bullet)
        {
            BulletsActive.Add(bullet);
            bullet.Init(this);
            if(OnBulletSpawned != null)
            {
                OnBulletSpawned(bullet);
            }
        }

        /// <summary>
        /// Tick 안의 루프에서 엔티티가 삭제/추가되면 안되므로 삭제해야 할 엔티티들은 Tick 후에 따로 삭제
        /// </summary>
        void RemoveInactiveEntities()
        {
            if (_charactersToRemove.Count > 0)
            {
                for (int i = _charactersToRemove.Count - 1; i >= 0; i--)
                {
                    CharactersLogic.Remove(_charactersToRemove[i]);
                }
                _charactersToRemove.Clear();
            }
            if (_enemiesToRemove.Count > 0)
            {
                for(int i = _enemiesToRemove.Count - 1; i>=0; i--)
                {
                    EnemiesLogic.Remove(_enemiesToRemove[i]);
                }
                _enemiesToRemove.Clear();
            }
            if (_bulletsToRemove.Count > 0)
            {
                for (int i = _bulletsToRemove.Count - 1; i >= 0; i--)
                {
                    BulletsActive.Remove(_bulletsToRemove[i]);
                }
                _bulletsToRemove.Clear();
            }
        }

        /// <summary>
        /// 아군 적군 상관없이 임의의 캐릭터 사망 시 이벤트
        /// </summary>
        public void RemoveDeadCharacter(CharacterLogic deadCharacter)
        {
            // 아군일 경우
            if(CharactersLogic.Contains(deadCharacter))
            {
                if (OnAllyDie != null)
                {
                    OnAllyDie(deadCharacter);
                }
                _charactersToRemove.Add(deadCharacter);
            }
            // 적군일 경우
            else if(EnemiesLogic.Contains(deadCharacter))
            {
                if (OnEnemyDie != null)
                {
                    OnEnemyDie(deadCharacter);
                }
                _enemiesToRemove.Add(deadCharacter);
            }
        }

        /// <summary>
        /// 총알 수명 다했을 때 없애기
        /// </summary>
        public void RemoveExpiredBullet(BulletLogic expiredBullet)
        {
            _bulletsToRemove.Add(expiredBullet);
            if(OnBulletExpired != null)
            {
                OnBulletExpired(expiredBullet);
            }
        }
    }
}