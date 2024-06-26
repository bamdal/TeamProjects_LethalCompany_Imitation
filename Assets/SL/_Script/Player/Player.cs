using Cinemachine;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.XInput;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEngine.Rendering.DebugUI;

public class Player : Singleton<Player>, IBattler, IHealth
{

    /// <summary>
    /// 플레이어 공격력
    /// </summary>
    public float attackPower = 10.0f;

    /// <summary>
    /// 플레이어 체력
    /// </summary>
    public float maxHp = 100.0f;

    private float currentHp = 0.0f;

    public float Hp
    {
        get => currentHp;
        set
        {
            if (currentHp != value)
            {
                if(value > 0)
                {
                    currentHp = Math.Clamp(value, 0, maxHp);
                    onHealthChange?.Invoke(Hp);
                }
                else
                {
                    currentHp = Math.Clamp(value, 0, maxHp);
                    OnDie();
                }
            }
        }
    }
    public void Die()
    {
        OnDie();
    }
    private void OnDie()
    {
        Debug.Log("사망");
        collider.enabled = false;
        ResetItemInventory();
        onDie?.Invoke();
    }

    /// <summary>
    /// 플레이어 기력
    /// </summary>
    public float maxStamina = 100.0f;

    /// <summary>
    /// 플레이어 현재 기력
    /// </summary>
    private float currentStamina = 0.0f;

    public float Stamina
    {
        get => currentStamina;
        private set
        {
            if(currentStamina != value)
            {
                currentStamina = Math.Clamp(value, 0, maxStamina);
                onStaminaChange?.Invoke(Stamina);
            }
        }
    }

    float totalWeight = 0.0f;
    float TotalWeight
    {
        get => totalWeight;
        set
        {
            if(totalWeight != value)
            {
                totalWeight = Math.Clamp(value, 0, 80.0f);
            }
        }
    }
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
    [SerializeField]
    float currentSpeed = 0.0f;

    float slowRatio = 1.0f;
    
    float SlowRatio
    {
        get => slowRatio;
        set
        {
            if(slowRatio != value)
            {
                slowRatio = value;
            }
        }
    }


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

    Transform inventoryTransform;

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

    /// <summary>
    /// 중력 가속도 변수
    /// </summary>
    private float gravityForce = -9.8f;

    // 컴포넌트들
    CharacterController characterController;

    Collider collider;

    // 입력용 인풋 액션
    PlayerInput input;

    /// <summary>
    /// 카메라를 불러오기 위한 변수
    /// </summary>
    public Camera cam;

    /// <summary>
    /// 현재 중력을 담당할 Y값
    /// </summary>
    float gravityY = 0.0f;
    /// <summary>
    /// 이동방향
    /// </summary>
    Vector3 moveDirection;


    /// <summary>
    /// 중력을 담당하는 방향 백터
    /// </summary>
    Vector3 gravityDir = Vector3.zero;

    public float groundCheckDistance = 0.2f;    // 바닥 체크 거리
    public LayerMask groundLayer;               // 바닥을 나타내는 레이어
    Transform groundCheckPosition;              // 바닥 체크할 포지션
    Transform itemRader;
    Inventory inventory;

    Transform currentItem = null;

    bool isInDungeon = false;
    public bool IsInDungeon
    {
        get => isInDungeon;
        set
        {
            if(isInDungeon !=  value)
            {
                isInDungeon = value;
            }
        }
    }
    public Transform CurrentItem
    {
        get => currentItem;
        set
        {
            if(currentItem != value)
            {
                currentItem = value;
                if(currentItem != null)
                {
                    IItemDataBase temp = currentItem.GetComponent<IItemDataBase>();
                    CurrentItemDB = temp.GetItemDB();
                }
                else
                {
                    CurrentItemDB = null;
                }
            }
        }
    }
    ItemDB currentItemDB;
    ItemDB CurrentItemDB
    {
        get => currentItemDB;
        set
        {
            if(currentItemDB != value)
            {
                currentItemDB = value;
            }
        }
    }
    int currentItemIndex = 0;
    public int CurrentItemIndex
    {
        get => currentItemIndex;
        set
        {
            if(currentItemIndex != value)
            {
                currentItemIndex = value;
            }
        }
    }

    public Action<float> onHealthChange;
    public Action<float> onStaminaChange;
    public Action onRclickIsNotPressed;
    public Action onDie;
    public Action onRefresh;
    public InventoryUI invenUI;

    CinemachineImpulseSource _source;   // 카메라 흔들림을 위한 시네머신 임펄스 소스 컴포넌트
    Volume volume;
    Vignette vignette;
    Terminal terminal;
    private void Awake()
    {
        
        DontDestroyOnLoad(gameObject);

    }

    private void OnEnable()
    {
        input = GetComponent<PlayerInput>();
        
        input.onMove += OnMoveInput;
        input.onMoveModeChange += OnMoveModeChageInput;
        input.onInteract += OnInteractInput;
        input.onJump += OnJumpInput;
        input.onLClick += OnLClickInput;
        input.onRClick += OnRClickInput;
        input.onScroll += OnScrollWheel;
        input.onItemDrop += OnItemDrop;
        
        collider = GetComponent<Collider>();
        cam = Camera.main;
        inventoryTransform = transform.GetChild(1);
        inventory = inventoryTransform.GetComponent<Inventory>();
        characterController = GetComponent<CharacterController>();
        itemRader = transform.GetChild(2);
        groundCheckPosition = transform.GetChild(4);
        invenUI = FindObjectOfType<InventoryUI>();
        Transform child = transform.GetChild(0);
        _source = child.GetComponent<CinemachineImpulseSource>();
        volume = FindObjectOfType<Volume>();
        volume.profile.TryGet(out vignette);

        Stamina = maxStamina;
        Hp = maxHp;
        gravityY = -1f;
    }
    private void OnDisable()
    {
        input.onMove -= OnMoveInput;
        input.onMoveModeChange -= OnMoveModeChageInput;
        input.onInteract -= OnInteractInput;
        input.onJump -= OnJumpInput;
        input.onLClick -= OnLClickInput;
        input.onRClick -= OnRClickInput;
        input.onScroll -= OnScrollWheel;
        input.onItemDrop -= OnItemDrop;
    }
    private void Start()
    {
        terminal = FindAnyObjectByType<Terminal>();
        CurrentItem = null;
        if(terminal != null)
        {
            terminal.onRequest += OnInputAction;
        }
        itemRader.gameObject.SetActive(false);
    }



    private void Update()
    {
        // 걷기 or 달리기 상태일때 스테미나 회복 및 감소
        if (CurrentMoveMode == MoveMode.Run && Stamina > 0 && currentSpeed > 0)
        {
            ConsumeStamina();
        }
        else if (CurrentMoveMode == MoveMode.Walk && isCanRecovery)
        {
            RecoverStamina();
        }
        else
        {
            currentMoveMode = MoveMode.Walk;
        }
        // 이동 방향 계산
        CalculateMoveDirection();
        // 플레이어가 바라보는 방향
        Vector3 playerForward = transform.forward;
        // 입력 방향의 크기만큼 이동 방향 설정
        Vector3 moveDirection = playerForward * inputDirection.z + transform.right * inputDirection.x;
        moveDirection.Normalize();          // 이동 방향을 정규화하여 일정한 속도로 이동하도록 함
        gravityDir.y = gravityY;            // 중력을 담당하는 방향백터에 y값에 중력을 넣음
        ApplyGravity();                     // 공중일때 중력 적용하는 함수
        // 이동 처리
        characterController.Move(currentSpeed * Time.deltaTime * moveDirection);
        // 중력 처리
        characterController.Move(1f * Time.deltaTime * gravityDir);
    }

    private void FixedUpdate()
    {
        // 아이템을 상호작용하는 함수 호출
        FindItemRay();
        inventoryTransform.forward = ItemRotation();
    }

    /// <summary>
    /// 스테미나 회복이 가능함을 담당하는 변수를 바꾸는 함수
    /// </summary>
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
    private void CalculateMoveDirection()
    {
        // 카메라가 바라보는 방향
        Vector3 cameraForward = Camera.main.transform.forward;
        // Y축은 고려하지 않음
        cameraForward.y = 0f;
        // 정규화하여 방향 벡터를 얻음
        cameraForward.Normalize();

        // 플레이어가 바라볼 방향 설정
        transform.forward = cameraForward;
    }

    Vector3 ItemRotation()
    {
        return (Camera.main.transform.forward).normalized;
    }
    /// <summary>
    /// 이동 입력 처리용 함수
    /// </summary>
    /// <param name="context"></param>
    private void OnMoveInput(Vector2 input, bool isPress)
    {
        inputDirection.x = input.x;     // 입력 방향 저장
        inputDirection.y = 0;
        inputDirection.z = input.y;

        if (isPress)
        {
            MoveSpeedChange(CurrentMoveMode);
        }
        else
        {
            // 입력을 끝낸 상황
            currentSpeed = 0.0f;    // 정지 시키기
        }
    }

    /// <summary>
    /// 점프 입력에 대한 델리게이트로 실행되는 함수
    /// </summary>
    private void OnJumpInput()
    {
        if (IsGrounded())
        {
            gravityY = 5f;
        }
    }

    /// <summary>
    /// 현재 지금 땅 위인지 확인하는 함수
    /// </summary>
    /// <returns></returns>
    bool IsGrounded()
    {
        // 캐릭터의 아래에 레이캐스트를 쏴서 바닥에 닿았는지 확인
        return Physics.Raycast(groundCheckPosition.position, Vector3.down, groundCheckDistance, layerMask: groundLayer);
    }

    /// <summary>
    /// 공중일때 중력 적용하는 함수
    /// </summary>
    void ApplyGravity()
    {
        if (!IsGrounded())
        {
            gravityY += gravityForce * Time.deltaTime;
        }
    }



    /// <summary>
    /// 이동 모드 변경 입력에 대한 델리게이트로 실행되는 함수
    /// </summary>
    private void OnMoveModeChageInput()
    {
        if (CurrentMoveMode == MoveMode.Walk)
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
        Stamina -= staminaConsumptionRate * Time.deltaTime;
        if (Stamina < 0.1f)
        {
            Stamina = 0.0f;
            CurrentMoveMode = MoveMode.Walk;
            isCanRecovery = false;
            Invoke(nameof(EnableStaminaRecovery), 3.0f);
        }
    }


    /// <summary>
    /// 걷는 동안 스테미나 회복
    /// </summary>
    void RecoverStamina()
    {
        Stamina += staminaRecoveryRate * Time.deltaTime;
        if (Stamina > maxStamina)
        {
            Stamina = maxStamina;
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
                currentSpeed = walkSpeed * SlowRatio;
                break;
            case MoveMode.Run:
                currentSpeed = runSpeed * SlowRatio;
                break;
        }
    }

    /// <summary>
    /// 상호작용 처리용 함수
    /// </summary>
    /// <param name="context"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void OnInteractInput(bool isClick)
    {
        if (isClick)
        {
            // 카메라의 정 중앙을 기준으로 레이를 쏩니다.
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 3.0f))
            {
                if (hit.collider.CompareTag("Item") || hit.collider.CompareTag("Hardware") || hit.collider.CompareTag("Weapon"))
                {
                    for (int i = 0; i < inventory.InvenSlots.Length; i++)
                    {
                        if (inventory.InvenSlots[i].childCount == 0)
                        {
                            // 아이템을 인벤토리 슬롯에 넣습니다.
                            Transform itemTransform = hit.collider.transform;
                            itemTransform.SetParent(inventory.InvenSlots[i]);
                            TotalWeight += itemTransform.GetComponent<IItemDataBase>().GetItemDB().weight;
                            SlowRatio = (0.01f * (100 - TotalWeight));

                            itemTransform.localPosition = new Vector3(0.0f, 0.0f, 1.0f); // 포지션을 (0, 0, 0)으로 설정합니다.
                            itemTransform.localRotation = Quaternion.Euler(0, 0, 0);
                            invenUI.ItemImages[i].color = new(1, 1, 1, 1);
                            Collider itemCollider = hit.collider.GetComponent<Collider>();
                            if (itemCollider != null)
                                itemCollider.enabled = false;

                            Rigidbody itemRigidbody = hit.collider.GetComponent<Rigidbody>();
                            if (itemRigidbody != null)
                                itemRigidbody.isKinematic = true;
                            hit.collider.gameObject.SetActive(false);
                            if (inventory.InvenSlots[CurrentItemIndex] != null && inventory.InvenSlots[CurrentItemIndex].childCount > 0)
                            {
                                inventory.InvenSlots[CurrentItemIndex].GetChild(0).gameObject.SetActive(true);

                            }
                            break;
                        }

                    }
                    OnScrollWheel(new Vector2(0, 0));
                    for (int j = 0; j < 4; j++)
                    {
                        if (inventory.InvenSlots[j].childCount > 0)
                        {
                            Transform tempItem = inventory.InvenSlots[j].GetChild(0);
                            if (tempItem != null)
                            {
                                IItemDataBase itemData = tempItem.GetComponent<IItemDataBase>();
                                if (itemData != null)
                                {
                                    inventory.ItemDBs[j] = itemData.GetItemDB();
                                    invenUI.ItemImages[j].sprite = inventory.ItemDBs[j].itemIcon;
                                }
                                
                            }
                        }
                        else
                        {
                            invenUI.ItemImages[j].sprite = null;
                        }
                    }

                }
                IInteraction interaction = hit.collider.gameObject.GetComponent<IInteraction>();

                // 상호작용이 가능한 물체일때 
                if (interaction != null)
                {
                    // 상호작용 
                    interaction.Interaction(transform.gameObject);
                    if(hit.collider.CompareTag("Terminal"))
                    {
                        input.OffInputActions();
                    }
                }
            }
            else
            {
                Debug.Log("레이캐스트 실패!");
            }
        }
        if (!isClick)
        {
        }

    }
    private void OnInputAction()
    {
        input.OnInputActions();
    }

    /// <summary>
    /// 아이템 버리는 함수
    /// </summary>
    private void OnItemDrop()
    {
        if (inventory.InvenSlots[CurrentItemIndex] != null && inventory.InvenSlots[CurrentItemIndex].childCount > 0)    // 인벤토리 인벤슬롯의 현재 인덱스가 널이 아니고, 인벤토리 인벤슬롯안에 아이템이 있다면
        {
            CurrentItem = inventory.InvenSlots[CurrentItemIndex].GetChild(0);   // 현재 아이템은 인벤슬롯의 안에 있는 아이템이다.
            Collider itemCollider = CurrentItem.GetComponent<Collider>();       // 버릴때 콜라이더와 리지드바디 다시 킴
            if (itemCollider != null)
                itemCollider.enabled = true;
            Rigidbody itemRigidbody = CurrentItem.GetComponent<Rigidbody>();
            if (itemRigidbody != null)
                itemRigidbody.isKinematic = false;
            invenUI.ItemImages[CurrentItemIndex].color = new(1, 1, 1, 0.3f);

            Recycle recycle = CurrentItem.GetComponent<Recycle>();   
            if (recycle.getParent() != null)
            {
                CurrentItem.SetParent(recycle.getParent()); // 부모에서 떼어냅니다.
                Debug.Log("팩토리로 돌아감");
            }
            else
            {
                CurrentItem.SetParent(null);
                Debug.Log("그냥 버림");
            }
            TotalWeight -= CurrentItem.GetComponent<IItemDataBase>().GetItemDB().weight;
            SlowRatio = (0.01f * (100 - TotalWeight));
            for (int j = 0; j < 4; j++)
            {
                if (inventory.InvenSlots[j].childCount > 0)             // 인벤토리 인벤슬롯에 아이템이 들어있다면
                {
                    Transform tempItem = inventory.InvenSlots[j].GetChild(0);
                    if (tempItem != null)
                    {
                        IItemDataBase itemData = tempItem.GetComponent<IItemDataBase>();
                        if (itemData != null)
                        {
                            inventory.ItemDBs[j] = itemData.GetItemDB();                    // ItemDB에서 데이터를 가져와
                            invenUI.ItemImages[j].sprite = inventory.ItemDBs[j].itemIcon;   // 이미지를 인벤토리창에 띄움

                        }

                    }
                    
                }
                else
                {
                    inventory.ItemDBs[j] = null;
                    invenUI.ItemImages[j].sprite = null;            // 인벤토리 인벤슬롯에 아이템이 비어있으면 인벤토리 이미지 비움
                }
            }
        }
        CurrentItem = null;
    }


    /// <summary>
    /// 아이템 레이더를 켜고 끌수있게 오른쪽 마우스 버튼 입력에 대한 델리게이트로 연결되어있는 함수
    /// </summary>
    private void OnRClickInput(bool isPressed)
    {
        if(isPressed)
        {
            itemRader.gameObject.SetActive(true);
        }
        else
        {
            onRclickIsNotPressed?.Invoke();
            itemRader.gameObject.SetActive(false);
        }
    }


    /// <summary>
    /// 좌클릭에 해당하는 입력에 대해 델리게이트로 실행되는 함수
    /// </summary>
    private void OnLClickInput(bool isPressed)
    {
        
        // 아이템 사용 처리
        if (CurrentItem != null && isPressed)
        {
            IEquipable equipable = CurrentItem.GetComponent<IEquipable>();
            if (equipable != null)
            {
                equipable.Use();
            }
        }
    }
    void ResetItemInventory()
    {
        Transform tempItem;
        for (int j = 0; j < 4; j++)
        {
            if (inventory.InvenSlots[j].childCount > 0)             // 인벤토리 인벤슬롯에 아이템이 들어있다면
            {
                tempItem = inventory.InvenSlots[j].GetChild(0);
                Destroy(tempItem.gameObject);
                inventory.ItemDBs[j] = null;
                invenUI.ItemImages[j].sprite = null;
                CurrentItem = null;
            }
        }
    }

    private void OnScrollWheel(Vector2 vector)
    {
        foreach (Transform obj in inventory.InvenSlots)
        {
            if (obj != null && obj.childCount > 0 && obj.GetChild(0).gameObject.activeSelf)
            {
                obj.GetChild(0).gameObject.SetActive(false);
            }
        }

        if (vector.y > 0)
        {
            CurrentItemIndex = PrevIndex(CurrentItemIndex);
        }
        else if (vector.y < 0)
        {
            CurrentItemIndex = NextIndex(CurrentItemIndex);
        }

        if (inventory.InvenSlots[CurrentItemIndex] != null && inventory.InvenSlots[CurrentItemIndex].childCount > 0)    // 인벤토리 슬롯 현재 인덱스가 널값이 아니고, 인벤토리 슬롯 현재 인덱스 내에 아이템이 들어있다면
        {
            CurrentItem = inventory.InvenSlots[CurrentItemIndex].GetChild(0);                                           // 현재 아이템을 인벤토리 슬롯의 현재 인덱스 안에 있는 아이템으로 저장
            
            if (CurrentItem != null)
            {
                CurrentItem.gameObject.SetActive(true);
            }
        }
        else
        {
            CurrentItem = null;
        }
        for (int i = 0; i < inventory.InvenSlots.Length; i++)
        {
            invenUI.ItemEdgeImages[i].color = invenUI.edgeRedInvisible;     // 일단 인벤토리 테두리 전부다 투명하게
        }
        invenUI.ItemEdgeImages[CurrentItemIndex].color = invenUI.edgeRed;   // 그후 현재 선택된 인덱스의 테두리를 활성화

    }

    /// <summary>
    /// 휠 사용할때 인덱스 증가시키는 함수
    /// </summary>
    /// <param name="index"> 휠 움직임 전 인덱스</param>
    /// <returns>다음 인덱스 값</returns>
    int NextIndex(int index)
    {
        int result;
        if (index + 1 > inventory.InvenSlots.Length - 1)
        {
            result = 0;
        }
        else
        {
            result = index + 1;
        }
        return result;
    }
    /// <summary>
    /// 휠 사용할 때 인덱스 감소시키는 함수
    /// </summary>
    /// <param name="index">휠 움직임 전 인덱스</param>
    /// <returns>이전 인덱스 값</returns>
    int PrevIndex(int index)
    {
        int result;
        if (index - 1 < 0)
        {
            result = inventory.InvenSlots.Length - 1;
        }
        else
        {
            result = index - 1;
        }
        return result;
    }

    
    private void OnInTerminal()
    {
        // 터미널 진입시 인풋시스템 비활성화
    }
    public void OnTestDamage()
    {
        Hp -= 10;
        if(onDamageVignette != null)
        {
            StopCoroutine(onDamageVignette);
        }
        _source.GenerateImpulse(new Vector3(UnityEngine.Random.Range(-1.0f, 0.0f), UnityEngine.Random.Range(-1.0f, 0.0f), 0.0f));
        onDamageVignette = StartCoroutine(OnDamageVignette());
    }

    Coroutine onDamageVignette;
    IEnumerator OnDamageVignette()
    {
        vignette.intensity.value = 0.3f;
        yield return new WaitForSeconds(0.1f);
        vignette.intensity.value = 0f;
    }

    public void Attack(IBattler target)
    {
        target.Defense(attackPower);
    }

    public void Defense(float Damage)
    {
        Hp -= Damage;
        if (onDamageVignette != null)
        {
            StopCoroutine(onDamageVignette);
        }
        _source.GenerateImpulse(new Vector3(UnityEngine.Random.Range(-1.0f, 0.0f), UnityEngine.Random.Range(-1.0f, 0.0f), 0.0f));
        onDamageVignette = StartCoroutine(OnDamageVignette());
    }


    public void ControllerTPPosition(Vector3 pos)
    {
        characterController.enabled = false;
        transform.position = pos;
        characterController.enabled = true;
    }

    public void DamageLog()
    {
        Debug.Log(Hp + "가 남았습니다.");
    }


    public void PlayerRefresh()
    {
        Stamina = maxStamina;
        Hp = maxHp;
        gravityY = -1f;
        collider.enabled = true;
        onRefresh?.Invoke();
    }
}
