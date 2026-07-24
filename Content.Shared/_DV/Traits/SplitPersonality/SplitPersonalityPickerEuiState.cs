using System;
using Robust.Shared.Serialization;
using Content.Shared.Eui;

namespace Content.Shared._DV.Traits.SplitPersonality;

[Serializable, NetSerializable]
public sealed class SplitPersonalityPickerEuiState : EuiStateBase
{
    /// <summary>
    /// Display name of the puppet the trait was added to, shown in the window title.
    /// </summary>
    public string TargetName = string.Empty;
}
