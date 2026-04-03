using System;
using System.Linq;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared._Stalker.Mood;
using Content.Shared._Stalker.Weight;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;

namespace Content.Server._Stalker.Mood;

public sealed class STMoodSystem : SharedSTMoodSystem
{
    private SolutionContainerSystem _solutionContainer = default!;

    private static readonly ReagentId NicotineReagent = new("Nicotine", null);
    private static readonly ReagentId EthanolReagent = new("Ethanol", null);

    private static readonly ReagentId STHerculesReagent = new("STHercules", null);
    private static readonly ReagentId STTaurineReagent = new("STTaurine", null);
    private static readonly ReagentId STStimulantsReagent = new("STStimulants", null);
    private static readonly ReagentId ExperimentalStimulantsStalkerReagent = new("ExperimentalStimulantsStalker", null);

    private static readonly ReagentId THCReagent = new("THC", null);
    private static readonly ReagentId SpaceDrugsReagent = new("SpaceDrugs", null);
    private static readonly ReagentId HappinessReagent = new("Happiness", null);
    private static readonly ReagentId PsicodineReagent = new("Psicodine", null);

    private static readonly ReagentId NocturneSerumReagent = new("NocturneSerum", null);
    private static readonly ReagentId MindbreakerToxinReagent = new("MindbreakerToxin", null);
    private static readonly ReagentId AirlossSerumReagent = new("AirlossSerum", null);
    private static readonly ReagentId ChloralHydrateReagent = new("ChloralHydrate", null);
    private static readonly ReagentId LexorinReagent = new("Lexorin", null);

    private static readonly ReagentId STToxicWaterReagent = new("STToxicWater", null);
    private static readonly ReagentId STNavozReagent = new("STNavoz", null);
    private static readonly ReagentId AbsoluteAbsorberReagent = new("AbsoluteAbsorber", null);

    private const string AgonyProgressionSource = "agony_progression";
    private const string HungerSource = "hunger";
    private const string ThirstSource = "thirst";
    private const string WeightSource = "weight";
    private const string PainSource = "pain";
    private const string NicotineSource = "nicotine";
    private const string NicotineCravingSource = "nicotine_craving";
    private const string AlcoholSource = "alcohol";
    private const string AlcoholCrashSource = "alcohol_crash";
    private const string DrugSource = "drug";
    private const string DrugCrashSource = "drug_crash";
    private const string WithdrawalSource = "withdrawal";
    private const string ChemicalStressSource = "chemical_stress";

    public override void Initialize()
    {
        base.Initialize();
        _solutionContainer = EntityManager.System<SolutionContainerSystem>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<STMoodComponent>();

        while (query.MoveNext(out var uid, out var mood))
        {
            UpdateHungerMood((uid, mood));
            UpdateThirstMood((uid, mood));
            UpdateWeightMood((uid, mood));
            UpdatePainMood((uid, mood));
            UpdateNicotineMood((uid, mood));
            UpdateNicotineWithdrawalMood((uid, mood));
            UpdateAlcoholMood((uid, mood));
            UpdateAlcoholWithdrawalMood((uid, mood));
            UpdateDrugMood((uid, mood));
            UpdateDrugWithdrawalMood((uid, mood));
            UpdateChemicalStressMood((uid, mood));
            UpdateAgonyProgress((uid, mood), frameTime);
        }
    }

    private void UpdateHungerMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<HungerComponent>(ent, out var hunger))
        {
            RemoveEffect(ent, STMoodEffectType.Hungry, HungerSource);
            RemoveEffect(ent, STMoodEffectType.WellFed, HungerSource);
            return;
        }

        switch (hunger.CurrentThreshold)
        {
            case HungerThreshold.Overfed:
                RemoveEffect(ent, STMoodEffectType.Hungry, HungerSource);
                UpsertEffect(ent, STMoodEffectType.WellFed, 10f, HungerSource);
                break;

            case HungerThreshold.Okay:
                RemoveEffect(ent, STMoodEffectType.Hungry, HungerSource);
                RemoveEffect(ent, STMoodEffectType.WellFed, HungerSource);
                break;

            case HungerThreshold.Peckish:
                RemoveEffect(ent, STMoodEffectType.WellFed, HungerSource);
                UpsertEffect(ent, STMoodEffectType.Hungry, -8f, HungerSource);
                break;

            case HungerThreshold.Starving:
                RemoveEffect(ent, STMoodEffectType.WellFed, HungerSource);
                UpsertEffect(ent, STMoodEffectType.Hungry, -18f, HungerSource);
                break;

            case HungerThreshold.Dead:
                RemoveEffect(ent, STMoodEffectType.WellFed, HungerSource);
                UpsertEffect(ent, STMoodEffectType.Hungry, -30f, HungerSource);
                break;

            default:
                RemoveEffect(ent, STMoodEffectType.WellFed, HungerSource);
                RemoveEffect(ent, STMoodEffectType.Hungry, HungerSource);
                break;
        }
    }

    private void UpdateThirstMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<ThirstComponent>(ent, out var thirst))
        {
            RemoveEffect(ent, STMoodEffectType.Thirsty, ThirstSource);
            RemoveEffect(ent, STMoodEffectType.Hydrated, ThirstSource);
            return;
        }

        switch (thirst.CurrentThirstThreshold)
        {
            case ThirstThreshold.OverHydrated:
                RemoveEffect(ent, STMoodEffectType.Thirsty, ThirstSource);
                UpsertEffect(ent, STMoodEffectType.Hydrated, 10f, ThirstSource);
                break;

            case ThirstThreshold.Okay:
                RemoveEffect(ent, STMoodEffectType.Thirsty, ThirstSource);
                RemoveEffect(ent, STMoodEffectType.Hydrated, ThirstSource);
                break;

            case ThirstThreshold.Thirsty:
                RemoveEffect(ent, STMoodEffectType.Hydrated, ThirstSource);
                UpsertEffect(ent, STMoodEffectType.Thirsty, -8f, ThirstSource);
                break;

            case ThirstThreshold.Parched:
                RemoveEffect(ent, STMoodEffectType.Hydrated, ThirstSource);
                UpsertEffect(ent, STMoodEffectType.Thirsty, -20f, ThirstSource);
                break;

            case ThirstThreshold.Dead:
                RemoveEffect(ent, STMoodEffectType.Hydrated, ThirstSource);
                UpsertEffect(ent, STMoodEffectType.Thirsty, -35f, ThirstSource);
                break;

            default:
                RemoveEffect(ent, STMoodEffectType.Hydrated, ThirstSource);
                RemoveEffect(ent, STMoodEffectType.Thirsty, ThirstSource);
                break;
        }
    }

    private void UpdateWeightMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<STWeightComponent>(ent, out var weight))
        {
            RemoveEffect(ent, STMoodEffectType.Overweight, WeightSource);
            return;
        }

        var total = weight.Total;
        var overload = weight.TotalOverload;
        var maximum = weight.TotalMaximum;

        if (total <= overload || maximum <= overload)
        {
            RemoveEffect(ent, STMoodEffectType.Overweight, WeightSource);
            return;
        }

        var overloadRange = maximum - overload;
        var overloadProgress = (total - overload) / overloadRange;

        float penalty;

        if (total >= maximum)
        {
            penalty = -30f;
        }
        else if (overloadProgress >= 0.66f)
        {
            penalty = -30f;
        }
        else if (overloadProgress >= 0.33f)
        {
            penalty = -18f;
        }
        else
        {
            penalty = -8f;
        }

        UpsertEffect(ent, STMoodEffectType.Overweight, penalty, WeightSource);
    }

    private void UpdatePainMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<DamageableComponent>(ent, out var damageable) ||
            !TryComp<MobThresholdsComponent>(ent, out var thresholds) ||
            !TryComp<MobStateComponent>(ent, out var mobState))
        {
            RemoveEffect(ent, STMoodEffectType.PainShock, PainSource);
            return;
        }

        if (mobState.CurrentState == MobState.Dead)
        {
            RemoveEffect(ent, STMoodEffectType.PainShock, PainSource);
            return;
        }

        if (!TryGetCriticalThreshold(thresholds, out var critThreshold))
        {
            RemoveEffect(ent, STMoodEffectType.PainShock, PainSource);
            return;
        }

        var totalDamage = damageable.TotalDamage.Float();
        var critValue = critThreshold.Float();

        if (critValue <= 0f)
        {
            RemoveEffect(ent, STMoodEffectType.PainShock, PainSource);
            return;
        }

        float painPenalty;

        if (mobState.CurrentState == MobState.Critical)
        {
            painPenalty = -80f;
        }
        else
        {
            var damageRatio = totalDamage / critValue;

            if (damageRatio >= 0.85f)
            {
                painPenalty = -50f;
            }
            else if (damageRatio >= 0.60f)
            {
                painPenalty = -25f;
            }
            else if (damageRatio >= 0.35f)
            {
                painPenalty = -12f;
            }
            else
            {
                painPenalty = 0f;
            }
        }

        UpsertEffect(ent, STMoodEffectType.PainShock, painPenalty, PainSource);
    }

    private void UpdateNicotineMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<BloodstreamComponent>(ent, out var bloodstream))
        {
            RemoveEffect(ent, STMoodEffectType.NicotineRush, NicotineSource);
            return;
        }

        if (!_solutionContainer.ResolveSolution(ent.Owner,
                bloodstream.ChemicalSolutionName,
                ref bloodstream.ChemicalSolution,
                out var chemicalSolution))
        {
            RemoveEffect(ent, STMoodEffectType.NicotineRush, NicotineSource);
            return;
        }

        if (!chemicalSolution.TryGetReagent(NicotineReagent, out var nicotine) ||
            nicotine.Quantity <= FixedPoint2.Zero)
        {
            RemoveEffect(ent, STMoodEffectType.NicotineRush, NicotineSource);
            return;
        }

        UpsertEffect(ent, STMoodEffectType.NicotineRush, 5f, NicotineSource);
    }

    private void UpdateNicotineWithdrawalMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<STAddictionComponent>(ent, out var addiction) || !addiction.NicotineAddicted)
        {
            RemoveEffect(ent, STMoodEffectType.NicotineCraving, NicotineCravingSource);
            return;
        }

        if (addiction.NicotineReliefTime > 0f)
        {
            RemoveEffect(ent, STMoodEffectType.NicotineCraving, NicotineCravingSource);
            return;
        }

        var progress = addiction.NicotineWithdrawalProgress;

        if (progress < 25f)
        {
            UpsertEffect(ent, STMoodEffectType.NicotineCraving, -4f, NicotineCravingSource);
            return;
        }

        if (progress < 65f)
        {
            UpsertEffect(ent, STMoodEffectType.NicotineCraving, -10f, NicotineCravingSource);
            return;
        }

        UpsertEffect(ent, STMoodEffectType.NicotineCraving, -18f, NicotineCravingSource);
    }

    private void UpdateAlcoholMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<BloodstreamComponent>(ent, out var bloodstream))
        {
            RemoveEffect(ent, STMoodEffectType.AlcoholBuzz, AlcoholSource);
            return;
        }

        if (!_solutionContainer.ResolveSolution(ent.Owner,
                bloodstream.ChemicalSolutionName,
                ref bloodstream.ChemicalSolution,
                out var chemicalSolution))
        {
            RemoveEffect(ent, STMoodEffectType.AlcoholBuzz, AlcoholSource);
            return;
        }

        if (!chemicalSolution.TryGetReagent(EthanolReagent, out var ethanol) ||
            ethanol.Quantity <= FixedPoint2.Zero)
        {
            RemoveEffect(ent, STMoodEffectType.AlcoholBuzz, AlcoholSource);
            return;
        }

        UpsertEffect(ent, STMoodEffectType.AlcoholBuzz, 10f, AlcoholSource);
    }

    private void UpdateAlcoholWithdrawalMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<STAddictionComponent>(ent, out var addiction) || !addiction.AlcoholAddicted)
        {
            RemoveEffect(ent, STMoodEffectType.AlcoholCrash, AlcoholCrashSource);
            return;
        }

        if (addiction.AlcoholReliefTime > 0f)
        {
            RemoveEffect(ent, STMoodEffectType.AlcoholCrash, AlcoholCrashSource);
            return;
        }

        var progress = addiction.AlcoholWithdrawalProgress;

        if (progress < 30f)
        {
            UpsertEffect(ent, STMoodEffectType.AlcoholCrash, -6f, AlcoholCrashSource);
            return;
        }

        if (progress < 70f)
        {
            UpsertEffect(ent, STMoodEffectType.AlcoholCrash, -12f, AlcoholCrashSource);
            return;
        }

        UpsertEffect(ent, STMoodEffectType.AlcoholCrash, -20f, AlcoholCrashSource);
    }

    private void UpdateDrugMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<BloodstreamComponent>(ent, out var bloodstream))
        {
            RemoveEffect(ent, STMoodEffectType.DrugHigh, DrugSource);
            return;
        }

        if (!_solutionContainer.ResolveSolution(ent.Owner,
                bloodstream.ChemicalSolutionName,
                ref bloodstream.ChemicalSolution,
                out var chemicalSolution))
        {
            RemoveEffect(ent, STMoodEffectType.DrugHigh, DrugSource);
            return;
        }

        var bestBonus = 0f;

        if (chemicalSolution.TryGetReagent(ExperimentalStimulantsStalkerReagent, out var expStim) &&
            expStim.Quantity > FixedPoint2.Zero)
        {
            bestBonus = MathF.Max(bestBonus, 28f);
        }

        if (chemicalSolution.TryGetReagent(STStimulantsReagent, out var stimulants) &&
            stimulants.Quantity > FixedPoint2.Zero)
        {
            bestBonus = MathF.Max(bestBonus, 25f);
        }

        if (chemicalSolution.TryGetReagent(STHerculesReagent, out var hercules) &&
            hercules.Quantity > FixedPoint2.Zero)
        {
            bestBonus = MathF.Max(bestBonus, 18f);
        }

        if (chemicalSolution.TryGetReagent(SpaceDrugsReagent, out var spaceDrugs) &&
            spaceDrugs.Quantity > FixedPoint2.Zero)
        {
            bestBonus = MathF.Max(bestBonus, 16f);
        }

        if (chemicalSolution.TryGetReagent(STTaurineReagent, out var taurine) &&
            taurine.Quantity > FixedPoint2.Zero)
        {
            bestBonus = MathF.Max(bestBonus, 12f);
        }

        if (chemicalSolution.TryGetReagent(THCReagent, out var thc) &&
            thc.Quantity > FixedPoint2.Zero)
        {
            bestBonus = MathF.Max(bestBonus, 10f);
        }

        if (chemicalSolution.TryGetReagent(HappinessReagent, out var happiness) &&
            happiness.Quantity > FixedPoint2.Zero)
        {
            bestBonus = MathF.Max(bestBonus, 8f);
        }

        if (chemicalSolution.TryGetReagent(PsicodineReagent, out var psicodine) &&
            psicodine.Quantity > FixedPoint2.Zero)
        {
            bestBonus = MathF.Max(bestBonus, 6f);
        }

        UpsertEffect(ent, STMoodEffectType.DrugHigh, bestBonus, DrugSource);
    }

    private void UpdateDrugWithdrawalMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<STAddictionComponent>(ent, out var addiction) || !addiction.DrugAddicted)
        {
            RemoveEffect(ent, STMoodEffectType.DrugCrash, DrugCrashSource);
            RemoveEffect(ent, STMoodEffectType.Withdrawal, WithdrawalSource);
            return;
        }

        if (addiction.DrugReliefTime > 0f)
        {
            RemoveEffect(ent, STMoodEffectType.DrugCrash, DrugCrashSource);
            RemoveEffect(ent, STMoodEffectType.Withdrawal, WithdrawalSource);
            return;
        }

        var progress = addiction.DrugWithdrawalProgress;

        if (progress < 20f)
        {
            UpsertEffect(ent, STMoodEffectType.DrugCrash, -15f, DrugCrashSource);
            RemoveEffect(ent, STMoodEffectType.Withdrawal, WithdrawalSource);
            return;
        }

        RemoveEffect(ent, STMoodEffectType.DrugCrash, DrugCrashSource);

        if (progress < 45f)
        {
            UpsertEffect(ent, STMoodEffectType.Withdrawal, -35f, WithdrawalSource);
            return;
        }

        if (progress < 75f)
        {
            UpsertEffect(ent, STMoodEffectType.Withdrawal, -55f, WithdrawalSource);
            return;
        }

        UpsertEffect(ent, STMoodEffectType.Withdrawal, -85f, WithdrawalSource);
    }

    private void UpdateChemicalStressMood(Entity<STMoodComponent> ent)
    {
        if (!TryComp<BloodstreamComponent>(ent, out var bloodstream))
        {
            RemoveEffect(ent, STMoodEffectType.ChemicalStress, ChemicalStressSource);
            return;
        }

        if (!_solutionContainer.ResolveSolution(ent.Owner,
                bloodstream.ChemicalSolutionName,
                ref bloodstream.ChemicalSolution,
                out var chemicalSolution))
        {
            RemoveEffect(ent, STMoodEffectType.ChemicalStress, ChemicalStressSource);
            return;
        }

        var penalty = 0f;

        if (chemicalSolution.TryGetReagent(NocturneSerumReagent, out var nocturne) &&
            nocturne.Quantity > FixedPoint2.Zero)
        {
            penalty -= 20f;
        }

        if (chemicalSolution.TryGetReagent(MindbreakerToxinReagent, out var mindbreaker) &&
            mindbreaker.Quantity > FixedPoint2.Zero)
        {
            penalty -= 8f;
        }

        if (chemicalSolution.TryGetReagent(AirlossSerumReagent, out var airlossSerum) &&
            airlossSerum.Quantity > FixedPoint2.Zero)
        {
            penalty -= 35f;
        }

        if (chemicalSolution.TryGetReagent(ChloralHydrateReagent, out var chloral) &&
            chloral.Quantity > FixedPoint2.Zero)
        {
            penalty -= 15f;
        }

        if (chemicalSolution.TryGetReagent(LexorinReagent, out var lexorin) &&
            lexorin.Quantity > FixedPoint2.Zero)
        {
            penalty -= 20f;
        }

        if (chemicalSolution.TryGetReagent(STToxicWaterReagent, out var toxicWater) &&
            toxicWater.Quantity > FixedPoint2.Zero)
        {
            penalty -= 10f;
        }

        if (chemicalSolution.TryGetReagent(STNavozReagent, out var navoz) &&
            navoz.Quantity > FixedPoint2.Zero)
        {
            penalty -= 12f;
        }

        if (chemicalSolution.TryGetReagent(AbsoluteAbsorberReagent, out var absorber) &&
            absorber.Quantity > FixedPoint2.Zero)
        {
            penalty -= 25f;
        }

        UpsertEffect(ent, STMoodEffectType.ChemicalStress, penalty, ChemicalStressSource);
    }

    private bool TryGetCriticalThreshold(MobThresholdsComponent thresholds, out FixedPoint2 criticalThreshold)
    {
        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state == MobState.Critical)
            {
                criticalThreshold = threshold;
                return true;
            }
        }

        criticalThreshold = default;
        return false;
    }

    private void UpdateAgonyProgress(Entity<STMoodComponent> ent, float frameTime)
    {
        var state = ent.Comp.State;

        if (state == STMoodState.Pain)
        {
            ent.Comp.AgonyProgress += ent.Comp.PainToAgonyPerSecond * frameTime;
        }
        else if (state != STMoodState.Agony)
        {
            ent.Comp.AgonyProgress -= ent.Comp.AgonyRecoveryPerSecond * frameTime;
        }

        if (ent.Comp.AgonyProgress < 0f)
            ent.Comp.AgonyProgress = 0f;

        var overflow = 0f;

        if (ent.Comp.AgonyProgress >= ent.Comp.AgonyThreshold)
        {
            var extraProgress = ent.Comp.AgonyProgress - ent.Comp.AgonyThreshold;
            overflow = -MathF.Min(35f, 8f + extraProgress * 1.1f);
        }

        UpsertEffect(ent, STMoodEffectType.AgonyProgression, overflow, AgonyProgressionSource);
        Dirty(ent);
    }

    public void Recalculate(Entity<STMoodComponent> ent)
    {
        var oldValue = ent.Comp.Value;
        var oldState = ent.Comp.State;

        var total = 0f;

        foreach (var effect in ent.Comp.ActiveEffects)
        {
            total += effect.Value;
        }

        total = ClampMood(total);
        var newState = GetMoodState(total);

        ent.Comp.Value = total;
        ent.Comp.State = newState;

        Dirty(ent);

        if (oldValue.Equals(total) && oldState == newState)
            return;

        var ev = new STMoodChangedEvent(oldValue, total, oldState, newState);
        RaiseLocalEvent(ent, ev);
    }

    public void AddEffect(
        Entity<STMoodComponent> ent,
        STMoodEffectType type,
        float value,
        string? sourceId = null)
    {
        ent.Comp.ActiveEffects.Add(new STMoodEffect(type, value, sourceId));
        Recalculate(ent);
    }

    public void UpsertEffect(
        Entity<STMoodComponent> ent,
        STMoodEffectType type,
        float value,
        string? sourceId = null)
    {
        var existing = ent.Comp.ActiveEffects.FirstOrDefault(x =>
            x.Type == type &&
            x.SourceId == sourceId);

        if (value.Equals(0f))
        {
            if (existing == null)
                return;

            ent.Comp.ActiveEffects.Remove(existing);
            Recalculate(ent);
            return;
        }

        if (existing == null)
        {
            ent.Comp.ActiveEffects.Add(new STMoodEffect(type, value, sourceId));
            Recalculate(ent);
            return;
        }

        if (existing.Value.Equals(value))
            return;

        existing.Value = value;
        Recalculate(ent);
    }

    public void RemoveEffects(
        Entity<STMoodComponent> ent,
        STMoodEffectType type)
    {
        if (ent.Comp.ActiveEffects.RemoveAll(x => x.Type == type) == 0)
            return;

        Recalculate(ent);
    }

    public void RemoveEffect(
        Entity<STMoodComponent> ent,
        STMoodEffectType type,
        string? sourceId)
    {
        if (ent.Comp.ActiveEffects.RemoveAll(x =>
                x.Type == type &&
                x.SourceId == sourceId) == 0)
            return;

        Recalculate(ent);
    }

    public void ClearEffects(Entity<STMoodComponent> ent)
    {
        if (ent.Comp.ActiveEffects.Count == 0)
            return;

        ent.Comp.ActiveEffects.Clear();
        Recalculate(ent);
    }

    public float GetMoodValue(EntityUid uid)
    {
        return TryComp<STMoodComponent>(uid, out var mood) ? mood.Value : 0f;
    }

    public STMoodState GetMoodState(EntityUid uid)
    {
        return TryComp<STMoodComponent>(uid, out var mood) ? mood.State : STMoodState.Okay;
    }
}