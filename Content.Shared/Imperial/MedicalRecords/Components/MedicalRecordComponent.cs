using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MedicalRecordComponent : Component
{
    /// <summary>
    ///     The icon that should be displayed based on the criminal status of the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<StatusIconPrototype> StatusIcon = "SecurityIconWanted";
}
