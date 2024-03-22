using Cinemachine;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    /// <summary>
    /// 플레이어 체력
    /// </summary>
    public float hp = 100.0f;

    /// <summary>
    /// 플레이어 기력
    /// </summary>
    public float stamina = 100.0f;

    /// <summary>
    /// 플레이어 현재 기력
    /// </summary>
    private float currnetStamina = 0.0f;

    /// <summary>
    /// 걷는 속도
    /// </summary>
    public float walkSpeed = 3.0f;

    /// <summary>
    /// 달리는 속도
    /// </summary>
    public float runSpeed = 5.0f;

    /// <summary>
    /// 플레이어 점프 높이
    /// </summary>
    public float jumpForce = 5.0f;

    /// <summary>
    /// 걷는 동안의 스테미나 회복 속도
    /// </summary>
    private float staminaRecoveryRate = 10.0f;

    /// <summary>
    /// 달리는 동안의 스테미나 소모 속도
    /// </summary>
    private float staminaConsumptionRate = 10.0f;

    /// <summary>
    /// 스테미나 회복가능상태인지 여부
    /// </summary>
    bool isCanRecovery = true;

    /// <summary>
    /// 현재 속도
    /// </summary>
    float currentSpeed = 0.0f;

    /// <summary>
    /// 이동 모드
    /// </summary>
    enum MoveMode
    {
        Walk = 0,   // 걷기 모드
        Run         // 달리기 모드
    }

    Vector3 mouseDir = Vector3.zero;

    Quaternion cameraRotation = Quaternion.identity;

    Transform inventory;

    /// <summary>
    /// 현재 이동 모드
    /// </summary>
    MoveMode currentMoveMode = MoveMode.Run;

    /// <summary>
    /// 현재 이동 모드 확인 및 설정용 프로퍼티
    /// </summary>
    MoveMode CurrentMoveMode
    {
        get => currentMoveMode;
        set
        {
            currentMoveMode = value;    // 상태 변경
            if (currentSpeed > 0.0f)     // 이동 중인지 아닌지 확인
            {
                // 이동 중이면 모드에 맞게 속도와 애니메이션 변경
                MoveSpeedChange(currentMoveMode);
            }
        }
    }

    /// <summary>
    /// 입력된 이동 방향
    /// </summary>
    Vector3 inputDirection = Vector3.zero;  // y는 무조건 바닥 높이

    /// <summary>
    /// 캐릭터 회전 속도
    /// </summary>
    public float turnSpeed = 10.0f;

    // 컴포넌트들
    CharacterController characterController;


    // 입력용 인풋 액션
    PlayerInputActions inputActions;
    public Camera cam;

    /// <summary>
    /// 이동방향
    /// </summary>
    Vector3 moveDirection;
    //Transform itemRaderTransform;

    Collider itemRader;
    public float groundCheckDistance = 1f;    // 바닥 체크 거리
    public LayerMask groundLayer;               // 바닥을 나타내는 레이어
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        inputActions = new PlayerInputActions();
        Transform parentTransform = transform.parent;
        Transform camera = parentTransform.Find("Main Camera");
        cam = camera.GetComponent<Camera>();
        inventory = transform.Find("Inventory");
        currnetStamina = stamina;


        Collider[] colliders = GetComponents<Collider>();
        itemRader = colliders[1];
        //itemRaderTransform = transform.Find("ItemRader");
        //itemRader = itemRaderTransform.GetComponent<Collider>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.MoveModeChange.performed += OnMoveModeChange;
        inputActions.Player.Interact.performed += OnInteract;
        inputActions.Player.Interact.canceled += OnInteract;
        inputActions.Player.MouseLClick.performed += OnLClick;
        inputActions.Player.MouseRClick.performed += OnRClick;
        inputActions.Player.MouseRClick.canceled += OnRClick;
        inputActions.Player.Jump.performed += OnJump;
    }


    private void OnDisable()
    {
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.MouseRClick.canceled -= OnRClick;
        inputActions.Player.MouseRClick.performed -= OnRClick;
        inputActions.Player.MouseLClick.performed -= OnLClick;
        inputActions.Player.Interact.canceled -= OnInteract;
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.MoveModeChange.performed -= OnMoveModeChange;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Disable();
    }

    private void FixedUpdate()
    {
        // 이동 방향 계산
        moveDirection = CalculateMoveDirection();

        // 이동 처리
        characterController.Move(Time.deltaTime * currentSpeed * moveDirection);

        // 아이템을 상호작용하는 함수 호출
        FindItemRay();
    }
    private void Update()
    {
        // 걷기 or 달리기 상태일때 스테미나 회복 및 감소
        if (CurrentMoveMode == MoveMode.Run && currnetStamina > 1 && currentSpeed > 0)
        {
            ConsumeStamina();
        }
        //else if ((CurrentMoveMode == MoveMode.Walk || currentSpeed < 0.1f) && isCanRecovery)
        //{
        //    RecoverStamina();
        //}
        else if (CurrentMoveMode == MoveMode.Walk && isCanRecovery)
        {
            RecoverStamina();
        }
        else
        {
            currentMoveMode = MoveMode.Walk;
        }

        Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 raycastStart = transform.position;
        Vector3 raycastDirection = Vector3.down;

        // 레이캐스트 시작점에서 아래쪽으로 레이를 쏩니다.
        Gizmos.DrawLine(raycastStart, raycastStart + raycastDirection * groundCheckDistance);
    }

    void EnableStaminaRecovery()
    {
        isCanRecovery = true;
    }


    /// <summary>
    /// 바라보고있는 오브젝트가 아이템이면 반응하는 함수
    /// </summary>
    private void FindItemRay()
    {
        // LayerMask를 설정하여 item 레이어만 검출하도록 합니다.
        int layerMask = 1 << LayerMask.NameToLayer("Item");

        // 카메라의 정 중앙을 기준으로 레이를 쏩니다.
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        // Physics.Raycast 메서드에 layerMask를 추가하여 해당 레이어만 검출하도록 합니다.
        if (Physics.Raycast(ray, out hit, 5.0f, layerMask))
        {


            // 레이를 그려줍니다.
            Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
        }
        else
        {


            // 실패 시에도 레이를 그려줍니다.
            Debug.DrawRay(ray.origin, ray.direction * 5.0f, Color.red);
        }
    }


    /// <summary>
    /// 카메라의 방향을 기준으로 이동 방향을 계산합니다.
    /// </summary>
    /// <returns>이동 방향</returns>
    private Vector3 CalculateMoveDirection()
    {
        Vector3 cameraForward = cam.transform.forward;
        Vector3 cameraRight = cam.transform.right;
        cameraForward.y = 0f; // y 값을 무시하여 플레이어의 높이를 고정합니다.
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * inputDirection.z + cameraRight * inputDirection.x;
        moveDirection.Normalize();

        return moveDirection;
    }





    /// <summary>
    /// 이동 입력 처리용 함수
    /// </summary>
    /// <param name="context"></param>
    private void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Vector3 input = context.ReadValue<Vector2>();
        inputDirection.x = input.x;     // 입력 방향 저장
        inputDirection.z = input.y;

        if (!context.canceled)
        {
            MoveSpeedChange(CurrentMoveMode);
        }
        else
        {
            // 입력을 끝낸 상황
            currentSpeed = 0.0f;    // 정지 시키기
        }
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log(IsGrounded());
            // 점프 가능한 상황에서만 점프를 실행합니다.
            /*Debug.Log("호출됨");
            moveDirection.y = jumpForce; // 캐릭터에 점프 힘을 적용합니다.
            Debug.Log(moveDirection.y);*/
        }
    }

    private bool IsGrounded()
    {
        // 캐릭터의 아래쪽에 레이캐스트를 쏴서 바닥과 충돌하는지 확인
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }


    /// <summary>
    /// 이동 모드 변경용 함수
    /// </summary>
    /// <param name="context"></param>
    private void OnMoveModeChange(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (CurrentMoveMode == MoveMode.Walk && currnetStamina > 0.0f)
        {
            CurrentMoveMode = MoveMode.Run;
        }
        else
        {
            CurrentMoveMode = MoveMode.Walk;
        }
    }

    /// <summary>
    /// 달리는 동안 스테미나 소모하는 함수
    /// </summary>
    void ConsumeStamina()
    {
        // 달리는 동안 스테미나 소모
        currnetStamina -= staminaConsumptionRate * Time.deltaTime;
        if (currnetStamina < 1.0f)
        {
            currnetStamina = 0.0f;
            CurrentMoveMode = MoveMode.Walk;
            isCanRecovery = false;
            Debug.Log("3초간 스테미나를 회복할 수 없습니다.");
            Invoke(nameof(EnableStaminaRecovery), 3.0f);
        }
    }


    /// <summary>
    /// 걷는 동안 스테미나 회복
    /// </summary>
    void RecoverStamina()
    {
        currnetStamina += staminaRecoveryRate * Time.deltaTime;
        if (currnetStamina > stamina)
        {
            currnetStamina = stamina;
        }
    }

    /// <summary>
    /// 모드에 따라 이동 속도를 변경하는 함수
    /// </summary>
    /// <param name="mode">설정된 모드</param>
    void MoveSpeedChange(MoveMode mode)
    {
        switch (mode) // 이동 모드에 따라 속도와 애니메이션 변경
        {
            case MoveMode.Walk:
                currentSpeed = walkSpeed;
                break;
            case MoveMode.Run:
                currentSpeed = runSpeed;
                break;
        }
    }

    /// <summary>
    /// 상호작용 처리용 함수
    /// </summary>
    /// <param name="context"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("f키 눌렀음!");

            // 카메라의 정 중앙을 기준으로 레이를 쏩니다.
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 3.0f))
            {
                if (hit.collider.CompareTag("Item"))
                {
                    Debug.Log(hit);
                    // 충돌한 객체를 자식으로 만듭니다.
                    hit.collider.transform.SetParent(inventory);

                    // 자식으로 만든 객체를 비활성화합니다.
                    hit.collider.gameObject.SetActive(false);
                    Debug.Log("아이템을 획득했습니다!");

                }
            }
            else
            {
                Debug.Log("레이캐스트 실패!");
            }
        }
        if (context.canceled)
        {
            Debug.Log("f키 떨어짐!");
        }
    }
    List<Transform> itemTransforms = new List<Transform>();
    private void OnLClick(InputAction.CallbackContext context)
    {
    }

    private void OnRClick(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            itemRader.enabled = true;
            Quaternion cameraRotation = Camera.main.transform.rotation;

            Vector3 currentRotation = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(currentRotation.x, cameraRotation.eulerAngles.y, currentRotation.z);
            StopAllCoroutines();
            StartCoroutine(DisableItemRaderAfterDelay());
            Debug.Log("아이템 목록:");
        }
    }

    private IEnumerator DisableItemRaderAfterDelay()
    {
        // 일정 시간(예: 1초) 후에 itemRader.enabled를 false로 변경
        yield return new WaitForSeconds(0.05f); // 변경하고자 하는 시간으로 수정 가능
        itemRader.enabled = false;
    }





    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Item"))
        {
            Transform itemTransform = collision.transform;
            Vector3 itemPosition = itemTransform.position;

            // 아이템의 위치와 플레이어의 위치 사이의 방향 벡터를 구합니다.
            Vector3 directionToItem = itemPosition - transform.position;

            // 플레이어 위치에서 아이템까지의 레이를 생성합니다.
            Ray ray = new Ray(transform.position + transform.forward * 0.5f, directionToItem);

            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                // 레이가 충돌한 객체가 벽인지 확인합니다.
                if (hit.collider.CompareTag("Obstacle"))
                {
                    // 벽 뒤에 있는 아이템을 제거합니다.
                    Debug.Log("벽 뒤에 있는 아이템: " + itemTransform.gameObject.name);
                }
                else if (hit.collider.CompareTag("Item"))
                {
                    // 벽 뒤에 없는 아이템을 목록에 추가합니다.
                    itemTransforms.Add(itemTransform);
                }
            }
            else
            {
                // 레이가 아이템에 닿지 않은 경우 아이템을 목록에 추가합니다.
                itemTransforms.Add(itemTransform);
            }

            foreach (Transform item in itemTransforms)
            {
                Debug.Log(item.gameObject.name);
            }
            itemTransforms.Clear();
        }


    }


}