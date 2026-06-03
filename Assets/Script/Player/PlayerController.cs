using JKFrame;
using UnityEngine;

public class PlayerController : MonoBehaviour, IStateMachineOwner
{
    public Animator animator;
    public CharacterController cc;
    public float moveSpeed = 4f;

    [HideInInspector] public Vector2 moveDir;
    [HideInInspector] public bool deadTrigger;

    private StateMachine stateMachine = new StateMachine();

    void Start()
    {
        animator = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
        stateMachine.Init(this);
        ChangeState(PlayerState.Idle);
    }

    void Update()
    {
        moveDir = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        // shoot 在 ATK Layer 独立播放，不打断主状态机
        if (Input.GetMouseButtonDown(0)) animator.SetTrigger("shoot");
    }

    public void ChangeState(PlayerState state)
    {
        switch (state)
        {
            case PlayerState.Idle: stateMachine.ChangeState<PlayerIdleState>(); break;
            case PlayerState.Run:  stateMachine.ChangeState<PlayerRunState>();  break;
            case PlayerState.Dead: stateMachine.ChangeState<PlayerDeadState>(); break;
        }
    }

    public void PlayAnim(string trigger) => animator.SetTrigger(trigger);
}
