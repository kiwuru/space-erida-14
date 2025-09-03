using System.Diagnostics;
using Content.Server.Chat.Systems;
using Content.Server.Jittering;
using Content.Shared.Clothing;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Erida.ERP.Vibrator;

public sealed class VibratorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggleSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VibratorComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<VibratorComponent, ClothingGotUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<VibratorComponent, ItemToggledEvent>(OnItemToggled);
        SubscribeLocalEvent<VibratorComponent, SignalReceivedEvent>(OnSignalReceived);
    }

    private void OnEquipped(EntityUid uid, VibratorComponent component, ref ClothingGotEquippedEvent args)
    {
        component.User = args.Wearer;
    }

    private void OnUnequipped(EntityUid uid, VibratorComponent component, ref ClothingGotUnequippedEvent args)
    {
        component.User = null;
    }

    private void OnItemToggled(EntityUid uid, VibratorComponent component, ItemToggledEvent args)
    {
        component.IsActive = args.Activated;

        var audioParams = AudioParams.Default.WithLoop(true).WithMaxDistance(1);
        _audioSystem.Stop(component.Stream);

        if (args.Activated)
            component.Stream = _audioSystem.PlayPvs(component.VibrationSound, uid, audioParams)?.Entity;
    }

    private void OnSignalReceived(EntityUid uid, VibratorComponent component, SignalReceivedEvent args)
    {
        switch (args.Port)
        {
            case "On":
                _itemToggleSystem.TryActivate(uid);
                break;
            case "Off":
                _itemToggleSystem.TryDeactivate(uid);
                break;
            case "Toggle":
                if (component.IsActive)
                {
                    _itemToggleSystem.TryDeactivate(uid);
                    break;
                }
                _itemToggleSystem.TryActivate(uid);
                break;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var curTime = _gameTiming.CurTime;

        var query = EntityQueryEnumerator<VibratorComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (curTime < component.NextEmoteTime || component.User is null || !component.IsActive && component.IsTogglable)
                continue;

            if (_random.Next(1, 101) <= component.JitterProbablity)
            {
                _jitter.DoJitter(component.User.Value, TimeSpan.FromSeconds(1), true, 2, 2);

                if (_random.Next(1, 101) <= component.MoanProbablity)
                    _chatSystem.TryEmoteWithoutChat(component.User.Value, "Moan");
            }
            component.NextEmoteTime = curTime + component.Interval;
        }
    }
}
