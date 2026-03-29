using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;

namespace Content.Shared._Stalker.PersistentCrafting;

public sealed class PersistentCraftIngredientMatcher
{
    private readonly IEntityManager _entityManager;
    private readonly TagSystem _tagSystem;

    public PersistentCraftIngredientMatcher(
        IEntityManager entityManager,
        TagSystem tagSystem)
    {
        _entityManager = entityManager;
        _tagSystem = tagSystem;
    }

    public bool Matches(EntityUid entity, PersistentCraftIngredient ingredient)
    {
        switch (ingredient.GetSelectorKind())
        {
            case PersistentCraftIngredientSelectorKind.Proto:
                return _entityManager.TryGetComponent(entity, out MetaDataComponent? meta) &&
                       meta.EntityPrototype?.ID == ingredient.Proto;

            case PersistentCraftIngredientSelectorKind.StackType:
                var stackType = ingredient.StackType;
                return _entityManager.TryGetComponent(entity, out StackComponent? stack) &&
                       !string.IsNullOrWhiteSpace(stackType) &&
                       stack.StackTypeId == stackType;

            case PersistentCraftIngredientSelectorKind.Tag:
                return _tagSystem.HasTag(entity, ingredient.Tag!);

            default:
                return false;
        }

    }

    public int GetUsableAmount(EntityUid entity)
    {
        return _entityManager.TryGetComponent(entity, out StackComponent? stack) ? stack.Count : 1;
    }
}
