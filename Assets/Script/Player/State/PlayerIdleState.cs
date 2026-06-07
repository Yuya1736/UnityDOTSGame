public class PlayerIdleState : PlayerStateBase
{
    public override void Enter() => player.PlayAnim("idle");

    public override void Update()
    {
        if (player.deadTrigger) { player.deadTrigger = false; player.ChangeState(PlayerState.Dead); return; }
        if (player.dashTrigger) { player.dashTrigger = false; player.ChangeState(PlayerState.Dash); return; }
        if (player.moveDir != UnityEngine.Vector2.zero) player.ChangeState(PlayerState.Run);
    }
}
