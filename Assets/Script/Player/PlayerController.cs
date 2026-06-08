using JKFrame;
using RootMotion.FinalIK;
using Unity.Mathematics;
using UnityEngine;

public class PlayerController : MonoBehaviour, IStateMachineOwner
{
    public Animator animator;
    public CharacterController cc;
    public float moveSpeed = 4f;

    [Header("冲刺")]
    public float dashSpeed = 14f;
    public float dashDuration = 0.25f;
    public float dashCooldown = 1f;

    [HideInInspector] public Vector2 moveDir;
    [HideInInspector] public bool deadTrigger;
    [HideInInspector] public bool dashTrigger;
    [HideInInspector] public bool shootTrigger;

    private float _dashCooldownTimer;

    private StateMachine stateMachine = new StateMachine();

    private AimController aimController;
    public Transform aimTarget;

    private const string shootAnimName = "shoot";
    private int shootAnimHash;

    private BulletSpawner _bulletSpawner;
    private bool _shootPending;
    private float _shootTimer;
    [Header("射击延迟（秒，匹配动画出枪帧）")]
    public float shootDelay = 0.15f;

    void Start()
    {
        currentHp = maxHp;
        aimController = GetComponent<AimController>();
        animator = GetComponent<Animator>();
        shootAnimHash = Animator.StringToHash(shootAnimName);
        cc = GetComponent<CharacterController>();
        _bulletSpawner = GetComponent<BulletSpawner>();
        stateMachine.Init(this);
        ChangeState(PlayerState.Idle);
    }

    void Update()
    {
#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                ? CursorLockMode.None
                : CursorLockMode.Locked;
            Cursor.visible = Cursor.lockState == CursorLockMode.None;
        }
#endif

#if UNITY_STANDALONE || UNITY_EDITOR
        moveDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif

        if (_dashCooldownTimer > 0f) _dashCooldownTimer -= Time.deltaTime;
#if UNITY_STANDALONE || UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.LeftShift) && _dashCooldownTimer <= 0f)
        {
            dashTrigger = true;
        }
#endif

        if (Input.GetMouseButtonDown(0) || shootTrigger)
        {
            animator.SetTrigger("shoot");
            shootTrigger = false;
        }

        if (IsShooting())
        {
            aimController.weight = 1;
            aimController.target = aimTarget;

            if (!_shootPending)
            {
                _shootPending = true;
                _shootTimer = shootDelay;
            }
        }
        else
        {
            aimController.target = null;
        }

        if (_shootPending)
        {
            _shootTimer -= Time.deltaTime;
            if (_shootTimer <= 0f)
            {
                _shootPending = false;
                if (_bulletSpawner != null && aimTarget != null)
                {
                    float3 dir = math.normalizesafe((float3)aimTarget.position - (float3)transform.position);
                    dir.y = 0;
                    _bulletSpawner.Fire(dir);
                }
            }
        }
    }

    public void ChangeState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle:
                stateMachine.ChangeState<PlayerIdleState>();
                break;
            case PlayerState.Run:
                stateMachine.ChangeState<PlayerRunState>();
                break;
            case PlayerState.Dash:
                stateMachine.ChangeState<PlayerDashState>();
                break;
            case PlayerState.Dead:
                stateMachine.ChangeState<PlayerDeadState>();
                break;
        }
    }

    public void PlayAnim(string trigger) => animator.SetTrigger(trigger);


    [Header("最大生命值")]
    public int maxHp = 100;
    [HideInInspector] public int currentHp;

    [HideInInspector] public bool isInvincible;

    public void TakeDamage(int damage)
    {
        if (deadTrigger || isInvincible) return;
        currentHp -= damage;
        if (currentHp <= 0)
        {
            currentHp = 0;
            deadTrigger = true;
            ChangeState(PlayerState.Dead);
        }
    }

    /// <summary>冷却进度 0=可用，1=刚触发，供 UI 冷却遮罩使用</summary>
    public float DashCooldownRatio => dashCooldown > 0f ? Mathf.Clamp01(_dashCooldownTimer / dashCooldown) : 0f;

    public bool IsDashReady => _dashCooldownTimer <= 0f;

    public void ResetDashCooldown() => _dashCooldownTimer = dashCooldown;

    public bool IsShooting()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(1);

        return stateInfo.shortNameHash == shootAnimHash && stateInfo.normalizedTime < 0.8f;
    }
}
