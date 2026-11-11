namespace Domain.Entities;

/*  Summart
 *  If a task is an assignment, managers can assign it to an employee (or employees) and give them an
 *  expected duration and deadline (for each employee assigned) to have a better overview for allocation
 */
public sealed class TaskAssignment
{
    public Guid TaskId { get; set; }
    
    //which employee is it assigned to
    public Guid AssignedTo { get; set; }
    public double ExpectedDuration { get; set; }
    public DateTime ExpectedDeadline { get; set; }
}
