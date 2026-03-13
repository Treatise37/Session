using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Profiling;
using Robust.Shared.Timing;

namespace Content.Shared._RD.Watcher;

// TODO: job for grouping

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class RDWatcherSystemSingletonComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float GroupRadius = 10f;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan PositionInterval = TimeSpan.FromSeconds(1);

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan PositionNext;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan GroupInterval = TimeSpan.FromSeconds(15);

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan GroupNext;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool ProfilingEnabled;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string ProfilingMessage;
}

public sealed partial class RDWatcherSystem : RDEntitySystemSingleton<RDWatcherSystemSingletonComponent>
{
    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly INetManager _net = null!;

    public NetEntity ViewVariablesUid => GetNetEntity(Inst.Owner);

    private readonly System.Diagnostics.Stopwatch _stopwatch = new();

    public override void Initialize()
    {
        base.Initialize();

        InitializeGrouping();
        InitializeWatcherCache();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var component = Inst.Comp;
        var now = _timing.CurTime;

        if (component.ProfilingEnabled)
        {
            component.ProfilingMessage = string.Empty;

            _stopwatch.Restart();
            UpdateInternal(component, now);
            _stopwatch.Stop();

            component.ProfilingMessage += $"Total Update: {_stopwatch.Elapsed.TotalMilliseconds:F2} ms{Environment.NewLine}";
            return;
        }

        UpdateInternal(component, now);
    }

    private void UpdateInternal(RDWatcherSystemSingletonComponent component, TimeSpan now)
    {
        UpdateGrouping(component, now);
        UpdatePositions(component, now);
    }

    private void UpdateGrouping(RDWatcherSystemSingletonComponent component, TimeSpan now)
    {
        if (now < component.GroupNext)
            return;

        component.GroupNext = now + component.GroupInterval;
        DirtyField(nameof(RDWatcherSystemSingletonComponent.GroupNext));

        if (component.ProfilingEnabled)
        {
            _stopwatch.Restart();
            UpdateWatchers();
            _stopwatch.Stop();

            component.ProfilingMessage += $"Grouping: {_stopwatch.Elapsed.TotalMilliseconds:F2} ms{Environment.NewLine}";
            return;
        }

        UpdateWatchers();
    }

    private void UpdatePositions(RDWatcherSystemSingletonComponent component, TimeSpan now)
    {
        if (now < component.PositionNext)
            return;

        component.PositionNext = now + component.PositionInterval;
        DirtyField(nameof(RDWatcherSystemSingletonComponent.PositionNext));

        if (component.ProfilingEnabled)
        {
            _stopwatch.Restart();
            UpdateWatcherPositions();
            _stopwatch.Stop();

            component.ProfilingMessage += $"Positions: {_stopwatch.Elapsed.TotalMilliseconds:F2} ms{Environment.NewLine}";
            return;
        }

        UpdateWatcherPositions();
    }
}
