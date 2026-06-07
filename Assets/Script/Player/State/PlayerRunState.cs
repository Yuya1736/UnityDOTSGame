using UnityEngine;

public class PlayerRunState : PlayerStateBase
{
    public override void Enter() => player.PlayAnim("run");

    public override void Update()
    {
        if (player.deadTrigger) { player.deadTrigger = false; player.ChangeState(PlayerState.Dead); return; }
        if (player.dashTrigger) { player.dashTrigger = false; player.ChangeState(PlayerState.Dash); return; }
        if (player.moveDir == Vector2.zero) { player.ChangeState(PlayerState.Idle); return; }

        Transform cam = Camera.main.transform;
        Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camRight   = Vector3.ProjectOnPlane(cam.right,   Vector3.up).normalized;
        Vector3 dir = (camForward * player.moveDir.y + camRight * player.moveDir.x).normalized;
        player.cc.Move(dir * player.moveSpeed * Time.deltaTime);
        if (dir != Vector3.zero)
            player.transform.rotation = Quaternion.LookRotation(dir);
    }
}
