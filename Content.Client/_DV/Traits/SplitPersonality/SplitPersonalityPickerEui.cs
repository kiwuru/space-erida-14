using System.Collections.Generic;
using Content.Client.Eui;
using Content.Client.Lobby;
using Content.Shared._DV.Traits.SplitPersonality;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Shared.IoC;

namespace Content.Client._DV.Traits.SplitPersonality;

[UsedImplicitly]
public sealed partial class SplitPersonalityPickerEui : BaseEui
{
    [Dependency] private IClientPreferencesManager _prefsManager = default!;

    private SplitPersonalityPickerWindow? _window;

    public SplitPersonalityPickerEui()
    {
        IoCManager.InjectDependencies(this);
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not SplitPersonalityPickerEuiState s)
            return;

        _window?.Close();

        var characters = _prefsManager.Preferences?.Characters ?? new Dictionary<int, Content.Shared.Preferences.HumanoidCharacterProfile>();

        _window = new SplitPersonalityPickerWindow(s.TargetName, characters);
        _window.OnSlotChosen += slot =>
        {
            SendMessage(new SplitPersonalityPickSlotMessage(slot));
            _window?.Close();
        };
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();
        _window?.Close();
    }
}
