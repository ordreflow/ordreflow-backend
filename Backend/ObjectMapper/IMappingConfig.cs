namespace ObjectMapper;

public interface IMappingConfig<TIn, TOut>
{
    TOut Map(TIn input);
}