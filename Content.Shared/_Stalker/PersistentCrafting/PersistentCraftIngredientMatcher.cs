using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stalker.PersistentCrafting;

public sealed class PersistentCraftIngredientMatcher
{
    private readonly IEntityManager _entityManager;
    private readonly IPrototypeManager _prototypeManager;
    private readonly TagSystem _tagSystem;

    public PersistentCraftIngredientMatcher(
        IEntityManager entityManager,
        IPrototypeManager prototypeManager,
        TagSystem tagSystem)
    {
        _entityManager = entityManager;
        _prototypeManager = prototypeManager;
        _tagSystem = tagSystem;
    }

    public bool Matches(EntityUid entity, PersistentCraftIngredient ingredient)
    {
        if (!string.IsNullOrWhiteSpace(ingredient.Proto) &&
            _entityManager.TryGetComponent(entity, out MetaDataComponent? meta) &&
            meta.EntityPrototype?.ID == ingredient.Proto)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(ingredient.Proto) &&
            _entityManager.TryGetComponent(entity, out StackComponent? stack) &&
            _prototypeManager.TryIndex<EntityPrototype>(ingredient.Proto, out var ingredientProto) &&
            ingredientProto.TryGetComponent<StackComponent>(out var ingredientStack, _entityManager.ComponentFactory) &&
            stack.StackTypeId == ingredientStack.StackTypeId)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(ingredient.Tag) && _tagSystem.HasTag(entity, ingredient.Tag);
    }

    public int GetUsableAmount(EntityUid entity)
    {
        return _entityManager.TryGetComponent(entity, out StackComponent? stack) ? stack.Count : 1;
    }

    public bool TryGetIngredientStackType(string prototypeId, out string? stackType)
    {
        stackType = null;
        if (string.IsNullOrWhiteSpace(prototypeId))
            return false;

        if (!_prototypeManager.TryIndex<EntityPrototype>(prototypeId, out var prototype))
            return false;

        if (!prototype.TryGetComponent<StackComponent>(out var stack, _entityManager.ComponentFactory))
            return false;

        stackType = stack.StackTypeId;
        return true;
    }
}
