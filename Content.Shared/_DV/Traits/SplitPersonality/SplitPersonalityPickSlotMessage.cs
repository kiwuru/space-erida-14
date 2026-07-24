using System;
using Robust.Shared.Serialization;
using Content.Shared.Eui;

namespace Content.Shared._DV.Traits.SplitPersonality;

/// <summary>
/// Sent client -> server when the player picks one of their own character slots
/// to use as the alter persona's name/appearance.
/// </summary>
[Serializable, NetSerializable]
public sealed class SplitPersonalityPickSlotMessage : EuiMessageBase
{
    public int Slot;

    public SplitPersonalityPickSlotMessage(int slot)
    {
        Slot = slot;
    }
}
