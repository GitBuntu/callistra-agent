using CallistraAgent.Functions.Models;
using Microsoft.EntityFrameworkCore;

namespace CallistraAgent.Functions.Data.Repositories;

/// <summary>
/// Repository implementation for Member entity operations
/// </summary>
public class MemberRepository : IMemberRepository
{
    private readonly CallistraAgentDbContext _context;

    public MemberRepository(CallistraAgentDbContext context)
    {
        _context = context;
    }

    public async Task<Member?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Members
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Member?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await _context.Members
            .FirstOrDefaultAsync(m => m.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<List<Member>> GetActiveMembersAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Members
            .Where(m => m.Status == "Active")
            .ToListAsync(cancellationToken);
    }

    public async Task<Member> CreateAsync(Member member, CancellationToken cancellationToken = default)
    {
        _context.Members.Add(member);
        await _context.SaveChangesAsync(cancellationToken);
        return member;
    }

    public async Task UpdateAsync(Member member, CancellationToken cancellationToken = default)
    {
        member.UpdatedAt = DateTime.UtcNow;
        _context.Members.Update(member);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
