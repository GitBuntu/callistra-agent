using CallistraAgent.Functions.Models;

namespace CallistraAgent.Functions.Data.Repositories;

/// <summary>
/// Repository interface for Member entity operations
/// </summary>
public interface IMemberRepository
{
    /// <summary>
    /// Gets a member by ID
    /// </summary>
    Task<Member?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a member by phone number
    /// </summary>
    Task<Member?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active members
    /// </summary>
    Task<List<Member>> GetActiveMembersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new member
    /// </summary>
    Task<Member> CreateAsync(Member member, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing member
    /// </summary>
    Task UpdateAsync(Member member, CancellationToken cancellationToken = default);
}
