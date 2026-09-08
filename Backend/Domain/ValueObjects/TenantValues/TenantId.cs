namespace Domain.ValueObjects;

public record TenantId
{
    public Guid Value { get; }

    private TenantId(Guid value)
    {
        Value = value;
    }

    public static TenantId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("TenantId cannot be empty.", nameof(value));
        }

        return new TenantId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}