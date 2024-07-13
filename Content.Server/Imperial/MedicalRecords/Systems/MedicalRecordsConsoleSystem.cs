using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.MedicalRecords;
using Content.Shared.MedicalRecords.Components;
using Content.Shared.MedicalRecords.Systems;
using Content.Shared.Medical;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical.Components;
using Content.Server.Paper;
using Robust.Shared.Utility;

namespace Content.Server.MedicalRecords.Systems;


/// <summary>
/// Handles all UI for medical records console
/// </summary>
public sealed class MedicalRecordsConsoleSystem : SharedMedicalRecordsConsoleSystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly MedicalRecordsSystem _medicalRecords = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<MedicalRecordsConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<MedicalRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(UpdateUserInterface);

        Subs.BuiEvents<MedicalRecordsConsoleComponent>(MedicalRecordsConsoleKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(UpdateUserInterface);
            subs.Event<SelectStationRecord>(OnKeySelected);
            subs.Event<SetStationRecordFilter>(OnFiltersChanged);
            subs.Event<MedicalRecordChangeStatus>(OnChangeStatus);
            subs.Event<MedicalRecordAddHistory>(OnAddHistory);
            subs.Event<MedicalRecordDeleteHistory>(OnDeleteHistory);
            subs.Event<PrintMedicalCard>(OnPrint);
        });
    }

    private void UpdateUserInterface<T>(Entity<MedicalRecordsConsoleComponent> ent, ref T args)
    {
        // wizden: TODO: this is probably wasteful, maybe better to send a message to modify the exact state?
        UpdateUserInterface(ent);
    }

    private void OnKeySelected(Entity<MedicalRecordsConsoleComponent> ent, ref SelectStationRecord msg)
    {
        // wizden: no concern of sus client since record retrieval will fail if invalid id is given
        ent.Comp.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(ent);
    }

    private void OnFiltersChanged(Entity<MedicalRecordsConsoleComponent> ent, ref SetStationRecordFilter msg)
    {
        if (ent.Comp.Filter == null ||
            ent.Comp.Filter.Type != msg.Type || ent.Comp.Filter.Value != msg.Value)
        {
            ent.Comp.Filter = new StationRecordsFilter(msg.Type, msg.Value);
            UpdateUserInterface(ent);
        }
    }

    private void OnChangeStatus(Entity<MedicalRecordsConsoleComponent> ent, ref MedicalRecordChangeStatus msg)
    {
        // wizden: prevent malf client violating wanted/reason nullability
        if ((msg.Status == MedicalStatus.Dead || msg.Status == MedicalStatus.DeadNonClone || msg.Status == MedicalStatus.DeadWithoutSoul) && msg.Reason == null)
            return;

        if (!CheckSelected(ent, msg.Session, out var mob, out var key))
            return;

        if (!_stationRecords.TryGetRecord<MedicalRecord>(key.Value, out var record) || record.Status == msg.Status)
            return;
        // validate the reason
        string? reason = null;
        if (msg.Reason != null)
        {
            reason = msg.Reason.Trim();
            if (reason.Length < 1 || reason.Length > ent.Comp.MaxStringLength)
                return;
        }

        // when setting someone add it to history automatically
        // fallback exists if the player was not set to wanted beforehand
        if (msg.Status == MedicalStatus.Dead || msg.Status == MedicalStatus.DeadNonClone || msg.Status == MedicalStatus.DeadWithoutSoul)
        {
            var oldReason = msg.Reason ?? Loc.GetString("medical-records-console-unspecified-reason");
            var history = Loc.GetString("medical-records-console-auto-history", ("reason", oldReason));
            _medicalRecords.TryAddHistory(key.Value, history);
        }

        var oldStatus = record.Status;

        // will probably never fail given the checks above
        _medicalRecords.TryChangeStatus(key.Value, msg.Status, msg.Reason);

        var name = RecordName(key.Value);
        var officer = Loc.GetString("medical-records-console-unknown-officer");
        if (_idCard.TryFindIdCard(mob.Value, out var id) && id.Comp.FullName is { } fullName)
            officer = fullName;

        (string, object)[] args;
        if (reason != null)
            args = new (string, object)[] { ("name", name), ("officer", officer), ("reason", reason) };
        else
            args = new (string, object)[] { ("name", name), ("officer", officer) };

        // figure out which radio message to send depending on transition
        var statusString = (oldStatus, msg.Status) switch
        {
            (_, MedicalStatus.Dead) => "dead",
            (_, MedicalStatus.DeadNonClone) => "dead-non-clone",
            (_, MedicalStatus.DeadWithoutSoul) => "dead-without-soul",
            // this is impossible
            _ => "none"
        };
        _radio.SendRadioMessage(ent, Loc.GetString($"medical-records-console-{statusString}", args),
            ent.Comp.MedicalChannel, ent);

        UpdateUserInterface(ent);
        UpdateMedicalIdentity(name, msg.Status);
    }

    private void OnAddHistory(Entity<MedicalRecordsConsoleComponent> ent, ref MedicalRecordAddHistory msg)
    {
        if (!CheckSelected(ent, msg.Session, out _, out var key))
            return;

        var line = msg.Line.Trim();
        if (line.Length < 1 || line.Length > ent.Comp.MaxStringLength)
            return;

        if (!_medicalRecords.TryAddHistory(key.Value, line))
            return;

        // no radio message since its not crucial to officers patrolling

        UpdateUserInterface(ent);
    }

    private void OnDeleteHistory(Entity<MedicalRecordsConsoleComponent> ent, ref MedicalRecordDeleteHistory msg)
    {
        if (!CheckSelected(ent, msg.Session, out _, out var key))
            return;

        if (!_medicalRecords.TryDeleteHistory(key.Value, msg.Index))
            return;

        // a bit sus but not crucial to officers patrolling

        UpdateUserInterface(ent);
    }

    private void UpdateUserInterface(Entity<MedicalRecordsConsoleComponent> ent)
    {
        var (uid, console) = ent;
        var owningStation = _station.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecords))
        {
            _ui.TrySetUiState(uid, MedicalRecordsConsoleKey.Key, new MedicalRecordsConsoleState());
            return;
        }

        var listing = _stationRecords.BuildListing((owningStation.Value, stationRecords), console.Filter);

        var state = new MedicalRecordsConsoleState(listing, console.Filter);
        if (console.ActiveKey is { } id)
        {
            // get records to display when a crewmember is selected
            var key = new StationRecordKey(id, owningStation.Value);
            _stationRecords.TryGetRecord(key, out state.StationRecord, stationRecords);
            _stationRecords.TryGetRecord(key, out state.MedicalRecord, stationRecords);
            state.SelectedKey = id;
        }

        _ui.TrySetUiState(uid, MedicalRecordsConsoleKey.Key, state);
    }

    /// <summary>
    /// Boilerplate that most actions use, if they require that a record be selected.
    /// Obviously shouldn't be used for selecting records.
    /// </summary>
    private bool CheckSelected(Entity<MedicalRecordsConsoleComponent> ent, ICommonSession session,
        [NotNullWhen(true)] out EntityUid? mob, [NotNullWhen(true)] out StationRecordKey? key)
    {
        key = null;
        mob = null;
        if (session.AttachedEntity is not { } user)
            return false;

        if (!_access.IsAllowed(user, ent))
        {
            _popup.PopupEntity(Loc.GetString("medical-records-permission-denied"), ent, session);
            return false;
        }

        if (ent.Comp.ActiveKey is not { } id)
            return false;

        // checking the console's station since the user might be off-grid using on-grid console
        if (_station.GetOwningStation(ent) is not { } station)
            return false;

        key = new StationRecordKey(id, station);
        mob = user;
        return true;
    }

    /// <summary>
    /// Gets the name from a record, or empty string if this somehow fails.
    /// </summary>
    private string RecordName(StationRecordKey key)
    {
        if (!_stationRecords.TryGetRecord<GeneralStationRecord>(key, out var record))
            return "";

        return record.Name;
    }

    private GeneralStationRecord GetGeneralRecord(StationRecordKey key)
    {
        if (!_stationRecords.TryGetRecord<GeneralStationRecord>(key, out var record))
            return new GeneralStationRecord();
        return record;
    }
    private MedicalRecord GetMedicalRecord(Entity<MedicalRecordsConsoleComponent> ent)
    {
        var (uid, console) = ent;
        var owningStation = _station.GetOwningStation(uid);
        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecords))
            return new MedicalRecord();
        if (console.ActiveKey is { } id)
        {
            // get records to display when a crewmember is selected
            var key = new StationRecordKey(id, owningStation.Value);
            _stationRecords.TryGetRecord<MedicalRecord>(key, out var medicalRecord, stationRecords);
            if (medicalRecord == null) {
                return new MedicalRecord();
            }
            return medicalRecord;
        }
        return new MedicalRecord();
    }

    /// <summary>
    /// Checks if the new identity's name has a medical record attached to it, and gives the entity the icon that
    /// belongs to the status if it does.
    /// </summary>
    public void CheckNewIdentity(EntityUid uid)
    {
        var name = Identity.Name(uid, EntityManager);
        var xform = Transform(uid);
        var station = _station.GetStationInMap(xform.MapID);

        if (station != null && _stationRecords.GetRecordByName(station.Value, name) is { } id)
        {
            if (_stationRecords.TryGetRecord<MedicalRecord>(new StationRecordKey(id, station.Value),
                    out var record))
            {
                if (record.Status != MedicalStatus.None)
                {
                    // Может быть, стоит сделать иконки для некоторых статусов. Но пока не вижу нужды   
                    // SetMedicalIcon(name, record.Status, uid);
                    return;
                }
            }
        }
        RemComp<MedicalRecordComponent>(uid);
    }
    private FormattedMessage GetMedicalReportText(GeneralStationRecord generalRecord, MedicalRecord medicalRecord, string medic, string medicJob)
    {
        var msg = new FormattedMessage();
        for (var i = 0; i < 7; i++)
        {
            msg.AddMarkup(Loc.GetString($"medical-report-header{i}"));
            msg.PushNewline();
        }
        for (var i = 0; i < 8; i++)
        {
            if (i == 4)
                msg.AddMarkup(Loc.GetString($"medical-report-requisites{i}", ("date", DateTime.Now.AddYears(1000).ToString("MM/dd/yyyy"))));
            else if (i == 5)
                msg.AddMarkup(Loc.GetString($"medical-report-requisites{i}", ("medicName", medic)));
            else if (i == 6)
                msg.AddMarkup(Loc.GetString($"medical-report-requisites{i}", ("medicJob", medicJob)));
            else
                msg.AddMarkup(Loc.GetString($"medical-report-requisites{i}"));
            msg.PushNewline();
        }
        for (var i = 0; i < 7; i++)
        {
            if (i == 2)
                msg.AddMarkup(Loc.GetString($"medical-report-data{i}", ("patientName", generalRecord.Name)));
            else if (i == 3)
                msg.AddMarkup(Loc.GetString($"medical-report-data{i}", ("patientJob", generalRecord.JobTitle)));
            else if (i == 4)
                msg.AddMarkup(Loc.GetString($"medical-report-data{i}", ("patientAge", generalRecord.Age)));
            else if (i == 5)
                msg.AddMarkup(Loc.GetString($"medical-report-data{i}", ("patientStatus", Loc.GetString(medicalRecord.Status.ToString()))));
            else
                msg.AddMarkup(Loc.GetString($"medical-report-data{i}"));
            msg.PushNewline();
        }
        for (var i = 0; i < medicalRecord.History.Count; i++) {
            msg.AddMarkup(Loc.GetString("medical-report-data-history-newline", ("i", i + 1)));
            msg.PushNewline();
            msg.AddMarkup(Loc.GetString("medical-report-data-history-time", ("time", medicalRecord.History[i].AddTime.ToString().Substring(0,5))));
            msg.PushNewline();
            msg.AddMarkup(Loc.GetString("medical-report-data-history-text", ("text", medicalRecord.History[i].Medical)));
            msg.PushNewline();
        }
        msg.AddMarkup(Loc.GetString($"medical-report-data7"));
        return msg;
    }
    private void OnPrint(Entity<MedicalRecordsConsoleComponent> ent, ref PrintMedicalCard msg)
    {
        if (!CheckSelected(ent, msg.Session, out var mob, out var key))
            return;
        var generalRecord = GetGeneralRecord(key.Value);
        var medicalRecord = GetMedicalRecord(ent);
        string medic = Loc.GetString("medical-report-unknown-medic");
        string medicJob = Loc.GetString("medical-report-unknown-medic-job");

        if (_idCard.TryFindIdCard(mob.Value, out var id) && id.Comp.FullName is { } fullName && id.Comp.JobTitle is { })
        {
            medic = fullName;
            medicJob = id.Comp.JobTitle;
        }
        var report = Spawn(ent.Comp.ReportEntityId, Transform(ent.Owner).Coordinates);
        _metaSystem.SetEntityName(report, Loc.GetString("medical-report-name", ("name", generalRecord.Name)));
        var content = GetMedicalReportText(generalRecord, medicalRecord, medic, medicJob);
        _paper.SetContent(report, content.ToMarkup());
    }
}
