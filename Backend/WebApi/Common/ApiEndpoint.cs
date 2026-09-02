using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace WebAPI.Common;

[ApiController, Route("api")]
public abstract class EndpointBase : ControllerBase;

public static class ApiEndpoint
{
    public static class WithRequest<TRequest>
    {
        public abstract class AndResponse<T1> : EndpointBase
        where T1 : IResult
        {
            public abstract Task<T1> HandleAsync(TRequest request);
        }

        public abstract class AndResponse<T1, T2> : EndpointBase
            where T1 : IResult
            where T2 : IResult
        {
            public abstract Task<Results<T1, T2>> HandleAsync(TRequest request);
        }

        public abstract class AndResponse<T1, T2, T3> : EndpointBase
            where T1 : IResult
            where T2 : IResult
            where T3 : IResult
        {
            public abstract Task<Results<T1, T2, T3>> HandleAsync(TRequest request);
        }
    }

    public static class WithoutRequest
    {
        public abstract class AndResponse<TResult> : EndpointBase
        where TResult : IResult
        {
            public abstract Task<TResult> HandleAsync();
        }

        public abstract class AndResponse<T1, T2> : EndpointBase
            where T1 : IResult
            where T2 : IResult
        {
            public abstract Task<Results<T1, T2>> HandleAsync();
        }

        public abstract class AndResponse<T1, T2, T3> : EndpointBase
            where T1 : IResult
            where T2 : IResult
            where T3 : IResult
        {
            public abstract Task<Results<T1, T2, T3>> HandleAsync();
        }
    }
}