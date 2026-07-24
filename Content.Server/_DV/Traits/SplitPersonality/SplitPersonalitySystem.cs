using System.Collections.Generic;
using System.Linq;
using Content.Server.EUI;
using Content.Shared._DV.Traits.SplitPersonality;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._DV.Traits.SplitPersonality;

public sealed partial class SplitPersonalitySystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private Content.Server.Body.VisualBodySystem _visualBody = default!;
    [Dependency] private EuiManager _euiManager = default!;

    private static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";
    private static readonly EntProtoId ToggleActionId = "ActionSplitPersonalityToggle";

    /// <summary>
    /// Which visual layers get pulled from the chosen character slot.
    /// - Hair: hair style + colour.
    /// - HeadTop: horns (this fork's Demon horn markings live here).
    /// - Chest: the "Succubus" wings+tail marking (id ADTAllsuccubus) lives here, along with
    ///   any other Chest markings (spots/stripes/etc) - those get swapped too as a side effect,
    ///   since Chest is a single stackable layer and can't be split further without picking
    ///   apart individual marking ids. If that's not what you want, see the README.
    /// </summary>
    public static readonly HashSet<HumanoidVisualLayers> SwapLayers = new()
    {
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.HeadTop,
        HumanoidVisualLayers.Chest,
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SplitPersonalityComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SplitPersonalityComponent, SplitPersonalityToggleEvent>(OnToggle);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnMapInit(EntityUid uid, SplitPersonalityComponent comp, MapInitEvent args)
    {
        EnsureAppearanceSnapshot(uid, comp);
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (!TryComp<SplitPersonalityComponent>(ev.Entity, out var comp))
            return;

        if (comp.AlterConfigured)
            return;

        OpenPicker(ev.Entity, ev.Player);
    }

    /// <summary>
    /// Called by the "addsplitpersonality" admin command. Adds the component if missing,
    /// grants the toggle action, snapshots the current appearance as "primary", and - if
    /// a player is currently attached - immediately opens the character picker for them.
    /// </summary>
    public void AdminAddSplitPersonality(EntityUid uid)
    {
        var comp = EnsureComp<SplitPersonalityComponent>(uid);
        EnsureAppearanceSnapshot(uid, comp);
        _actions.AddAction(uid, ToggleActionId);

        if (TryComp<ActorComponent>(uid, out var actor))
            OpenPicker(uid, actor.PlayerSession);
    }

    private void OpenPicker(EntityUid uid, ICommonSession player)
    {
        _euiManager.OpenEui(new SplitPersonalityPickerEui(GetNetEntity(uid), this), player);
    }

    /// <summary>
    /// Called by <see cref="SplitPersonalityPickerEui"/> once the player has picked a slot.
    /// </summary>
    public void ApplyCharacterAsAlter(EntityUid uid, HumanoidCharacterProfile profile)
    {
        if (!TryComp<SplitPersonalityComponent>(uid, out var comp))
            return;

        EnsureAppearanceSnapshot(uid, comp);

        comp.AlterName = profile.Name;
        comp.AlterEyeColor = profile.Appearance.EyeColor;

        var alterMarkings = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();
        foreach (var (category, layers) in profile.Appearance.Markings)
        {
            var filtered = layers.Where(l => SwapLayers.Contains(l.Key))
                .ToDictionary(l => l.Key, l => l.Value.ToList());

            if (filtered.Count > 0)
                alterMarkings[category] = filtered;
        }

        comp.AlterMarkings = alterMarkings;
        comp.AlterConfigured = true;

        Dirty(uid, comp);

        _popup.PopupEntity(Loc.GetString("split-personality-configured"), uid, uid);
    }

    private void EnsureAppearanceSnapshot(EntityUid uid, SplitPersonalityComponent comp)
    {
        if (!comp.PrimaryNameCaptured)
        {
            if (string.IsNullOrEmpty(comp.PrimaryName))
                comp.PrimaryName = MetaData(uid).EntityName;

            comp.PrimaryNameCaptured = true;
        }

        if (comp.AppearanceSnapshotCaptured)
            return;

        if (_visualBody.TryGatherMarkingsData(uid, SwapLayers, out var profiles, out var markingData, out var applied))
        {
            comp.PrimarySupportedLayers = markingData.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<HumanoidVisualLayers>(kvp.Value.Layers.Intersect(SwapLayers)));

            var snapshot = new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();
            foreach (var (category, supportedLayers) in comp.PrimarySupportedLayers)
            {
                var layerDict = new Dictionary<HumanoidVisualLayers, List<Marking>>();
                foreach (var layer in supportedLayers)
                {
                    layerDict[layer] = applied.TryGetValue(category, out var catMarkings) &&
                                        catMarkings.TryGetValue(layer, out var list)
                        ? list.ToList()
                        : new List<Marking>();
                }

                snapshot[category] = layerDict;
            }

            comp.PrimaryMarkingsSnapshot = snapshot;

            if (profiles.TryGetValue(EyesCategory, out var eyesProfile))
            {
                comp.PrimaryEyeColor = eyesProfile.EyeColor;
                comp.PrimaryEyeSex = eyesProfile.Sex;
            }
        }

        comp.AppearanceSnapshotCaptured = true;
        Dirty(uid, comp);
    }

    private void OnToggle(EntityUid uid, SplitPersonalityComponent comp, SplitPersonalityToggleEvent args)
    {
        if (args.Handled)
            return;

        if (!comp.AlterConfigured)
        {
            _popup.PopupEntity(Loc.GetString("split-personality-not-configured"), uid, uid);
            return;
        }

        if (_timing.CurTime < comp.NextSwitchAllowed)
        {
            _popup.PopupEntity(Loc.GetString("split-personality-too-soon"), uid, uid);
            return;
        }

        comp.IsAlterActive = !comp.IsAlterActive;
        comp.NextSwitchAllowed = _timing.CurTime + comp.SwitchCooldown;

        ApplyName(uid, comp);
        ApplyAppearance(uid, comp);

        _popup.PopupEntity(
            Loc.GetString(comp.IsAlterActive ? "split-personality-switch-alter" : "split-personality-switch-primary"),
            uid,
            uid);

        Dirty(uid, comp);
        args.Handled = true;
    }

    private void ApplyName(EntityUid uid, SplitPersonalityComponent comp)
    {
        var newName = comp.IsAlterActive ? comp.AlterName : comp.PrimaryName;
        if (!string.IsNullOrEmpty(newName))
            _metaData.SetEntityName(uid, newName);
    }

    private void ApplyAppearance(EntityUid uid, SplitPersonalityComponent comp)
    {
        if (!comp.IsAlterActive)
        {
            // Snapshot always has explicit entries (even empty ones) for every layer we
            // ever touch, so this reliably reverts hair/horns and their colours.
            _visualBody.ApplyMarkings(uid, comp.PrimaryMarkingsSnapshot);

            if (comp.PrimaryEyeColor is { } primaryEye)
            {
                _visualBody.ApplyProfiles(uid, new Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>
                {
                    [EyesCategory] = new() { EyeColor = primaryEye, Sex = comp.PrimaryEyeSex },
                });
            }

            return;
        }

        // Start from the primary snapshot (so anything not covered by AlterMarkings is left
        // alone) and overlay whatever the chosen character has on the swap layers.
        var alterMarkings = comp.PrimaryMarkingsSnapshot
            .ToDictionary(cat => cat.Key, cat => cat.Value.ToDictionary(l => l.Key, l => l.Value.ToList()));

        foreach (var (category, layers) in comp.AlterMarkings)
        {
            if (!alterMarkings.TryGetValue(category, out var catDict))
            {
                catDict = new Dictionary<HumanoidVisualLayers, List<Marking>>();
                alterMarkings[category] = catDict;
            }

            foreach (var (layer, markings) in layers)
            {
                catDict[layer] = markings.ToList();
            }
        }

        _visualBody.ApplyMarkings(uid, alterMarkings);

        if (comp.AlterEyeColor is { } alterEye)
        {
            _visualBody.ApplyProfiles(uid, new Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>
            {
                [EyesCategory] = new() { EyeColor = alterEye, Sex = comp.PrimaryEyeSex },
            });
        }
    }
}
