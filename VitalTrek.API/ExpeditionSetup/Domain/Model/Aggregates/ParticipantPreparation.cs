using VitalTrek.API.ExpeditionSetup.Domain.Model.ValueObjects;

namespace VitalTrek.API.ExpeditionSetup.Domain.Model.Aggregates;

/// <summary>
///     Aggregate root representing the preparation of a manifest participant before the tour: wearable assignment,
///     emergency contact, monitoring consent (Law N.° 29733) and device activation.
/// </summary>
public partial class ParticipantPreparation
{
    /// <summary>Default lower heart rate limit of the baseline range, in beats per minute.</summary>
    public const int DefaultBaselineMin = 50;

    /// <summary>Default upper heart rate limit of the baseline range, in beats per minute.</summary>
    public const int DefaultBaselineMax = 140;

    /// <summary>Gets the preparation identifier.</summary>
    public int Id { get; private set; }

    /// <summary>Gets the agency that owns the preparation.</summary>
    public int AgencyId { get; private set; }

    /// <summary>Gets the expedition group of the participant.</summary>
    public int ExpeditionGroupId { get; private set; }

    /// <summary>Gets the manifest entry of the participant.</summary>
    public int MemberId { get; private set; }

    /// <summary>Gets the assigned wearable device, if any.</summary>
    public int? WearableId { get; private set; }

    /// <summary>Gets a value indicating whether the participant granted monitoring consent.</summary>
    public bool Consent { get; private set; }

    /// <summary>Gets the moment the consent was last granted or revoked.</summary>
    public DateTimeOffset? ConsentRecordedAt { get; private set; }

    /// <summary>Gets a value indicating whether the wearable is active.</summary>
    public bool Activated { get; private set; }

    /// <summary>Gets the emergency contact name.</summary>
    public string? EmergencyContactName { get; private set; }

    /// <summary>Gets the emergency contact phone.</summary>
    public string? EmergencyContactPhone { get; private set; }

    /// <summary>Gets the emergency contact relationship.</summary>
    public string? EmergencyContactRelationship { get; private set; }

    /// <summary>Gets the lower heart rate limit considered normal for the participant.</summary>
    public int BaselineMin { get; private set; }

    /// <summary>Gets the upper heart rate limit considered normal for the participant.</summary>
    public int BaselineMax { get; private set; }

    /// <summary>Gets the moment the wearable was returned at closure.</summary>
    public DateTimeOffset? ReturnedAt { get; private set; }

    /// <summary>Gets the condition reported when the wearable was returned.</summary>
    public WearableDeviceStatus? ReturnCondition { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ParticipantPreparation"/> class for Entity Framework Core.
    /// </summary>
    public ParticipantPreparation()
    {
    }

    /// <summary>
    ///     Initializes the empty preparation of a newly registered manifest participant.
    /// </summary>
    /// <param name="agencyId">The agency identifier.</param>
    /// <param name="expeditionGroupId">The expedition group identifier.</param>
    /// <param name="memberId">The manifest entry identifier.</param>
    public ParticipantPreparation(int agencyId, int expeditionGroupId, int memberId)
    {
        AgencyId = agencyId;
        ExpeditionGroupId = expeditionGroupId;
        MemberId = memberId;
        BaselineMin = DefaultBaselineMin;
        BaselineMax = DefaultBaselineMax;
    }

    /// <summary>Gets the emergency contact as a value object, if registered.</summary>
    public EmergencyContact? EmergencyContact =>
        EmergencyContactName is null || EmergencyContactPhone is null
            ? null
            : new EmergencyContact(EmergencyContactName, EmergencyContactPhone, EmergencyContactRelationship);

    /// <summary>Gets a value indicating whether the participant is ready to start the tour.</summary>
    public bool IsReady => WearableId is not null && Consent && Activated && EmergencyContact is not null;

    /// <summary>
    ///     Assigns a wearable device. Changing the device deactivates the previous activation.
    /// </summary>
    /// <param name="wearableId">The wearable device identifier.</param>
    public void AssignWearable(int wearableId)
    {
        if (WearableId != wearableId) Activated = false;
        WearableId = wearableId;
        ReturnedAt = null;
        ReturnCondition = null;
    }

    /// <summary>
    ///     Registers the emergency contact of the participant.
    /// </summary>
    /// <param name="contact">The validated emergency contact.</param>
    public void RegisterEmergencyContact(EmergencyContact contact)
    {
        EmergencyContactName = contact.Name;
        EmergencyContactPhone = contact.Phone;
        EmergencyContactRelationship = contact.Relationship;
    }

    /// <summary>
    ///     Grants monitoring consent and activates the wearable.
    /// </summary>
    /// <param name="recordedAt">The moment the consent is granted.</param>
    /// <exception cref="InvalidOperationException">Thrown when the wearable or emergency contact is missing.</exception>
    public void GrantConsent(DateTimeOffset recordedAt)
    {
        if (WearableId is null) throw new InvalidOperationException("A wearable device must be assigned first.");
        if (EmergencyContact is null) throw new InvalidOperationException("An emergency contact must be registered first.");
        Consent = true;
        Activated = true;
        ConsentRecordedAt = recordedAt;
    }

    /// <summary>
    ///     Revokes monitoring consent and deactivates the wearable.
    /// </summary>
    /// <param name="recordedAt">The moment the consent is revoked.</param>
    public void RevokeConsent(DateTimeOffset recordedAt)
    {
        Consent = false;
        Activated = false;
        ConsentRecordedAt = recordedAt;
    }

    /// <summary>
    ///     Records the return of the wearable at the end of the tour.
    /// </summary>
    /// <param name="condition">The condition of the returned device.</param>
    /// <param name="returnedAt">The moment the device was returned.</param>
    public void RegisterReturn(WearableDeviceStatus condition, DateTimeOffset returnedAt)
    {
        ReturnCondition = condition;
        ReturnedAt = returnedAt;
        Activated = false;
    }
}
