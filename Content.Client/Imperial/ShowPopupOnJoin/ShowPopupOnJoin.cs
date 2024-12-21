using Content.Client.Imperial.ShowPopupOnJoin.Prototypes;
using Content.Client.Imperial.ShowPopupOnJoin.UI;
using Content.Shared.CCVar;
using Content.Shared.Imperial.ICCVar;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.Imperial.ShowPopupOnJoin;

public sealed class ShowPopupOnJoin
{
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public void Open()
    {
        if (!_configManager.GetCVar(ICCVars.ShowPopupOnJoinEnabled))
            return;

        var nextPopup = GetNextPopup();

        if (nextPopup is not null)
            new PopupWindow(nextPopup).OpenCentered();
    }

    PopupWindowPrototype? GetNextPopup()
    {
        var popupsToShow = _configManager.GetCVar(ICCVars.PopupsOnJoinToShow).Split(',', StringSplitOptions.RemoveEmptyEntries);
        var readedPopups = PopupWindow.GetReaded();

        foreach (var popup in popupsToShow)
            if (!readedPopups.Contains(popup))
                if (_prototypeManager.TryIndex<PopupWindowPrototype>(popup, out var proto))
                    return proto;

        return null;
    }
}
