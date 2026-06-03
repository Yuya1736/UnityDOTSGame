public class PlayerDeadState : PlayerStateBase
{
    public override void Enter()
    {
        player.PlayAnim("dead");
        player.enabled = false;
    }
}
