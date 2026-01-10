using CallistraAgent.Functions.Models;
using Microsoft.EntityFrameworkCore;

namespace CallistraAgent.Functions.Data.Repositories;

/// <summary>
/// Repository implementation for CallSession entity operations
/// </summary>
public class CallSessionRepository : ICallSessionRepository
{
    private readonly CallistraAgentDbContext _context;

    public CallSessionRepository(CallistraAgentDbContext context)
    {
        _context = context;
    }

    public async Task<CallSession?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.CallSessions
            .Include(cs => cs.Member)
            .Include(cs => cs.Responses)
            .FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);
    }

    public async Task<CallSession?> GetByCallConnectionIdAsync(string callConnectionId, CancellationToken cancellationToken = default)
    {
        return await _context.CallSessions
            .Include(cs => cs.Member)
            .Include(cs => cs.Responses)
            .FirstOrDefaultAsync(cs => cs.CallConnectionId == callConnectionId, cancellationToken);
    }

    public async Task<List<CallSession>> GetByMemberIdAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await _context.CallSessions
            .Include(cs => cs.Responses)
            .Where(cs => cs.MemberId == memberId)
            .OrderByDescending(cs => cs.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<CallSession?> GetActiveCallForMemberAsync(int memberId, CancellationToken cancellationToken = default)
    {
        return await _context.CallSessions
            .Where(cs => cs.MemberId == memberId)
            .Where(cs => cs.Status == CallStatus.Initiated || cs.Status == CallStatus.Ringing || cs.Status == CallStatus.Connected)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CallSession> CreateAsync(CallSession callSession, CancellationToken cancellationToken = default)
    {
        _context.CallSessions.Add(callSession);
        await _context.SaveChangesAsync(cancellationToken);
        return callSession;
    }

    public async Task UpdateAsync(CallSession callSession, CancellationToken cancellationToken = default)
    {
        callSession.UpdatedAt = DateTime.UtcNow;
        _context.CallSessions.Update(callSession);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
