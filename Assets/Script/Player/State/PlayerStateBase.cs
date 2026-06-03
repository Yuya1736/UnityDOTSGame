using JKFrame;

public class PlayerStateBase : StateBase
{
    protected PlayerController player;

    public override void Init(IStateMachineOwner owner)
    {
        player = owner as PlayerController;
    }
}
