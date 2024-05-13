using UnityEngine;
using UnityEngine.AI;

public class Enemy_KWS : MonoBehaviour
{
    // 몬스터의 상태를 나타내는 열거형
    public enum State
    {
        IDLE,
        CHASE,
        ATTACK,
        DIE
    }

    public float attackRange = 1.5f; // 공격 범위
    public float attackCooldown = 3f; // 공격 쿨다운 시간

    private State currentState = State.IDLE; // 몬스터의 현재 상태
    private Transform target; // 추적할 대상 (플레이어)
    private NavMeshAgent agent; // NavMeshAgent 컴포넌트
    private float lastAttackTime; // 마지막 공격 시간 기록 변수
    private bool isWalking = false;
    private bool needToIdle = false;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player").transform; // 플레이어를 추적 대상으로 설정
        agent = GetComponent<NavMeshAgent>(); // NavMeshAgent 컴포넌트 가져오기
    }

    private void Update()
    {
        // 현재 상태에 따라 적절한 동작 수행
        switch (currentState)
        {
            case State.IDLE:
                Idle();
                break;
            case State.CHASE:
                Chase();
                break;
            case State.ATTACK:
                Attack();
                break;
            case State.DIE:
                Die();
                break;
        }

        // 다음 Update에서 Idle() 메서드를 호출
        if (needToIdle)
        {
            needToIdle = false;
            Idle();
        }
    }

    private void Idle()
{
    // 플레이어가 일정 범위 내에 있으면 추적 상태로 변경
    if (Vector3.Distance(transform.position, target.position) <= attackRange)
    {
        currentState = State.CHASE;
        Debug.Log("Chase");
    }
    else
    {
        if (!isWalking)
        {
            // 새로운 랜덤 방향 선택
            Vector3 randomDirection = Random.insideUnitCircle.normalized;
            Vector3 targetPosition = transform.position + new Vector3(randomDirection.x, 0, randomDirection.y) * 10f;
            NavMeshHit hit;
            NavMesh.SamplePosition(targetPosition, out hit, 10f, NavMesh.AllAreas);
            agent.SetDestination(hit.position);

            isWalking = true; // 이동 시작
        }
        else if (!agent.pathPending && agent.remainingDistance <= 0.1f)
        {
            isWalking = false; // 이동 끝
        }
    }
}



    private void Chase()
    {
        // NavMeshAgent를 이용하여 플레이어를 추적
        agent.SetDestination(target.position);

        // 공격 범위에 도달하면 공격 상태로 변경
        if (Vector3.Distance(transform.position, target.position) <= attackRange)
        {
            currentState = State.ATTACK;
        }

        // 추적 중에 속도 높이기
        agent.speed = 3.0f;
    }

    private void Attack()
    {
        // 일정 시간 간격으로 공격
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // 공격 코드 추가
            Debug.Log("Attack!");
            lastAttackTime = Time.time;
        }

        // 공격 범위를 벗어나면 추적 상태로 변경
        if (Vector3.Distance(transform.position, target.position) > attackRange)
        {
            currentState = State.CHASE;
        }
    }

    private void Die()
    {
        // 사망 처리 코드 추가
        Debug.Log("I'm dead!");
        Destroy(gameObject);

        // 몬스터가 사망하면 속도를 원래대로 복구
        agent.speed = 1.0f;
    }
}
