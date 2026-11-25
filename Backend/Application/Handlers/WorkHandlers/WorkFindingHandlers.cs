using Application.Accounts;
using Application.Interfaces;
using Application.Queries;
using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Application.Handlers;

public sealed class ListAccessibleWorkHandler(AccountAccess access, IAccessibleWorkQuery work)
    : IQueryHandler<ListAccessibleWorkQuery, Result<IReadOnlyList<AccessibleWorkDto>>>
{
    public const int MaxSearchLength = 200;

    public async Task<Result<IReadOnlyList<AccessibleWorkDto>>> HandleAsync(ListAccessibleWorkQuery query)
    {
        var actor = await access.RequireEmployeeAsync(query.ActorId);
        if (actor.IsFailure) return Result<IReadOnlyList<AccessibleWorkDto>>.Failure(actor.Errors.ToArray());

        var search = query.Search?.Trim();
        if (search?.Length > MaxSearchLength)
            return Result<IReadOnlyList<AccessibleWorkDto>>.Failure(
                new Error("SearchTooLong", $"The search text cannot exceed {MaxSearchLength} characters."));

        var items = await work.ListAsync(actor.Value.Id, query.AssignedToMe);

        // Search only narrows the already authorised list, so it cannot reveal hidden work (FR-21).
        if (!string.IsNullOrEmpty(search))
            items = items.Where(item =>
                    item.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    item.CategoryName.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                .ToArray();

        return Result<IReadOnlyList<AccessibleWorkDto>>.Success(items
            .OrderByDescending(item => item.AssignedToMe)
            .ThenBy(item => item.CategoryName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray());
    }
}

public sealed class GetAccessibleWorkHandler(AccountAccess access, IAccessibleWorkQuery work)
    : IQueryHandler<GetAccessibleWorkQuery, Result<AccessibleWorkDto>>
{
    public async Task<Result<AccessibleWorkDto>> HandleAsync(GetAccessibleWorkQuery query)
    {
        var actor = await access.RequireEmployeeAsync(query.ActorId);
        if (actor.IsFailure) return Result<AccessibleWorkDto>.Failure(actor.Errors.ToArray());

        var id = TaskId.Create(query.WorkItemId);
        if (id.IsFailure) return Result<AccessibleWorkDto>.Failure(id.Errors.ToArray());

        // Work the employee may not see is reported exactly like work that does not exist.
        var item = await work.GetAsync(actor.Value.Id, id.Value);
        return item is null
            ? Result<AccessibleWorkDto>.Failure(new Error("WorkItemNotFound", "The work item could not be found."))
            : Result<AccessibleWorkDto>.Success(item);
    }
}
