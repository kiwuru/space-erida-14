using Content.Server.EUI;
using Content.Server._DV.Traits.SplitPersonality;
using Content.Server.Preferences.Managers;
using Content.Shared._DV.Traits.SplitPersonality;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.IoC;

namespace Content.Server._DV.Traits.SplitPersonality;

[UsedImplicitly]
public sealed partial class SplitPersonalityPickerEui : BaseEui
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IServerPreferencesManager _prefsManager = default!;

    private readonly NetEntity _target;
    private readonly SplitPersonalitySystem _system;

    public SplitPersonalityPickerEui(NetEntity target, SplitPersonalitySystem system)
    {
        _target = target;
        _system = system;
        IoCManager.InjectDependencies(this);
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        var name = "???";
        if (_entManager.TryGetEntity(_target, out var uid) &&
            _entManager.TryGetComponent<MetaDataComponent>(uid, out var meta))
        {
            name = meta.EntityName;
        }

        return new SplitPersonalityPickerEuiState { TargetName = name };
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not SplitPersonalityPickSlotMessage pick)
            return;

        if (!_entManager.TryGetEntity(_target, out var uid))
        {
            Close();
            return;
        }

        var prefs = _prefsManager.GetPreferences(Player.UserId);
        if (!prefs.Characters.TryGetValue(pick.Slot, out var profile))
        {
            Close();
            return;
        }

        _system.ApplyCharacterAsAlter(uid.Value, profile);
        Close();
    }
}
