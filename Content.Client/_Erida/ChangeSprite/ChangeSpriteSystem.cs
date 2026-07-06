// SPDX-FileCopyrightText: 2026 vanomorodellefake <vanomorodellefake29@gmail.com>
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Client.DamageState;
using Content.Shared._Erida.ChangeSprite;
using Content.Shared._Erida.ChangeSprite.Components;
using Content.Shared.Mobs;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Erida.ChangeSprite;

public sealed partial class ChangeSpriteVisualizerSystem : VisualizerSystem<ChangeSpriteComponent>
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SpriteSystem _spriteSystem = default!;

    protected override void OnAppearanceChange(EntityUid uid, ChangeSpriteComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<ProtoId<ChangeSpritePrototype>>(uid, ChangeSpriteVisuals.SpriteId, out var spriteId, args.Component))
            return;

        if (!_proto.TryIndex(spriteId, out var prototype))
            return;

        while (args.Sprite.AllLayers.Count() > 0)
        {
            _spriteSystem.RemoveLayer(uid, 0);
        }

        foreach (var layer in prototype.Layers)
        {
            var layerId = _spriteSystem.AddLayer(uid, layer.Sprite);
            if (Enum.TryParse<DamageStateVisualLayers>(layer.LayerKey, out var damageLayer))
            {
                _spriteSystem.LayerMapSet(uid, damageLayer, layerId);
            }
            _spriteSystem.LayerSetVisible(uid, layerId, layer.Visible);
        }

        if (!TryComp<DamageStateVisualsComponent>(uid, out var damageStateComp))
            return;

        if (prototype.AliveStateBase != null)
            damageStateComp.States[MobState.Alive][DamageStateVisualLayers.Base] = prototype.AliveStateBase;
        if (prototype.AliveStateBaseUnshaded != null)
            damageStateComp.States[MobState.Alive][DamageStateVisualLayers.BaseUnshaded] = prototype.AliveStateBaseUnshaded;

        if (prototype.CriticalStateBase != null)
            damageStateComp.States[MobState.Critical][DamageStateVisualLayers.Base] = prototype.CriticalStateBase;
        if (prototype.CriticalStateBaseUnshaded != null)
            damageStateComp.States[MobState.Critical][DamageStateVisualLayers.BaseUnshaded] = prototype.CriticalStateBaseUnshaded;

        if (prototype.DeadStateBase != null)
            damageStateComp.States[MobState.Dead][DamageStateVisualLayers.Base] = prototype.DeadStateBase;
        if (prototype.DeadStateBaseUnshaded != null)
            damageStateComp.States[MobState.Dead][DamageStateVisualLayers.BaseUnshaded] = prototype.DeadStateBaseUnshaded;
    }
}
