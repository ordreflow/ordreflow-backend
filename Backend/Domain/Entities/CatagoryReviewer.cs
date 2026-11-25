using Core.Tools.OperationResult;
using Domain.ValueObjects;

namespace Domain.Entities;

/*
 * Before only Managers that owned the catagories (order) and tasks could review timeentries.
 * Now a manager in a department can assign timeentry reviewers to certain catagories that the department owns,
 * This could be the manager itself or an employee in the department (could be for example a//TimeEntries from daten manager-trainee,
 * or senior/lead employee.)
 */
public sealed class CatagoryReviewer
{
    public EmployeeId EmployeeId { get; set; } = null!;
    public CatagoryId CatagoryId { get; set; } = null!;

    public static Result<CatagoryReviewer> Create(EmployeeId employeeId, CatagoryId catagoryId) =>
        employeeId is null || catagoryId is null
            ? Result<CatagoryReviewer>.Failure(new Error("ReviewerRequired", "A reviewer and a category are required."))
            : Result<CatagoryReviewer>.Success(new CatagoryReviewer { EmployeeId = employeeId, CatagoryId = catagoryId });
}
