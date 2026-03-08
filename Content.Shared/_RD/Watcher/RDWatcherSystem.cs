using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RD.Watcher;

// TODO: job for grouping

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RDWatcherSystemSingletonComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float GroupRadius = 10f;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan PositionInterval = TimeSpan.FromSeconds(3);

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan PositionNext;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan GroupInterval = TimeSpan.FromMinutes(1);

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan GroupNext;
}

public sealed partial class RDWatcherSystem : RDEntitySystemSingleton<RDWatcherSystemSingletonComponent>
{
    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly INetManager _net = null!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeGrouping();
        InitializeWatcherCache();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Im badass
        if (_net.IsClient)
            return;

        if (Inst.Comp.GroupNext < _timing.CurTime)
        {
            Inst.Comp.GroupNext = _timing.CurTime + Inst.Comp.GroupInterval;
            DirtyField(nameof(RDWatcherSystemSingletonComponent.GroupNext));

            UpdateWatchers();
        }

        if (Inst.Comp.PositionInterval < _timing.CurTime)
        {
            Inst.Comp.PositionNext = _timing.CurTime + Inst.Comp.PositionInterval;
            DirtyField(nameof(RDWatcherSystemSingletonComponent.PositionNext));

            UpdateWatcherPositions();
        }
    }
}
