using Content.Shared.Medical;
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalRecords;

/// <summary>
/// Medical record for a crewmember.
/// Can be viewed and edited in a medical records console.
/// </summary>
[Serializable, NetSerializable, DataRecord]
public sealed record MedicalRecord
{
    /// <summary>
    /// Status of the person.
    /// </summary>
    [DataField]
    public MedicalStatus Status = MedicalStatus.None;

    /// <summary>
    /// When Status is one of dead, the reason for it.
    /// Should never be set otherwise.
    /// </summary>
    [DataField]
    public string? Reason;

    /// <summary>
    /// Medical history of the person.
    /// This should have charges and time served added after someone is detained.
    /// </summary>
    [DataField]
    public List<MedicalHistory> History = new();
}

/// <summary>
/// A line of medical activity and the time it was added at.
/// </summary>
[Serializable, NetSerializable]
public record struct MedicalHistory(TimeSpan AddTime, string Medical);
