using Domain.ValueObjects;

namespace Domain.Entities;

/*  Summary
 *  Defines which type of access employees has (aka role), SystemPermission could be administrator, payroll etc.
 *  If more permissions has come to mind feel free to add them in ValueObjects.SystemPermission.
 *  this is designed this way so its able to open for future development for RBAC.
 */

public sealed class EmployeeSystemAccess
{
    public Guid EmployeeId { get; set; }
    public SystemPermission Permission { get; set; }
}
