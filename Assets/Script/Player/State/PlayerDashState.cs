using UnityEngine;

public class PlayerDashState : PlayerStateBase
{
    private Vector3 _dashDir;
    private float _timer;

    public override void Enter()
    {
        player.PlayAnim("dash");
        player.ResetDashCooldown();
        _timer = player.dashDuration;

        // 以当前移动方向冲刺；无输入则沿朝向冲刺
        if (player.moveDir != Vector2.zero)
        {
            Transform cam = Camera.main.transform;
            Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 camRight   = Vector3.ProjectOnPlane(cam.right,   Vector3.up).normalized;
            _dashDir = (camForward * player.moveDir.y + camRight * player.moveDir.x).normalized;
        }
        else
        {
            _dashDir = player.transform.forward;
        }

        if (_dashDir != Vector3.zero)
            player.transform.rotation = Quaternion.LookRotation(_dashDir);
    }

    public override void Update()
    {
        if (player.deadTrigger) { player.deadTrigger = false; player.ChangeState(PlayerState.Dead); return; }

        _timer -= Time.deltaTime;
        player.cc.Move(_dashDir * player.dashSpeed * Time.deltaTime);

        if (_timer <= 0f)
        {
            if (player.moveDir != Vector2.zero)
                player.ChangeState(PlayerState.Run);
            else
                player.ChangeState(PlayerState.Idle);
        }
    }
}
