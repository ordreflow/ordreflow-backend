namespace Domain.Entities;

/*
 * Before only Managers that owned the catagories (order) and tasks could review timeentries.
 * Now a manager in a department can assign timeentry reviewers to certain catagories that the department owns,
 * This could be the manager itself or an employee in the department (could be for example a//TimeEntries from daten manager-trainee,
 * or senior/lead employee.)
 */
public sealed class CatagoryReviewer
{
    public Guid EmployeeId { get; set; }
    public Guid CatagoryId { get; set; }
}
