using Content.Shared._Stalker.TopographyMap;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Robust.Server.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Configuration;


namespace Content.Server._Stalker.TopographyMap;


public sealed partial class TopographyMapSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TopographyMapComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TopographyMapComponent, ActivateInWorldEvent>(OnActiveInWorld);
        SubscribeLocalEvent<TopographyMapComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<TopographyMapComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnUseInHand(EntityUid uid, TopographyMapComponent component, UseInHandEvent args)
    {
        OpenMap(uid, component, args.User);
    }

    private void OnActiveInWorld(EntityUid uid, TopographyMapComponent component, ActivateInWorldEvent args)
    {
        OpenMap(uid, component, args.User);
    }

    private void OnGetVerb(EntityUid uid, TopographyMapComponent component, GetVerbsEvent<AlternativeVerb> args)
    {   
        args.Verbs.Add(new()
        {
            Text = Loc.GetString("open-topograhpy-map"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/_Stalker/Interface/VerbIcons/openmap.png")),
            Priority = 3,
            Act = () => OpenMap(uid, component, args.User)
        });
    }

    private void OnInteractUsing(EntityUid uid, TopographyMapComponent component, InteractUsingEvent args)
    {
        if (!TryComp(args.Used, out TopographyMapComponent? componentused))
            return;
        
        UpdateTextureLists(componentused,componentused.TextureNames,componentused.TexturePaths);
        UpdateTextureLists(component,componentused.TextureNames,componentused.TexturePaths);

        _popupSystem.PopupEntity(Loc.GetString("added-new-topography-maps"), uid, args.User);
    }

    #region Helpers

    private void OpenMap(EntityUid map, TopographyMapComponent component, EntityUid opener)
    {
        if (_userInterfaceSystem.IsUiOpen(map, TopographyMapUIKey.Key, opener)) return;

        UpdateTextureLists(component,component.TextureNames,component.TexturePaths);

        var state = new TopographyMapBoundUiState()
        {
            Size = component.Size,
            MapTexturePath = component.MapTexturePath,
            TextureNames = component.TextureNames,
            TexturePaths = component.TexturePaths
        };

        _userInterfaceSystem.TryOpenUi(map, TopographyMapUIKey.Key, opener);
        _userInterfaceSystem.SetUiState(map, TopographyMapUIKey.Key, state);
    }

    private void UpdateTextureLists(TopographyMapComponent component, List<String> TextureNamesNew, List<String> TexturePathsNew)
    {
        if (!component.TextureNames.Contains(component.MapTextureName))
        {
            component.TextureNames.Add(component.MapTextureName);
            component.TexturePaths.Add(component.MapTexturePath);
        }

        for (int i = 0; i < TextureNamesNew.Count; i++)
        {
            if (!component.TextureNames.Contains(TextureNamesNew[i]))
            {
                component.TextureNames.Add(TextureNamesNew[i]);
                component.TexturePaths.Add(TexturePathsNew[i]);
            }
        }
    }

    #endregion
}
