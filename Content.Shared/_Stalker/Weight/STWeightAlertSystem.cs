using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.Weight;

public sealed class STWeightAlertSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;

    private static readonly ProtoId<AlertPrototype> WeightAlert = "StalkerWeight";

    public override void Initialize()
    {
        SubscribeLocalEvent<STWeightComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<STWeightComponent, STWeightChangedEvent>(OnWeightChanged);
    }

    private void OnStartup(Entity<STWeightComponent> ent, ref ComponentStartup args)
    {
        RefreshAlert(ent.Owner, ent.Comp.Total, ent.Comp.TotalMaximum);
    }

    private void OnWeightChanged(Entity<STWeightComponent> ent, ref STWeightChangedEvent args)
    {
        RefreshAlert(ent.Owner, args.Total, args.TotalMaximum);
    }

    private void RefreshAlert(EntityUid uid, float total, float max)
    {
        if (total >= 100f)
        {
            _alerts.ShowAlert(uid, WeightAlert, 2); // красный
            return;
        }

        if (total >= 50f)
        {
            _alerts.ShowAlert(uid, WeightAlert, 1); // желтый
            return;
        }

        _alerts.ClearAlert(uid, WeightAlert);
    }
}