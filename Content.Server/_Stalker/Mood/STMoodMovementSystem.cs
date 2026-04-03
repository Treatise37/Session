using System.Collections.Generic;
using Content.Shared._Stalker.Mood;
using Content.Shared.Movement.Systems;

namespace Content.Server._Stalker.Mood;

public sealed class STMoodMovementSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    private readonly Dictionary<EntityUid, STMoodState> _lastStates = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<STMoodComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<STMoodComponent, ComponentShutdown>(OnShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<STMoodComponent>();

        while (query.MoveNext(out var uid, out var mood))
        {
            if (_lastStates.TryGetValue(uid, out var oldState) && oldState == mood.State)
                continue;

            _lastStates[uid] = mood.State;
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }
    }

    private void OnShutdown(Entity<STMoodComponent> ent, ref ComponentShutdown args)
    {
        _lastStates.Remove(ent.Owner);
    }

    private void OnRefreshMovementSpeed(Entity<STMoodComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var modifier = ent.Comp.State switch
        {
            STMoodState.Great => 1.00f,
            STMoodState.Good => 1.00f,
            STMoodState.Okay => 1.00f,

            STMoodState.Discomfort => 0.97f,
            STMoodState.Bad => 0.92f,
            STMoodState.Pain => 0.82f,
            STMoodState.Agony => 0.70f,

            _ => 1.00f
        };

        args.ModifySpeed(modifier, modifier);
    }
}