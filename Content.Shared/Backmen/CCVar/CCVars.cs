using Robust.Shared;
using Robust.Shared.Configuration;
namespace Content.Shared.Backmen.CCVar;
// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class CCVars
{
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
        CVarDef.Create("ghost.respawn_time", 15f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> GhostRespawnMaxPlayers =
        CVarDef.Create("ghost.respawn_max_players", 20, CVar.SERVERONLY);
}
