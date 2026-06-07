using GLMS.Api.Data;
using GLMS.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace GLMS.Api.Services
{
    /// <summary>
    /// WHAT THIS FILE IS:
    /// The service implementation for Client operations.
    /// Contains ALL data access logic for Clients — the controller just calls these methods.
    ///
    /// WHAT I DID (Part 3 — Architectural Integrity):
    /// I moved all ApplicationDbContext queries out of ClientsController into this class.
    /// The controller is now a thin HTTP layer — it receives the request, calls the service,
    /// and returns the appropriate HTTP response. It never touches the database directly.
    ///
    /// PRESENTATION POINT — Separation of Concerns:
    ///   ClientsController: "What HTTP response do I return?"
    ///   ClientService:     "What data do I fetch/save?"
    ///
    /// DEPENDENCY INJECTION:
    /// ClientService is registered as Scoped in Program.cs (one per HTTP request).
    /// The same ApplicationDbContext instance is shared within the same request.
    /// </summary>
    public class ClientService : IClientService
    {
        private readonly ApplicationDbContext _context;

        public ClientService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>Returns all clients ordered by name.</summary>
        public async Task<List<Client>> GetAllAsync()
        {
            return await _context.Clients
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        /// <summary>Returns a single client by ID, or null if not found.</summary>
        public async Task<Client?> GetByIdAsync(int id)
        {
            return await _context.Clients.FindAsync(id);
        }

        /// <summary>Creates a new client and saves it to the database.</summary>
        public async Task<Client> CreateAsync(Client client)
        {
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
            return client; // EF Core populates the auto-generated Id after SaveChanges
        }

        /// <summary>Updates an existing client. Returns false if the client does not exist.</summary>
        public async Task<bool> UpdateAsync(int id, Client client)
        {
            if (!await _context.Clients.AnyAsync(c => c.Id == id))
                return false;

            client.Id = id; // Ensure the ID matches the route
            _context.Entry(client).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Deletes a client. Returns false if the client does not exist.
        /// Returns an error message if the client has associated contracts (FK constraint).
        /// </summary>
        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            var client = await _context.Clients.FindAsync(id);
            if (client == null) return (false, null);

            // Check for associated contracts before attempting delete
            // This provides a friendly error instead of a database exception
            var hasContracts = await _context.Contracts.AnyAsync(c => c.ClientId == id);
            if (hasContracts)
                return (false, "Cannot delete a client who has associated contracts.");

            _context.Clients.Remove(client);
            await _context.SaveChangesAsync();
            return (true, null);
        }
    }
}
