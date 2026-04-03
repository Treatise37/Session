using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.Mood;

public sealed class STMoodAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private static readonly ProtoId<AlertPrototype> MoodAlert = "STMoodState";

    public override void Initialize()
    {
        SubscribeLocalEvent<STMoodComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<STMoodComponent, STMoodChangedEvent>(OnMoodChanged);
    }

    private void OnStartup(Entity<STMoodComponent> ent, ref ComponentStartup args)
    {
        RefreshAlert(ent.Owner, ent.Comp.State);
    }

    private void OnMoodChanged(Entity<STMoodComponent> ent, ref STMoodChangedEvent args)
    {
        RefreshAlert(ent.Owner, args.NewState);
    }

    private void RefreshAlert(EntityUid uid, STMoodState state)
    {
        short severity = state switch
        {
            STMoodState.Agony => 1,
            STMoodState.Pain => 2,
            STMoodState.Bad => 3,
            STMoodState.Discomfort => 4,
            STMoodState.Okay => 5,
            STMoodState.Good => 6,
            STMoodState.Great => 7,
            _ => 5
        };

        _alerts.ShowAlert(uid, MoodAlert, severity);
    }
}