using Azure.Core;
using EventRegistration.Core.DTOs;
using EventRegistration.Core.Entities;
using EventRegistration.Core.Interfaces;
using EventRegistration.Core.Mappings;
using EventRegistration.Core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EventRegistration.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly IUnitOfWork _UnitOfWork;
    private readonly ILogger<EventsController> _logger;
    private readonly EventCache _eventCache;
    private readonly EventLimitsConfiguration _eventLimits;

    public EventsController(IUnitOfWork UnitofWork, ILogger<EventsController> logger, EventCache eventCache, IOptions<EventLimitsConfiguration> eventLimits)
    {
        _UnitOfWork = UnitofWork;
        _logger = logger;
        _eventCache = eventCache;
        _eventLimits = eventLimits.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EventResponse>>> GetEvents(
        [FromQuery] EventSearchParameters searchParameters, CancellationToken cancellationToken
        )
    {
        try
        {
            //if user is authenticated and not an admin then can view only events created by him
            if(User.Identity?.IsAuthenticated == true && User.IsInRole("EventCreator"))
            {
                var userId = GetUserId();
                if (!string.IsNullOrEmpty(userId)) {
                    searchParameters = searchParameters with { CreatedBy = userId };
                }
            }

            var pagedEvents = await _UnitOfWork.EventRepository.GetEventsAsync(searchParameters, cancellationToken);

            var response = new PagedResponse<EventSummaryResponse>
            {
                Items = pagedEvents.Items.Select(i => EntityMapper.MapToEventSummary(i)).ToList(),
                TotalCount = pagedEvents.TotalCount,
                PageNumber = pagedEvents.PageNumber,
                PageSize = pagedEvents.PageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            return StatusCode(500, "An error occurred while retrieving events");
        }
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> GetEventById(int id, CancellationToken cancellationToken) {

        var eventEntity = await _eventCache.GetOrCreateNullableAsync(
            id,
            async () => await _UnitOfWork.EventRepository.GetEventByIdAsync(id, false, cancellationToken),
            cancellationToken);

        if (eventEntity == null)
            return NotFound(new { message = $"Event with ID {id} not found" });

        if (User.Identity?.IsAuthenticated == true && !User.IsInRole("Administrator"))
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId) && eventEntity.CreatedBy == userId) {
                _logger.LogWarning(
                   "User {UserId} attempted to access event {EventId} owned by {OwnerId}",
                   userId,
                   id,
                   eventEntity.CreatedBy);

                return Forbid();
            }
        }

        var registrationsCount = await _UnitOfWork.EventRepository.GetRegistrationCountAsync(id, cancellationToken);

        return Ok(EntityMapper.MapToEventResponse(eventEntity, registrationsCount));
    }

    [HttpPost]
    [Authorize(Policy = "EventCreator")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventResponse>> CreateEvent([FromBody] CreateEventRequest eventRequest, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Custom validation
        if (!eventRequest.IsValid(out var errorMessage))
            return BadRequest(new { message = errorMessage });
        try
        {
            var userId = GetUserId();
            var userName = GetUserName();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token" });

            var (locationValid, locationError) = EventValidationUtility.IsValidLocation(eventRequest.Location);
            if (!locationValid)
                return BadRequest(new { message = locationError });

            // Check user event creation limit (unless admin)
            if (!User.IsInRole("Administrator"))
            {
                var userEventCount = await _UnitOfWork.EventRepository.GetEventsCountByCreatorAsync(
                    userId,
                    cancellationToken);

                var (limitValid, limitError) = EventValidationUtility.ValidateEventCreationLimit(
                    userEventCount,
                    _eventLimits.MaxEventsPerCreator);

                if (!limitValid)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to create event but has reached limit of {Limit}",
                        userId, _eventLimits.MaxEventsPerCreator);
                    return BadRequest(new { message = limitError });
                }
            }

            var eventEntity = new Event
            {
                Name = eventRequest.Name,
                Description = eventRequest.Description,
                Location = eventRequest.Location,
                StartTime = eventRequest.StartTime.ToUniversalTime(),
                EndTime = eventRequest.EndTime.ToUniversalTime(),
                CreatedBy = userId,
                CreatedByName = userName
            };

            var createdEvent = await _UnitOfWork.EventRepository.CreateEventAsync(eventEntity, cancellationToken);

            // Add to cache
            _eventCache.SetEvent(createdEvent.Id, createdEvent);

            return CreatedAtAction(
                nameof(CreateEvent),
                new { id = createdEvent.Id },
                EntityMapper.MapToEventResponse(createdEvent)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event");
            return StatusCode(500, "An error occurred while creating the event");
        }
    }

    /// <summary>
    /// Update event (requires ownership or administrator role)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "EventCreator")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(int id, [FromBody]UpdateEventRequest updateRequest, CancellationToken cancellationToken) {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token" });

            // Check if user is creator or administrator
            var isCreator = await _UnitOfWork.EventRepository.IsCreatorAsync(id, userId, cancellationToken);
            var isAdmin = User.IsInRole("Administrator");

            if (!isCreator && !isAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to update event {EventId} without ownership",
                    userId,
                    id);

                return Forbid();
            }

            if (!string.IsNullOrWhiteSpace(updateRequest.Location))
            {
                var (locationValid, locationError) = EventValidationUtility.IsValidLocation(updateRequest.Location);
                if (!locationValid)
                    return BadRequest(new { message = locationError });
            }

            var updatedEvent = await _UnitOfWork.EventRepository.UpdateEventAsync(
                id,
                evt =>
                {
                    if (updateRequest.Name != null) evt.Name = updateRequest.Name;
                    if (updateRequest.Description != null) evt.Description = updateRequest.Description;
                    if (updateRequest.Location != null) evt.Location = updateRequest.Location;
                    if (updateRequest.StartTime.HasValue) evt.StartTime = updateRequest.StartTime.Value.ToUniversalTime();
                    if (updateRequest.EndTime.HasValue) evt.EndTime = updateRequest.EndTime.Value.ToUniversalTime();
                },
                cancellationToken);

            if (updatedEvent == null)
                return NotFound(new { message = $"Event with ID {id} not found" });

            // Invalidate cache - will reload on next GET
            _eventCache.RemoveEvent(id);

            return Ok(EntityMapper.MapToEventResponse(updatedEvent));
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", id);
            return StatusCode(500, "An error occurred while updating the event");
        }
    }

    /// <summary>
    /// Delete event (soft delete - requires ownership or administrator role)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "EventCreator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token" });

            // Check if user is creator or administrator
            var isCreator = await _UnitOfWork.EventRepository.IsCreatorAsync(id, userId, cancellationToken);
            var isAdmin = User.IsInRole("Administrator");

            if (!isCreator && !isAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to delete event {EventId} without ownership",
                    userId,
                    id);

                return Forbid();
            }

            var deleted = await _UnitOfWork.EventRepository.DeleteEventAsync(id, cancellationToken);

            if (!deleted)
                return NotFound(new { message = $"Event with ID {id} not found" });

            // Remove from cache
            _eventCache.RemoveEvent(id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", id);
            return StatusCode(500, "An error occurred while deleting the event");
        }
    }

    /// <summary>
    /// Get registrations for an event (requires ownership or administrator role)
    /// </summary>
    [HttpGet("{id:int}/registrations")]
    [Authorize(Policy = "EventCreator")]
    [ProducesResponseType(typeof(PagedResponse<RegistrationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetEventRegistrations(
        int id,
        [FromQuery] RegistrationSearchParameters parameters,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var userId = GetUserId();

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token" });

            // Check if user is creator or administrator
            var isCreator = await _UnitOfWork.EventRepository.IsCreatorAsync(id, userId, cancellationToken);
            var isAdmin = User.IsInRole("Administrator");

            if (!isCreator && !isAdmin)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to view registrations for event {EventId} without ownership",
                    userId,
                    id);

                return Forbid();
            }

            // Ensure EventId in parameters matches route parameter
            parameters = parameters with { EventId = id };

            var pagedRegistrations = await _UnitOfWork.RegistrationRepository
                .GetRegistrationsByEventIdAsync(parameters, cancellationToken);

            var response = new PagedResponse<RegistrationResponse>
            {
                Items = pagedRegistrations.Items.Select(EntityMapper.MapToRegistrationResponse).ToList(),
                TotalCount = pagedRegistrations.TotalCount,
                PageNumber = pagedRegistrations.PageNumber,
                PageSize = pagedRegistrations.PageSize
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving registrations for event {EventId}", id);
            return StatusCode(500, "An error occurred while retrieving registrations");
        }
    }

    /// <summary>
    /// Export all registrations for an event as CSV
    /// Uses streaming for memory efficiency
    /// </summary>
    [HttpGet("{id:int}/registrations/export")]
    [Authorize(Policy = "EventCreator")]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task ExportRegistrations(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            // Verify event exists and user has access
            var eventEntity = await _UnitOfWork.EventRepository.GetEventByIdAsync(id, false, cancellationToken);

            if (eventEntity == null)
            {
                Response.StatusCode = 404;
                await Response.WriteAsJsonAsync(new { message = $"Event with ID {id} not found" }, cancellationToken);
                return;
            }

            // Authorization check
            if (!User.IsInRole("Administrator"))
            {
                var userId = GetUserId();
                if (string.IsNullOrEmpty(userId) || eventEntity.CreatedBy != userId)
                {
                    _logger.LogWarning(
                        "User {UserId} attempted to export registrations for event {EventId} owned by {OwnerId}",
                        userId, id, eventEntity.CreatedBy);

                    Response.StatusCode = 403;
                    await Response.WriteAsJsonAsync(new { message = "Access denied" }, cancellationToken);
                    return;
                }
            }

            // Set response headers for CSV download
            Response.ContentType = "text/csv";
            Response.Headers["Content-Disposition"] =
                $"attachment; filename=\"event-{id}-registrations-{DateTime.UtcNow:yyyyMMdd}.csv\"";

            // Write CSV header
            await using var writer = new StreamWriter(Response.Body, leaveOpen: true);
            await writer.WriteLineAsync("Name,Email,Phone,Registered At");

            // Stream registrations and write to CSV
            var count = 0;
            await foreach (var registration in _UnitOfWork.RegistrationRepository.StreamRegistrationsAsync(
                id,
                cancellationToken))
            {
                // Escape CSV values (handle commas, quotes)
                var name = EscapeCsvValue(registration.Name);
                var email = EscapeCsvValue(registration.EmailAddress);
                var phone = EscapeCsvValue(registration.PhoneNumber ?? "");
                var registeredAt = registration.RegisteredAt.ToString("yyyy-MM-dd HH:mm:ss");

                await writer.WriteLineAsync($"{name},{email},{phone},{registeredAt}");
                count++;
            }

            await writer.FlushAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting registrations for event {EventId}", id);

            if (!Response.HasStarted)
            {
                Response.StatusCode = 500;
                await Response.WriteAsJsonAsync(new { message = "An error occurred during export" }, cancellationToken);
            }
        }
    }

    
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // If contains comma, quote, or newline - wrap in quotes and escape quotes
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("oid");
    }

    private string? GetUserName()
    {
        return User.FindFirstValue(ClaimTypes.Name)
            ?? User.FindFirstValue("name");
    }
}
