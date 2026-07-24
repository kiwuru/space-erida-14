using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Shared.Preferences;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._DV.Traits.SplitPersonality;

public sealed class SplitPersonalityPickerWindow : DefaultWindow
{
    public event Action<int>? OnSlotChosen;

    private readonly List<int> _slots = new();
    private readonly ItemList _list;
    private readonly Button _confirm;
    private int? _selectedSlot;

    public SplitPersonalityPickerWindow(string targetName, IReadOnlyDictionary<int, HumanoidCharacterProfile> characters)
    {
        Title = Loc.GetString("split-personality-picker-title", ("name", targetName));
        SetSize = new Vector2(320, 420);
        MinSize = new Vector2(280, 320);

        var vbox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
        };

        vbox.AddChild(new Label
        {
            Text = Loc.GetString("split-personality-picker-hint"),
            HorizontalExpand = true,
            Margin = new Thickness(4, 4, 4, 4),
        });

        _confirm = new Button
        {
            Text = Loc.GetString("split-personality-picker-confirm"),
            Disabled = true,
            HorizontalAlignment = HAlignment.Right,
            Margin = new Thickness(4, 4, 4, 4),
        };
        _confirm.OnPressed += _ =>
        {
            if (_selectedSlot is { } slot)
                OnSlotChosen?.Invoke(slot);
        };

        _list = new ItemList
        {
            VerticalExpand = true,
            SelectMode = ItemList.ItemListSelectMode.Single,
        };

        foreach (var (slot, profile) in characters.OrderBy(c => c.Key))
        {
            _slots.Add(slot);
            _list.AddItem(profile.Name);
        }

        _list.OnItemSelected += args =>
        {
            _selectedSlot = _slots[args.ItemIndex];
            _confirm.Disabled = false;
        };

        vbox.AddChild(_list);
        vbox.AddChild(_confirm);

        Contents.AddChild(vbox);
    }
}
