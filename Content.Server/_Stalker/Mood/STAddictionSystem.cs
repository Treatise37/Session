using System;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared._Stalker.Mood;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Server._Stalker.Mood;

public sealed class STAddictionSystem : EntitySystem
{
    private SolutionContainerSystem _solutionContainer = default!;

    private static readonly ReagentId STHerculesReagent = new("STHercules", null);
    private static readonly ReagentId STTaurineReagent = new("STTaurine", null);
    private static readonly ReagentId STStimulantsReagent = new("STStimulants", null);
    private static readonly ReagentId ExperimentalStimulantsStalkerReagent = new("ExperimentalStimulantsStalker", null);
    private static readonly ReagentId THCReagent = new("THC", null);
    private static readonly ReagentId SpaceDrugsReagent = new("SpaceDrugs", null);
    private static readonly ReagentId HappinessReagent = new("Happiness", null);
    private static readonly ReagentId PsicodineReagent = new("Psicodine", null);
    private static readonly ReagentId NocturneSerumReagent = new("NocturneSerum", null);

    private static readonly ReagentId NicotineReagent = new("Nicotine", null);
    private static readonly ReagentId EthanolReagent = new("Ethanol", null);

    public override void Initialize()
    {
        base.Initialize();
        _solutionContainer = EntityManager.System<SolutionContainerSystem>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<STAddictionComponent>();

        while (query.MoveNext(out var uid, out var addiction))
        {
            UpdateDrugAddiction((uid, addiction), frameTime);
            UpdateNicotineAddiction((uid, addiction), frameTime);
            UpdateAlcoholAddiction((uid, addiction), frameTime);
        }
    }

    private void UpdateDrugAddiction(Entity<STAddictionComponent> ent, float frameTime)
    {
        var hasActiveDrug = HasTrackedDrugInBloodstream(ent.Owner);

        if (hasActiveDrug)
        {
            ent.Comp.DrugAddicted = true;
            ent.Comp.DrugWithdrawalProgress = 0f;
            ent.Comp.DrugReliefTime = ent.Comp.DrugReliefDuration;
            ent.Comp.DrugRecoveryTime = 0f;
            Dirty(ent);
            return;
        }

        if (!ent.Comp.DrugAddicted)
            return;

        if (ent.Comp.DrugReliefTime > 0f)
        {
            ent.Comp.DrugReliefTime -= frameTime;

            if (ent.Comp.DrugReliefTime < 0f)
                ent.Comp.DrugReliefTime = 0f;

            Dirty(ent);
            return;
        }

        ent.Comp.DrugWithdrawalProgress += ent.Comp.DrugWithdrawalPerSecond * frameTime;

        if (ent.Comp.DrugWithdrawalProgress > 100f)
            ent.Comp.DrugWithdrawalProgress = 100f;

        if (ent.Comp.DrugWithdrawalProgress >= 100f)
        {
            ent.Comp.DrugRecoveryTime += frameTime;

            if (ent.Comp.DrugRecoveryTime >= ent.Comp.DrugRecoveryDuration)
            {
                ent.Comp.DrugAddicted = false;
                ent.Comp.DrugWithdrawalProgress = 0f;
                ent.Comp.DrugReliefTime = 0f;
                ent.Comp.DrugRecoveryTime = 0f;
            }
        }
        else
        {
            ent.Comp.DrugRecoveryTime = 0f;
        }

        Dirty(ent);
    }

    private void UpdateNicotineAddiction(Entity<STAddictionComponent> ent, float frameTime)
    {
        var hasNicotine = HasNicotineInBloodstream(ent.Owner);

        if (hasNicotine)
        {
            ent.Comp.NicotineAddicted = true;
            ent.Comp.NicotineWithdrawalProgress = 0f;
            ent.Comp.NicotineReliefTime = ent.Comp.NicotineReliefDuration;
            ent.Comp.NicotineRecoveryTime = 0f;
            Dirty(ent);
            return;
        }

        if (!ent.Comp.NicotineAddicted)
            return;

        if (ent.Comp.NicotineReliefTime > 0f)
        {
            ent.Comp.NicotineReliefTime -= frameTime;

            if (ent.Comp.NicotineReliefTime < 0f)
                ent.Comp.NicotineReliefTime = 0f;

            Dirty(ent);
            return;
        }

        ent.Comp.NicotineWithdrawalProgress += ent.Comp.NicotineWithdrawalPerSecond * frameTime;

        if (ent.Comp.NicotineWithdrawalProgress > 100f)
            ent.Comp.NicotineWithdrawalProgress = 100f;

        if (ent.Comp.NicotineWithdrawalProgress >= 100f)
        {
            ent.Comp.NicotineRecoveryTime += frameTime;

            if (ent.Comp.NicotineRecoveryTime >= ent.Comp.NicotineRecoveryDuration)
            {
                ent.Comp.NicotineAddicted = false;
                ent.Comp.NicotineWithdrawalProgress = 0f;
                ent.Comp.NicotineReliefTime = 0f;
                ent.Comp.NicotineRecoveryTime = 0f;
            }
        }
        else
        {
            ent.Comp.NicotineRecoveryTime = 0f;
        }

        Dirty(ent);
    }

    private void UpdateAlcoholAddiction(Entity<STAddictionComponent> ent, float frameTime)
    {
        var hasAlcohol = HasAlcoholInBloodstream(ent.Owner);

        if (hasAlcohol)
        {
            ent.Comp.AlcoholAddicted = true;
            ent.Comp.AlcoholWithdrawalProgress = 0f;
            ent.Comp.AlcoholReliefTime = ent.Comp.AlcoholReliefDuration;
            ent.Comp.AlcoholRecoveryTime = 0f;
            Dirty(ent);
            return;
        }

        if (!ent.Comp.AlcoholAddicted)
            return;

        if (ent.Comp.AlcoholReliefTime > 0f)
        {
            ent.Comp.AlcoholReliefTime -= frameTime;

            if (ent.Comp.AlcoholReliefTime < 0f)
                ent.Comp.AlcoholReliefTime = 0f;

            Dirty(ent);
            return;
        }

        ent.Comp.AlcoholWithdrawalProgress += ent.Comp.AlcoholWithdrawalPerSecond * frameTime;

        if (ent.Comp.AlcoholWithdrawalProgress > 100f)
            ent.Comp.AlcoholWithdrawalProgress = 100f;

        if (ent.Comp.AlcoholWithdrawalProgress >= 100f)
        {
            ent.Comp.AlcoholRecoveryTime += frameTime;

            if (ent.Comp.AlcoholRecoveryTime >= ent.Comp.AlcoholRecoveryDuration)
            {
                ent.Comp.AlcoholAddicted = false;
                ent.Comp.AlcoholWithdrawalProgress = 0f;
                ent.Comp.AlcoholReliefTime = 0f;
                ent.Comp.AlcoholRecoveryTime = 0f;
            }
        }
        else
        {
            ent.Comp.AlcoholRecoveryTime = 0f;
        }

        Dirty(ent);
    }

    private bool HasTrackedDrugInBloodstream(EntityUid uid)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return false;

        if (!_solutionContainer.ResolveSolution(uid,
                bloodstream.ChemicalSolutionName,
                ref bloodstream.ChemicalSolution,
                out var chemicalSolution))
        {
            return false;
        }

        return HasPositiveAmount(chemicalSolution, STHerculesReagent) ||
               HasPositiveAmount(chemicalSolution, STTaurineReagent) ||
               HasPositiveAmount(chemicalSolution, STStimulantsReagent) ||
               HasPositiveAmount(chemicalSolution, ExperimentalStimulantsStalkerReagent) ||
               HasPositiveAmount(chemicalSolution, THCReagent) ||
               HasPositiveAmount(chemicalSolution, SpaceDrugsReagent) ||
               HasPositiveAmount(chemicalSolution, HappinessReagent) ||
               HasPositiveAmount(chemicalSolution, PsicodineReagent) ||
               HasPositiveAmount(chemicalSolution, NocturneSerumReagent);
    }

    private bool HasNicotineInBloodstream(EntityUid uid)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return false;

        if (!_solutionContainer.ResolveSolution(uid,
                bloodstream.ChemicalSolutionName,
                ref bloodstream.ChemicalSolution,
                out var chemicalSolution))
        {
            return false;
        }

        return HasPositiveAmount(chemicalSolution, NicotineReagent);
    }

    private bool HasAlcoholInBloodstream(EntityUid uid)
    {
        if (!TryComp<BloodstreamComponent>(uid, out var bloodstream))
            return false;

        if (!_solutionContainer.ResolveSolution(uid,
                bloodstream.ChemicalSolutionName,
                ref bloodstream.ChemicalSolution,
                out var chemicalSolution))
        {
            return false;
        }

        return HasPositiveAmount(chemicalSolution, EthanolReagent);
    }

    private static bool HasPositiveAmount(Solution solution, ReagentId reagent)
    {
        return solution.TryGetReagent(reagent, out var quantity) &&
               quantity.Quantity > FixedPoint2.Zero;
    }
}