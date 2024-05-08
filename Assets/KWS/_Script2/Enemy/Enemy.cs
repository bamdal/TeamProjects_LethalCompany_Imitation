using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : EnemyBase
{
    public float loweringSpeed = 1f; // 트랩이 내려가는 속도
    public float raisingSpeed = 1f; // 트랩이 올라가는 속도
    public float trapLowerPosition = 1f; // 트랩이 내려갈 위치 (y값)
    public float gravityAcceleration = 9.8f; // 중력 가속도
    private bool isLowering = false;
    Coroutine lowerTrap = null;
    public bool IsLowering
    {
        get => isLowering;
        set
        {
            if (isLowering != value)
            {
                isLowering = value;
            }
        }
    }
    Transform childEnemy;

    Enemy_Child_KWS enemy_Child;

    //protected GameObject player; // 플레이어 오브젝트를 저장할 변수
    Player player;

    private NavMeshAgent agent;
    Rigidbody enemyRigid;

    [Range(1f, 5f)]
    public float moveSpeed = 1.0f;

    /*[Range(0.01f, 1f)]
    public float jumpHeight = 0.01f;*/

    [Range(1f, 10f)]
    public float stopDistance = 1.0f;

    [Range(1f, 10f)]
    public float rotationSpeed = 10.0f;
    
    private void Awake()
    {
        childEnemy = transform.GetChild(0);       // 0번째 자식 Enemy
        enemy_Child = childEnemy.GetComponent<Enemy_Child_KWS>();
        enemyRigid = childEnemy.GetComponent<Rigidbody>();
        Collider collider = transform.GetComponent<Collider>();  // 플레이어를 탐지할 콜라이더

    }

    protected override void Start()
    {
        base.Start();
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed; // 이동 속도 설정
        //player = GameObject.FindWithTag("Player");
        player = GameManager.Instance.Player;
        agent.stoppingDistance = stopDistance;
        State = EnemyState.Stop;
    }

    protected override void Update()
    {
        base.Update();
        //bool isGrounded = enemy_Child.IsGrounded();
        //onEnemyStateUpdate();
    }    

    private void FixedUpdate()
    {
        onEnemyStateUpdate();
    }

    protected override void Update_Stop()
    {
        // 자식 오브젝트인 Enemy_Child_KWS의 IsGrounded 메서드를 호출하여 적이 땅에 있는지 판단
        bool isGrounded = enemy_Child.IsGrounded();
        Debug.Log("들어옴");
        Debug.Log($"{isGrounded}");

        if(isGrounded)
        {
            StartCoroutine(enemy_Child.RaiseTrap());
        }
    }

    protected override void Update_Patrol()
    {

    }

    protected override void Update_Chase()
    {
        if (player != null)
        {
            // 플레이어를 향해 회전
            Vector3 direction = player.transform.position - transform.position;
            direction.y = 0; // y축 회전 방지
            //transform.rotation = Quaternion.LookRotation(direction);

            // 목표 회전 각도를 계산
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 회전 속도에 따라 부드럽게 회전
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            // 플레이어와의 거리 계산
            float distance = Vector3.Distance(transform.position, player.transform.position);

            // 플레이어와의 거리가 일정 범위 이상이면 이동
            if (distance > agent.stoppingDistance)
            {
                // 플레이어를 향해 이동
                agent.SetDestination(player.transform.position);
            }
            else
            {
                // 적이 플레이어 근처에 있을 때 가해지던 힘 제거
                agent.velocity = Vector3.zero;

                // 플레이어가 가까이 있을 때는 멈춤
                agent.ResetPath();
            }
        }
    }

    protected override void Update_Attack()
    {

    }

    protected override void Update_Die()
    {
        base.Update_Die();
    }

    /*private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("바닥과 충돌");
        if (collision.gameObject.CompareTag("Ground"))
        {
            Jump();
        }
    }*/

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!IsLowering)
            {
                if (lowerTrap != null)
                {
                    StopCoroutine(lowerTrap);
                }
                lowerTrap = StartCoroutine(LowerTrap());
            }
            State = EnemyState.Chase;
        }
    }

    IEnumerator LowerTrap()
    {
        IsLowering = true;
        float currentSpeed = loweringSpeed;

        while (childEnemy.position.y > trapLowerPosition)
        {
            currentSpeed += gravityAcceleration * Time.deltaTime;
            float newY = childEnemy.position.y - currentSpeed * Time.deltaTime;
            childEnemy.position = new Vector3(childEnemy.position.x, Mathf.Max(newY, trapLowerPosition), childEnemy.position.z);
            yield return null;
        }
    }
}