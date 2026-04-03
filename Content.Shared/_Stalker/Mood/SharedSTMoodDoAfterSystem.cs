namespace Content.Shared._Stalker.Mood;

public sealed class SharedSTMoodDoAfterSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<STMoodComponent, STMoodModifyDoAfterEvent>(OnModifyDoAfter);
    }

    private void OnModifyDoAfter(Entity<STMoodComponent> ent, ref STMoodModifyDoAfterEvent args)
    {
        var multiplier = ent.Comp.State switch
        {
            STMoodState.Discomfort => 1.05f,
            STMoodState.Bad => 1.10f,
            STMoodState.Pain => 1.20f,
            STMoodState.Agony => 1.35f,
            _ => 1.0f
        };

        if (multiplier <= 1.0f)
            return;

        args.Delay = TimeSpan.FromSeconds(args.Delay.TotalSeconds * multiplier);
    }
}