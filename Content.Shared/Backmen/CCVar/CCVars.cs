﻿using Robust.Shared.Configuration;

namespace Content.Shared.Backmen.CCVar;

[CVarDefs]
public sealed partial class CCVars
{
    /// <summary>
    ///     With completely default supermatter values, Singuloose delamination will occur if engineers inject at least 900 moles of coolant per tile
    ///     in the crystal chamber. For reference, a gas canister contains 1800 moles of air. This Cvar directly multiplies the amount of moles required to singuloose.
    /// </summary>
    public static readonly CVarDef<float> SupermatterSingulooseMolesModifier =
        CVarDef.Create("supermatter.singuloose_moles_modifier", 1f, CVar.SERVER);

    /// <summary>
    ///     Toggles whether or not Singuloose delaminations can occur. If both Singuloose and Tesloose are disabled, it will always delam into a Nuke.
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoSingulooseDelam =
        CVarDef.Create("supermatter.do_singuloose", true, CVar.SERVER);

    /// <summary>
    ///     By default, Supermatter will "Tesloose" if the conditions for Singuloose are not met, and the core's power is at least 4000.
    ///     The actual reasons for being at least this amount vary by how the core was screwed up, but traditionally it's caused by "The core is on fire".
    ///     This Cvar multiplies said power threshold for the purpose of determining if the delam is a Tesloose.
    /// </summary>
    public static readonly CVarDef<float> SupermatterTesloosePowerModifier =
        CVarDef.Create("supermatter.tesloose_power_modifier", 1f, CVar.SERVER);

    /// <summary>
    ///     Toggles whether or not Tesloose delaminations can occur. If both Singuloose and Tesloose are disabled, it will always delam into a Nuke.
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoTeslooseDelam =
        CVarDef.Create("supermatter.do_tesloose", true, CVar.SERVER);

    /// <summary>
    ///     When true, bypass the normal checks to determine delam type, and instead use the type chosen by supermatter.forced_delam_type
    /// </summary>
    public static readonly CVarDef<bool> SupermatterDoForceDelam =
        CVarDef.Create("supermatter.do_force_delam", false, CVar.SERVER);

    /// <summary>
    ///     If supermatter.do_force_delam is true, this determines the delamination type, bypassing the normal checks.
    /// </summary>
    public static readonly CVarDef<string> SupermatterForcedDelamType =
        CVarDef.Create("supermatter.forced_delam_type", "Singulo", CVar.SERVER);

    /// <summary>
    ///     Directly multiplies the amount of rads put out by the supermatter. Be VERY conservative with this.
    /// </summary>
    public static readonly CVarDef<float> SupermatterRadsModifier =
        CVarDef.Create("supermatter.rads_modifier", 1f, CVar.SERVER);

    public static readonly CVarDef<bool>
        GameDiseaseEnabled = CVarDef.Create("game.disease", true, CVar.SERVERONLY);
    /// <summary>
    /// Shipwrecked
    /// </summary>
    public static readonly CVarDef<int> ShipwreckedMaxPlayers =
        CVarDef.Create("shipwrecked.max_players", 15);
    
    /*
     * Blob
     */
    /* public static readonly CVarDef<int> BlobMax =
    *    CVarDef.Create("blob.max", 3, CVar.SERVERONLY);
   * public static readonly CVarDef<int> BlobPlayersPer =
   *     CVarDef.Create("blob.players_per", 20, CVar.SERVERONLY);
*    public static readonly CVarDef<bool> BlobCanGrowInSpace =
 *       CVarDef.Create("blob.grow_space", true, CVar.REPLICATED);
 */
    /*
     * Ghost Respawn
     */

    public static readonly CVarDef<float> GhostRespawnTime =
        CVarDef.Create("ghost.respawn_time", 20f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> GhostRespawnMaxPlayers =
        CVarDef.Create("ghost.respawn_max_players", 30, CVar.SERVERONLY);
}
