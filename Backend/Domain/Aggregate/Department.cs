namespace Domain.Aggregate;

public sealed class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; }
}

//Create, Edit, Delete, EditManagers, EditEmployees