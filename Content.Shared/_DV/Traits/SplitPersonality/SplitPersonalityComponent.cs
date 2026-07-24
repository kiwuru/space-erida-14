using System;
using System.Collections.Generic;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits.SplitPersonality;

/// <summary>
/// Marks an entity as having a second, alternate persona that can be toggled via an action.
///
/// This is an admin-only feature - there is no way for a player to give this to themselves
/// through character creation or any in-round means. Admins add it with the
/// "addsplitpersonality" console command. If the target has a player attached, that player
/// is prompted (via a picker window) to choose one of their OWN saved character slots to use
/// as the alternate persona's name and appearance. If nobody is attached yet, the prompt is
/// shown automatically the next time a player attaches to the entity.
///
/// Toggling swaps the entity's display name and, for a configured set of visual layers
/// (hair and horns by default - see <see cref="SplitPersonalitySystem.SwapLayers"/>), swaps
/// in whatever markings/colours that layer has on the chosen character.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SplitPersonalityComponent : Component
{
    /// <summary>
    /// The "default" name - what the entity is called normally. Captured automatically
    /// the first time this component is set up (either on MapInit, or when an admin adds
    /// it to an already-spawned entity), so this doesn't need to be set manually.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PrimaryName = string.Empty;

    /// <summary>
    /// The alternate persona's name. Filled in automatically once the attached player picks
    /// a character slot in the picker window.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string AlterName = string.Empty;

    /// <summary>
    /// Whether the alternate persona is currently active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsAlterActive;

    /// <summary>
    /// Minimum time between switches, to stop it being spammed as an emote.
    /// </summary>
    [DataField]
    public TimeSpan SwitchCooldown = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Server game-time after which switching is allowed again.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan NextSwitchAllowed = TimeSpan.Zero;

    /// <summary>
    /// Whether an attached player has picked a character slot for the alter persona yet.
    /// While false, the action to switch does nothing (there's nothing to switch to), and
    /// a picker window will be (re-)offered the next time a player attaches to this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AlterConfigured;

    // ---- Appearance the alter persona uses (captured from the chosen character slot) ----

    [DataField, AutoNetworkedField]
    public Color? AlterEyeColor;

    /// <summary>
    /// Marking data for the alter persona, restricted to <see cref="SplitPersonalitySystem.SwapLayers"/>
    /// and copied wholesale (id + colour) from whichever character slot was picked. Same shape as
    /// <see cref="HumanoidCharacterAppearance.Markings"/> / <see cref="PrimaryMarkingsSnapshot"/>.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> AlterMarkings = new();

    // ---- Runtime snapshot of the primary appearance, captured automatically ----

    [DataField]
    public bool AppearanceSnapshotCaptured;

    [DataField]
    public bool PrimaryNameCaptured;

    [DataField]
    public Color? PrimaryEyeColor;

    [DataField]
    public Sex PrimaryEyeSex;

    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> PrimaryMarkingsSnapshot = new();

    /// <summary>
    /// Which layers each organ category actually supports (even if currently empty) -
    /// needed so we can reliably add/remove markings on a layer, not just recolour
    /// whatever's already there.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<OrganCategoryPrototype>, HashSet<HumanoidVisualLayers>> PrimarySupportedLayers = new();
}
