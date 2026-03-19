using Content.Server.Database;
using Content.Shared._Stalker_EN.PDA;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.PDA;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server._Stalker_EN.PDA;

/// <summary>
/// Server system for PDA password lock functionality.
/// Intercepts PDA UI open attempts and shows a password prompt for non-owners.
/// Persists passwords to the database so they survive entity deletion (e.g. personal stash).
/// </summary>
public sealed class STPdaPasswordSystem : SharedSTPdaPasswordSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <summary>Maximum password length to prevent abuse.</summary>
    private const int MaxPasswordLength = 16;

    private readonly HashSet<(EntityUid Pda, EntityUid User)> _opening = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivatableUIComponent, ActivatableUIOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<STPdaPasswordComponent, BoundUIOpenedEvent>(OnBuiOpened);
        SubscribeLocalEvent<STPdaPasswordComponent, BoundUIClosedEvent>(OnBuiClosed);
        SubscribeLocalEvent<STPdaPasswordComponent, STPdaPasswordSubmitMessage>(OnPasswordSubmit);
        SubscribeLocalEvent<STPdaPasswordComponent, STPdaPasswordSetMessage>(OnPasswordSet);
        SubscribeLocalEvent<STPdaPasswordComponent, STPdaPasswordOpenSettingsMessage>(OnOpenSettings);
        SubscribeLocalEvent<STPdaPasswordComponent, GotEquippedEvent>(OnPdaPasswordEquipped);
        SubscribeLocalEvent<STPdaPasswordComponent, GotEquippedHandEvent>(OnPdaPasswordPickedUp);
    }

    /// <summary>
    /// When a PDA with a password component is equipped in the ID slot,
    /// attempt to reload the password from DB (handles stash store/retrieve).
    /// </summary>
    private void OnPdaPasswordEquipped(Entity<STPdaPasswordComponent> ent, ref GotEquippedEvent args)
    {
        if (!args.SlotFlags.HasFlag(SlotFlags.IDCARD))
            return;

        if (ent.Comp.Password != null)
            return;

        var charName = MetaData(args.Equipee).EntityName;
        LoadPasswordAsync(ent.Owner, charName);
    }

    /// <summary>
    /// When a PDA with a password component is picked up into a hand,
    /// attempt to reload the password from DB (handles wild/admin-spawned PDAs).
    /// </summary>
    private void OnPdaPasswordPickedUp(Entity<STPdaPasswordComponent> ent, ref GotEquippedHandEvent args)
    {
        if (ent.Comp.Password != null)
            return;

        var charName = MetaData(args.User).EntityName;
        LoadPasswordAsync(ent.Owner, charName);
    }

    private async void LoadPasswordAsync(EntityUid uid, string charName)
    {
        try
        {
            var record = await _db.GetStalkerPdaPasswordAsync(charName);

            if (Deleted(uid))
                return;

            if (!TryComp<STPdaPasswordComponent>(uid, out var current))
                return;

            if (current.Password != null || record == null)
                return;

            current.Password = record.Password;
            current.IsLocked = true;
            Dirty(uid, current);
        }
        catch
        {
        }
    }

    private void OnOpenAttempt(EntityUid uid, ActivatableUIComponent _, ref ActivatableUIOpenAttemptEvent args)
    {
        if (!TryComp<STPdaPasswordComponent>(uid, out var comp))
            return;

        if (args.Cancelled || !comp.IsLocked)
            return;

        if (TryComp<PdaComponent>(uid, out var pda) && pda.PdaOwner == args.User)
            return;

        var key = (uid, args.User);

        if (_opening.Contains(key))
        {
            args.Cancel();
            return;
        }

        args.Cancel();

        if (args.Silent)
            return;

        _opening.Add(key);

        _ui.CloseUi(uid, PdaUiKey.Key, args.User);
        _ui.SetUiState(uid, STPdaPasswordUiKey.Key, new STPdaPasswordUiState(false, false, true));
        _ui.TryOpenUi(uid, STPdaPasswordUiKey.Key, args.User);
    }

    private void OnBuiOpened(Entity<STPdaPasswordComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is not STPdaPasswordUiKey)
            return;

        var isOwner = TryComp<PdaComponent>(ent.Owner, out var pda) && pda.PdaOwner == args.Actor;

        _ui.SetUiState(ent.Owner, STPdaPasswordUiKey.Key,
            new STPdaPasswordUiState(false, isOwner, ent.Comp.IsLocked));
    }

    private void OnBuiClosed(Entity<STPdaPasswordComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is not STPdaPasswordUiKey)
            return;

        _opening.Remove((ent.Owner, args.Actor));
    }

    private void OnPasswordSubmit(Entity<STPdaPasswordComponent> ent, ref STPdaPasswordSubmitMessage args)
    {
        if (!ent.Comp.IsLocked || ent.Comp.Password == null)
            return;

        // Rate-limit password attempts to prevent brute-forcing
        if (ent.Comp.NextAttemptTime > _timing.CurTime)
            return;

        ent.Comp.NextAttemptTime = _timing.CurTime + ent.Comp.AttemptCooldown;

        if (args.Password == ent.Comp.Password)
        {
            _opening.Remove((ent.Owner, args.Actor));
            _ui.CloseUi(ent.Owner, STPdaPasswordUiKey.Key, args.Actor);
            _ui.TryOpenUi(ent.Owner, PdaUiKey.Key, args.Actor);
        }
        else
        {
            _ui.SetUiState(ent.Owner, STPdaPasswordUiKey.Key,
                new STPdaPasswordUiState(true, false, true));
        }
    }

    private void OnPasswordSet(Entity<STPdaPasswordComponent> ent, ref STPdaPasswordSetMessage args)
    {
        var actor = args.Actor;

        if (!TryComp<PdaComponent>(ent.Owner, out var pda) || pda.PdaOwner != actor)
        {
            _popup.PopupEntity(Loc.GetString("st-pda-password-not-owner"), ent.Owner, actor);
            return;
        }

        // Resolve character name for DB persistence
        var charName = MetaData(actor).EntityName;

        if (string.IsNullOrWhiteSpace(args.NewPassword))
        {
            ent.Comp.Password = null;
            ent.Comp.IsLocked = false;
            ent.Comp.UnlockedBy.Clear();
            Dirty(ent.Owner, ent.Comp);

            RemovePasswordAsync(charName);
        }
        else
        {
            var pass = args.NewPassword.Trim();
            if (pass.Length > MaxPasswordLength)
                pass = pass[..MaxPasswordLength];

            ent.Comp.Password = pass;
            ent.Comp.IsLocked = true;
            ent.Comp.UnlockedBy.Clear();
            Dirty(ent.Owner, ent.Comp);

            SavePasswordAsync(charName, pass);
        }

        _ui.SetUiState(ent.Owner, STPdaPasswordUiKey.Key,
            new STPdaPasswordUiState(false, true, ent.Comp.IsLocked));
    }

    private void OnOpenSettings(Entity<STPdaPasswordComponent> ent, ref STPdaPasswordOpenSettingsMessage args)
    {
        var actor = args.Actor;

        if (!TryComp<PdaComponent>(ent.Owner, out var pda) || pda.PdaOwner != actor)
            return;

        _ui.SetUiState(ent.Owner, STPdaPasswordUiKey.Key,
            new STPdaPasswordUiState(false, true, ent.Comp.IsLocked));
        _ui.TryOpenUi(ent.Owner, STPdaPasswordUiKey.Key, actor);
    }

    /// <summary>
    /// Persists a password to the database for the given character.
    /// </summary>
    private async void SavePasswordAsync(string charName, string password)
    {
        try
        {
            await _db.SetStalkerPdaPasswordAsync(charName, password);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Removes a password from the database for the given character.
    /// </summary>
    private async void RemovePasswordAsync(string charName)
    {
        try
        {
            await _db.RemoveStalkerPdaPasswordAsync(charName);
        }
        catch
        {
        }
    }
}
