using EventRegistration.Core.DTOs;
using EventRegistration.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventRegistration.Core.Mappings;

public static class EntityMapper
{
    /// <summary>
    /// Maps Event entity to EventResponse DTO
    /// </summary>
    public static EventResponse MapToEventResponse(Event evt, int registrationCount = 0)
    {
        ArgumentNullException.ThrowIfNull(evt);

        return new EventResponse
        {
            Id = evt.Id,
            Name = evt.Name,
            Description = evt.Description,
            Location = evt.Location,
            StartTime = evt.StartTime,
            EndTime = evt.EndTime,
            CreatedBy = evt.CreatedBy,
            CreatedByName = evt.CreatedByName,
            CreatedAt = evt.CreatedAt,
            RegistrationCount = registrationCount
        };
    }

    /// <summary>
    /// Maps Event entity to EventSummaryResponse DTO (lightweight)
    /// </summary>
    public static EventSummaryResponse MapToEventSummary(Event evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        return new EventSummaryResponse
        {
            Id = evt.Id,
            Name = evt.Name,
            Location = evt.Location,
            StartTime = evt.StartTime,
            EndTime = evt.EndTime
        };
    }

    /// <summary>
    /// Maps Registration entity to RegistrationResponse DTO
    /// </summary>
    public static RegistrationResponse MapToRegistrationResponse(Registration reg)
    {
        ArgumentNullException.ThrowIfNull(reg);

        return new RegistrationResponse
        {
            Id = reg.Id,
            EventId = reg.EventId,
            Name = reg.Name,
            EmailAddress = reg.EmailAddress,
            PhoneNumber = reg.PhoneNumber,
            RegisteredAt = reg.RegisteredAt,
            RegistrationSource = reg.RegistrationSource
        };
    }

    /// <summary>
    /// Maps CreateEventRequest DTO to Event entity
    /// </summary>
    public static Event MapToEvent(CreateEventRequest request, string createdBy, string createdByName)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(createdBy);

        return new Event
        {
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Location = request.Location.Trim(),
            StartTime = request.StartTime.ToUniversalTime(),
            EndTime = request.EndTime.ToUniversalTime(),
            CreatedBy = createdBy,
            CreatedByName = createdByName ?? "Unknown"
        };
    }

    /// <summary>
    /// Maps CreateRegistrationRequest DTO to Registration entity
    /// </summary>
    public static Registration MapToRegistration(CreateRegistrationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new Registration
        {
            EventId = request.EventId,
            Name = request.Name.Trim(),
            EmailAddress = request.EmailAddress.Trim().ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber.Trim(),
            RegistrationSource = request.RegistrationSource?.Trim()
        };
    }
}
