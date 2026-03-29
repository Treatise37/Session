using Content.Shared._Stalker.PersistentCrafting;
using Content.Client._Stalker.PersistentCrafting.UI;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Client._Stalker.PersistentCrafting;

public sealed class PersistentCraftingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    private const float InventoryRefreshInterval = 0.15f;

    private PersistentCraftClientPrototypeCache _prototypeCache = default!;
    private PersistentCraftStationWindow? _craftWindow;
    private PersistentCraftingWindow? _skillsWindow;
    private PersistentCraftState? _latestState;
    private float _inventoryRefreshAccumulator;

    public override void Initialize()
    {
        base.Initialize();
        _prototypeCache = PersistentCraftClientPrototypeCache.Create(_prototype);

        SubscribeNetworkEvent<OpenPersistentCraftMenuEvent>(OnOpenMenuEvent);
        SubscribeNetworkEvent<PersistentCraftStateEvent>(OnStateEvent);
    }

    public void RequestState()
    {
        RaiseNetworkEvent(new RequestPersistentCraftStateEvent());
    }

    public void RequestUnlock(string nodeId)
    {
        RaiseNetworkEvent(new RequestPersistentCraftUnlockEvent(nodeId));
    }

    public void RequestCraft(string recipeId)
    {
        RaiseNetworkEvent(new RequestPersistentCraftRecipeEvent(recipeId));
    }

    public void OpenSkillsWindow()
    {
        EnsureSkillsWindow();
        _skillsWindow!.ResetInitialTabSelection();
        _skillsWindow.ApplyFullscreenLayout();

        if (!_skillsWindow!.IsOpen)
            _skillsWindow.OpenCentered();
        else
            _skillsWindow.MoveToFront();

        RefreshSkillWindow();
    }

    private void ToggleSkillsWindowFromCraft()
    {
        EnsureSkillsWindow();

        if (_skillsWindow!.IsOpen)
        {
            _skillsWindow.Close();
            return;
        }

        OpenSkillsWindow();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_craftWindow == null ||
            _craftWindow.Disposed ||
            !_craftWindow.IsOpen ||
            _latestState == null)
        {
            _inventoryRefreshAccumulator = 0f;
            return;
        }

        _inventoryRefreshAccumulator += frameTime;
        if (_inventoryRefreshAccumulator < InventoryRefreshInterval)
            return;

        _inventoryRefreshAccumulator = 0f;
        _craftWindow.RefreshLocalInventory();
    }

    private void OnOpenMenuEvent(OpenPersistentCraftMenuEvent ev, EntitySessionEventArgs args)
    {
        EnsureCraftWindow();
        _craftWindow!.ResetInitialTabSelection();

        if (!_craftWindow!.IsOpen)
            _craftWindow.OpenCentered();
        else
            _craftWindow.MoveToFront();

        RefreshCraftWindow();
        RequestState();
    }

    private void OnStateEvent(PersistentCraftStateEvent ev, EntitySessionEventArgs args)
    {
        _latestState = ev.State;
        RefreshCraftWindow();
        RefreshSkillWindow();
    }

    private void EnsureCraftWindow()
    {
        _craftWindow ??= new PersistentCraftStationWindow();
        if (_craftWindow.Disposed)
            _craftWindow = new PersistentCraftStationWindow();

        _craftWindow.OnCraftPressed -= RequestCraft;
        _craftWindow.OnCraftPressed += RequestCraft;
        _craftWindow.OnOpenSkillsPressed -= ToggleSkillsWindowFromCraft;
        _craftWindow.OnOpenSkillsPressed += ToggleSkillsWindowFromCraft;
    }

    private void EnsureSkillsWindow()
    {
        _skillsWindow ??= new PersistentCraftingWindow();
        if (_skillsWindow.Disposed)
            _skillsWindow = new PersistentCraftingWindow();
    }

    private void RefreshCraftWindow()
    {
        if (_craftWindow == null || _craftWindow.Disposed || !_craftWindow.IsOpen || _latestState == null)
            return;

        _craftWindow.UpdateState(_latestState, _prototypeCache);
    }

    private void RefreshSkillWindow()
    {
        if (_skillsWindow == null || _skillsWindow.Disposed || !_skillsWindow.IsOpen || _latestState == null)
            return;

        _skillsWindow.UpdateState(
            _latestState,
            _prototypeCache,
            RequestUnlock);
    }
}
