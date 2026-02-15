using Microsoft.EntityFrameworkCore;
using EventRegistration.Core.Entities;

namespace EventRegistration.Infrastructure.Queries
{
    /// <summary>
    /// Pre-compiled EF Core queries for better performance
    /// </summary>
    public static class CompiledQueries
    {
        public static readonly Func<ApplicationDbContext, int, Task<Event?>> GetEventById =
            EF.CompileAsyncQuery((ApplicationDbContext context, int id) =>
                context.Events
                    .AsNoTracking()
                    .FirstOrDefault(e => e.Id == id));

        public static readonly Func<ApplicationDbContext, int, Task<int>> GetRegistrationCount =
            EF.CompileAsyncQuery((ApplicationDbContext context, int eventId) =>
                context.Registrations
                    .Count(r => r.EventId == eventId));

        public static readonly Func<ApplicationDbContext, string, Task<int>> GetEventCountByCreator =
            EF.CompileAsyncQuery((ApplicationDbContext context, string createdBy) =>
                context.Events
                    .Count(e => e.CreatedBy == createdBy));
        
        public static readonly Func<ApplicationDbContext, string, int, Task<bool>> RegistrationExists =
            EF.CompileAsyncQuery((ApplicationDbContext context, string email, int eventId) =>
                context.Registrations
                    .Any(r => r.EmailAddress == email && r.EventId == eventId));
    }
}
