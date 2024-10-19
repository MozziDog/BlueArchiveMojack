using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Logic;

public class BattleSceneManager : MonoBehaviour
{
    [Title("전투 초기화 정보")]
    public BattleData BattleData;
    public BattleLogic BattleLogic;
    public CharacterPrefabDatabase CharacterViewDatabase;
    public GameObject EnemyPrefab;
    public GameObject BulletPrefab;
    public GameObject PathFinderPrefab;

    [Title("전투 씬 상태")]
    [SerializeField] int _logicTickPerSecond = 30;

    [Title("관리중인 비주얼 엔티티들")]
    public List<CharacterVisual> CharacterVisuals = new List<CharacterVisual>();
    public List<CharacterVisual> EnemyVisuals = new List<CharacterVisual>();
    public List<ObstacleVisual> ObstacleVisuals = new List<ObstacleVisual>();
    public List<BulletVisual> BulletVisuals = new List<BulletVisual>();

    // 캐릭터 비주얼 추가/삭제 이벤트
    public delegate void CharacterVisualEvent(CharacterVisual visual);
    public CharacterVisualEvent OnCharacterVisualSpawn;
    public CharacterVisualEvent OnCharacterVisualDestroy;
    public CharacterVisualEvent OnEnemyVisualSpawn;
    public CharacterVisualEvent OnEnemyVisualDestroy;
    public delegate void BulletVisualEvent(BulletVisual visual);
    public BulletVisualEvent OnBulletVisualSpawn;
    public BulletVisualEvent OnBulletVisualDestroy;

    // Start is called before the first frame update
    void Start()
    {
        BattleLogic = new BattleLogic();

        // 이벤트 등록
        BattleLogic.OnAllySpawn += SpawnCharacterVisual;
        BattleLogic.OnAllyDie += DestroyCharacterVisual;
        // BattleLogic.OnEnemySpawn += SpawnEnemyVisual;
        BattleLogic.OnEnemyDie += DestroyEnemyVisual;
        BattleLogic.OnBulletSpawned += SpawnBulletVisual;
        BattleLogic.OnBulletExpired += DestroyBulletVisual;

        // 테스트용: 사전에 배치된 적 관리 대상에 등록
        AddExistEnemies(BattleLogic);
        AddExistObstacles(BattleLogic);

        // 전투 시작
        BattleLogic.Init(BattleData);
        StartCoroutine(GameCoroutine(BattleData));
    }

    void AddExistEnemies(BattleLogic battleLogic)
    {
        foreach(var enemy in EnemyVisuals)
        {
            battleLogic.EnemiesLogic.Add(enemy.CharacterLogic);
        }
    }

    void AddExistObstacles(BattleLogic battleLogic)
    {
        foreach(var obstacle in ObstacleVisuals)
        {
            battleLogic.Obstacles.Add(obstacle.ObstacleLogic);
        }
    }

    void SpawnCharacterVisual(Character newCharacter)
    {
        // 캐릭터(비주얼) 생성
        GameObject characterVisualObject = Instantiate(CharacterViewDatabase.CharacterViews[newCharacter.Name]);
        CharacterVisual characterVisualComponent = characterVisualObject.GetComponent<CharacterVisual>();

        // Visual 초기화 진행
        characterVisualObject.transform.position = Position2ToVector3(newCharacter.Position);
        characterVisualComponent.CharacterLogic = newCharacter;
        CharacterVisuals.Add(characterVisualComponent);

        // PathFinder 설정
        // TODO: PathFinder를 직접 구현한 길찾기 에이전트로 대체, Logic 레이어로 분리
        PathFinder pathFinder = characterVisualObject.GetComponentInChildren<PathFinder>();
        newCharacter.pathFinder = pathFinder;
        pathFinder.CharacterLogic = newCharacter;

        if(OnCharacterVisualSpawn != null)
        {
            OnCharacterVisualSpawn(characterVisualComponent);
        }
    }

    void DestroyCharacterVisual(Character deadCharacter)
    {
        // 대상 CharacterVisual 찾기
        CharacterVisual deadCharacterVisual = CharacterVisuals.Find((ch) => { return ch.CharacterLogic == deadCharacter; });

        if(OnCharacterVisualDestroy != null)
        {
            OnCharacterVisualDestroy(deadCharacterVisual);
        }
        Destroy(deadCharacterVisual.gameObject);
    }

    void SpawnEnemyVisual(Character newEnemy)
    {
        // 적(비주얼) 생성
        GameObject enemyVisualObject = Instantiate(EnemyPrefab);
        CharacterVisual enemyVisualComponent = enemyVisualObject.GetComponent<CharacterVisual>();

        // 최소한의 초기화만 진행
        enemyVisualObject.transform.position = Position2ToVector3(newEnemy.Position);
        enemyVisualComponent.CharacterLogic = newEnemy;
        EnemyVisuals.Add(enemyVisualComponent);
    }

    void DestroyEnemyVisual(Character deadEnemy)
    {
        // 대상 CharacterVisual 찾기
        CharacterVisual deadEnemyVisual = EnemyVisuals.Find((ch) => { return ch.CharacterLogic == deadEnemy; });

        if(OnEnemyVisualDestroy != null)
        {
            OnEnemyVisualDestroy(deadEnemyVisual);
        }
        Destroy(deadEnemyVisual.gameObject);
    }

    void SpawnBulletVisual(Bullet newBullet)
    {
        // 새 총알 오브젝트를 생성하고 Bullet 로직과 연결
        GameObject bulletObject = Instantiate(BulletPrefab);
        BulletVisual bulletVisual = bulletObject.GetComponent<BulletVisual>();
        bulletVisual.BulletLogic = newBullet;
        BulletVisuals.Add(bulletVisual);

        if(OnBulletVisualSpawn != null)
        {
            OnBulletVisualSpawn(bulletVisual);
        }
    }

    void DestroyBulletVisual(Bullet expiredBullet)
    {
        // 대응하는 bulletVisual 찾기
        BulletVisual expiredBulletVisual = BulletVisuals.Find((bu) => { return bu.BulletLogic == expiredBullet; });
        if(OnBulletVisualDestroy != null)
        {
            OnBulletVisualDestroy(expiredBulletVisual);
        }
    }

    protected IEnumerator GameCoroutine(BattleData battleData)
    {
        // 게임 루프 진행
        while(BattleLogic.BattleState == BattleSceneState.InBattle)
        {
            BattleLogic.Tick();
            yield return new WaitForSeconds(1f / _logicTickPerSecond);
        }
        Debug.Log("게임 오버");
        yield break;
    }

    Vector3 Position2ToVector3(Position2 logicPosition)
    {
        return new Vector3(logicPosition.x, 0, logicPosition.y);
    }
}
