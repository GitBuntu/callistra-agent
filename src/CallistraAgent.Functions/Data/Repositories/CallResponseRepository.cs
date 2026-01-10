using CallistraAgent.Functions.Models;
using Microsoft.EntityFrameworkCore;

namespace CallistraAgent.Functions.Data.Repositories;

/// <summary>
/// Repository implementation for CallResponse entity operations
/// </summary>
public class CallResponseRepository : ICallResponseRepository
{
    private readonly CallistraAgentDbContext _context;

    public CallResponseRepository(CallistraAgentDbContext context)
    {
        _context = context;
    }

    public async Task<List<CallResponse>> GetByCallSessionIdAsync(int callSessionId, CancellationToken cancellationToken = default)
    {
        return await _context.CallResponses
            .Where(cr => cr.CallSessionId == callSessionId)
            .OrderBy(cr => cr.QuestionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<CallResponse> CreateAsync(CallResponse callResponse, CancellationToken cancellationToken = default)
    {
        _context.CallResponses.Add(callResponse);
        await _context.SaveChangesAsync(cancellationToken);
        return callResponse;
    }

    public async Task<CallResponse?> GetByQuestionAsync(int callSessionId, int questionNumber, CancellationToken cancellationToken = default)
    {
        return await _context.CallResponses
            .FirstOrDefaultAsync(cr => cr.CallSessionId == callSessionId && cr.QuestionNumber == questionNumber, cancellationToken);
    }
}
