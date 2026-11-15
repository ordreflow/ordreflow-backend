using Domain.ValueObjects;

namespace Domain.Entities;

/*  Summary
 *  Defines which type of access employees has (aka role), SystemPermission could be administrator, payroll etc.
 *  If more permissions has come to mind feel free to add them in ValueObjects.SystemPermission.
 *  this is designed this way so its able to open for future development for RBAC.
 */

public sealed class EmployeeSystemAccess
{
    public EmployeeId EmployeeId { get; private set; } = null!;
    public SystemPermission Permission { get; private set; }

    private EmployeeSystemAccess() { }
    internal static EmployeeSystemAccess Create(EmployeeId employeeId, SystemPermission permission) =>
        new() { EmployeeId = employeeId, Permission = permission };
}
