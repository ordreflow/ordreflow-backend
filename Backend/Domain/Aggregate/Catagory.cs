using Core.Tools.OperationResult;
using Domain.Entities;
using Domain.ValueObjects;
using Task = Domain.Entities.Task;
using TaskStatus = Domain.ValueObjects.TaskStatus;

namespace Domain.Aggregate;

/*  Summary
 *  Catagory (previous order) is the different catagories the tasks can be put in,
 *  so the departments can group their tasks.
 *  Catagories could for an example be "Customer orders" or "Internal Meetings", which can include team weekly meetings, 1-on-1's etc. for plumbers department
 */
public sealed class Catagory
{
    private readonly List<Task> _tasks = new();
    
    public CatagoryId Id { get; set; } = null!;
    public EmployeeId CreatedBy { get; set; } = null!;
    public DepartmentId DepartmentId { get; set; } = null!;
    public CatagoryName Name { get; set; } = null!;
    public string? Description { get; set; }
    public CatagoryStatus Status { get; set; }
    
    // set to true, if its a catagory that only the departments users should have access to enter time registrations
    public bool InternalOnly { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public IReadOnlyCollection<Task> Tasks => _tasks.AsReadOnly();

    public Result CanRegisterTime(
        Task task, Employee employee, bool isDepartmentMember, bool hasAssignments, bool isAssigned)
    {
        if (employee is null || !employee.HasPermission(SystemPermission.Employee))
            return Result.Failure(new Error("EmployeeAccessForbidden", "Active employee access is required."));

        if (task is null || task.CatagoryId != Id)
            return Result.Failure(new Error("TaskCategoryMismatch", "The task does not belong to this category."));

        if (Status != CatagoryStatus.Active)
            return Result.Failure(new Error("CatagoryClosed", "Time cannot be registered in a closed category."));

        if (task.Status != TaskStatus.Active)
            return Result.Failure(new Error("TaskClosed", "Time cannot be registered on a closed task."));

        if (InternalOnly && !isDepartmentMember)
            return Result.Failure(new Error("CatagoryAccessForbidden", "This category is restricted to its department."));

        if (hasAssignments && !isAssigned)
            return Result.Failure(new Error("TaskAssignmentForbidden", "This task is assigned to other employees."));

        return Result.Success();
    }
}

// Create, Close, Edit, SetInternalOnly
