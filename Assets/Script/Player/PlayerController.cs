using JKFrame;
using RootMotion.FinalIK;
using Unity.Mathematics;
using UnityEngine;

public class PlayerController : MonoBehaviour, IStateMachineOwner
{
    public Animator animator;
    public CharacterController cc;
    public float moveSpeed = 4f;

    [HideInInspector] public Vector2 moveDir;
    [HideInInspector] public bool deadTrigger;

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
        moveDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        if (Input.GetMouseButtonDown(0)) animator.SetTrigger("shoot");

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
            case PlayerState.Dead: 
                stateMachine.ChangeState<PlayerDeadState>(); 
                break;
        }
    }

    public void PlayAnim(string trigger) => animator.SetTrigger(trigger);


    public bool IsShooting()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(1);

        return stateInfo.shortNameHash == shootAnimHash && stateInfo.normalizedTime < 0.8f;
    }
}
