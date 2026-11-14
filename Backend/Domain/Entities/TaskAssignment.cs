using Domain.ValueObjects;

namespace Domain.Entities;

/*  Summart
 *  If a task is an assignment, managers can assign it to an employee (or employees) and give them an
 *  expected duration and deadline (for each employee assigned) to have a better overview for allocation
 */
public sealed class TaskAssignment
{
    public TaskId TaskId { get; set; } = null!;
    
    //which employee is it assigned to
    public EmployeeId AssignedTo { get; set; } = null!;
    public PlannedDuration ExpectedDuration { get; set; } = null!;
    public TargetCompletionDate ExpectedDeadline { get; set; } = null!;
}
