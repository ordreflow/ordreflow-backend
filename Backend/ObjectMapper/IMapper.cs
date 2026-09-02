namespace ObjectMapper;

public interface IMapper
{
    T Map<T>(object input);
}