using Content.Server._DV.Traits.SplitPersonality;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._DV.Commands;

/// <summary>
/// addsplitpersonality &lt;uid&gt; - admin-only. Adds the split-personality trait to an
/// entity. If a player is (or later becomes) attached to it, they'll be prompted to pick
/// one of their own saved characters to use as the alternate persona.
/// </summary>
[AdminCommand(AdminFlags.Fun)]
public sealed partial class AddSplitPersonalityCommand : LocalizedEntityCommands
{
    [Dependency] private SplitPersonalitySystem _splitPersonality = default!;

    public override string Command => "addsplitpersonality";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var entityUidNet) || !EntityManager.TryGetEntity(entityUidNet, out var entityUid))
        {
            shell.WriteError(Loc.GetString("shell-could-not-find-entity-with-uid", ("uid", args[0])));
            return;
        }

        _splitPersonality.AdminAddSplitPersonality(entityUid.Value);
        shell.WriteLine(Loc.GetString("split-personality-command-added"));
    }
}
