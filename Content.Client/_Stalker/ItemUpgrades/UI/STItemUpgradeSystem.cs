using Content.Client._Stalker.ItemUpgrades.UI;
using Content.Shared._Stalker.ItemUpgrades;
using Robust.Client.Player;
using Robust.Shared.GameObjects;

namespace Content.Client._Stalker.ItemUpgrades;

public sealed class STItemUpgradeSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;

    private STItemUpgradeWindow? _window;

    public override void Initialize()
    {
        SubscribeNetworkEvent<STItemUpgradeOpenEvent>(OnOpenEvent);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _window?.Dispose();
        _window = null;
    }

    private void OnOpenEvent(STItemUpgradeOpenEvent ev)
    {
        if (_window == null || _window.Disposed)
            _window = new STItemUpgradeWindow();

        _window.OpenCentered();
        _window.MoveToFront();
        _window.UpdateState(new STItemUpgradeBoundUserInterfaceState(ev.Items));
    }

    public void RequestInstallUpgrade(NetEntity item, string upgradeId)
    {
        if (_player.LocalEntity is not { } user)
            return;

        RaiseNetworkEvent(new STItemUpgradeInstallRequestEvent(
            GetNetEntity(user),
            item,
            upgradeId));
    }

    public void RequestResetUpgrades(NetEntity item)
    {
        if (_player.LocalEntity is not { } user)
            return;

        RaiseNetworkEvent(new STItemUpgradeResetRequestEvent(
            GetNetEntity(user),
            item));
    }

    public void RequestRepairItem(NetEntity item)
    {
        if (_player.LocalEntity is not { } user)
            return;

        RaiseNetworkEvent(new STItemRepairRequestEvent(
            GetNetEntity(user),
            item));
    }
}