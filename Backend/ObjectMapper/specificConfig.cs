namespace ObjectMapper;

public class MyInput
{
    public string Name { get; set; } = string.Empty;
}

public class MyOutput
{
    public string Name { get; set; } = string.Empty;
}

public class specificConfig : IMappingConfig<MyInput, MyOutput>
{
    public MyOutput Map(MyInput input)
    {
        return new MyOutput
        {
            Name = input.Name + " (Mapped)"
        };
    }
}