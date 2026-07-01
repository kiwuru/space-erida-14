using System.Linq;
using Content.Shared._Erida.ChangeSprite;
using Content.Shared._Erida.ChangeSprite.Components;
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
            _spriteSystem.RemoveLayer(uid, 0);

        foreach (var layer in prototype.Layers)
        {
            _spriteSystem.AddLayer(uid, layer);
        }
    }
}
