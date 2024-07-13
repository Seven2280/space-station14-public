using Content.Shared.Medical;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Robust.Shared.Serialization;
namespace Content.Shared.MedicalRecords;

[Serializable, NetSerializable]
public enum MedicalRecordsConsoleKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MedicalRecordsConsoleState : BoundUserInterfaceState
{
    /// <summary>
    /// Currently selected crewmember record key.
    /// </summary>
    public uint? SelectedKey = null;

    public MedicalRecord? MedicalRecord = null;
    public GeneralStationRecord? StationRecord = null;
    public readonly Dictionary<uint, string>? RecordListing;
    public readonly StationRecordsFilter? Filter;

    public MedicalRecordsConsoleState(Dictionary<uint, string>? recordListing, StationRecordsFilter? newFilter)
    {
        RecordListing = recordListing;
        Filter = newFilter;
    }

    /// <summary>
    /// Default state for opening the console
    /// </summary>
    public MedicalRecordsConsoleState() : this(null, null)
    {
    }

    public bool IsEmpty() => SelectedKey == null && StationRecord == null && MedicalRecord == null && RecordListing == null;
}

/// <summary>
/// Used to change status
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordChangeStatus : BoundUserInterfaceMessage
{
    public readonly MedicalStatus Status;
    public readonly string? Reason;

    public MedicalRecordChangeStatus(MedicalStatus status, string? reason)
    {
        Status = status;
        Reason = reason;
    }
}
[Serializable, NetSerializable]
public sealed class PrintMedicalCard : BoundUserInterfaceMessage
{
    public readonly uint? SelectedKey;
    public PrintMedicalCard(uint? selectedKey)
    {
        SelectedKey = selectedKey;
    }
}

/// <summary>
/// Used to add a single line to the record's medical history.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordAddHistory : BoundUserInterfaceMessage
{
    public readonly string Line;

    public MedicalRecordAddHistory(string line)
    {
        Line = line;
    }
}

/// <summary>
/// Used to delete a single line from the medical history, by index.
/// </summary>
[Serializable, NetSerializable]
public sealed class MedicalRecordDeleteHistory : BoundUserInterfaceMessage
{
    public readonly uint Index;

    public MedicalRecordDeleteHistory(uint index)
    {
        Index = index;
    }
}
