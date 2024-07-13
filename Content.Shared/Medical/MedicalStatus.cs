namespace Content.Shared.Medical;

/// <summary>
/// Status used in Criminal Records.
///
/// None - the default value
/// Dead - dead
/// </summary>
public enum MedicalStatus : byte
{
    None,
    Dead,
    DeadWithoutSoul,
    DeadNonClone
}