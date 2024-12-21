using Robust.Shared.Configuration;

namespace Content.Shared.Imperial.ICCVar;

[CVarDefs]
// ReSharper disable once InconsistentNaming
public sealed class ICCVars
{
    /// <summary>
    /// Определяет, активна ли система ShowPopupOnJoin
    /// </summary>
    public static readonly CVarDef<bool>
        ShowPopupOnJoinEnabled = CVarDef.Create("imperial.show_popup_on_join", false, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Список названий popup-ов, которые необходимо отобразить игроку, разделённых через запятую
    /// </summary>
    public static readonly CVarDef<string>
        PopupsOnJoinToShow = CVarDef.Create("imperial.show_popup_on_join_list", "", CVar.SERVER | CVar.REPLICATED);

    /// Enables footprints 
    /// </summary>
    public static readonly CVarDef<bool>
        FootPrintsEnabled = CVarDef.Create("imperial.footprints_enabled", true, CVar.SERVERONLY);
    /// <summary>
    /// Enables autovote for map and preset in lobby
    /// </summary>
    public static readonly CVarDef<bool>
        VoteAutoStartInLobby = CVarDef.Create("vote.autostartinlobby", true, CVar.SERVERONLY);
    /// <summary>
    /// Timer for end round
    /// </summary>
    public static readonly CVarDef<int>
        GameEndRoundDuration = CVarDef.Create("game.endroundduration", 40, CVar.SERVERONLY);
    /// SpacingEscapeRatio Moved to CVars from SpacingEscapeRatio
    /// <summary>
    ///     What fraction of air from a spaced tile escapes every tick.
    ///     1.0 for instant spacing, 0.2 means 20% of remaining air lost each time
    /// </summary>
    public static readonly CVarDef<float>
        SpacingEscapeRatio = CVarDef.Create("atmos.spacingescaperatio", 1f, CVar.SERVERONLY);

    public static readonly CVarDef<bool>
        PsychosisEnabled = CVarDef.Create("psychosis.enabled", true, CVar.REPLICATED);
}
