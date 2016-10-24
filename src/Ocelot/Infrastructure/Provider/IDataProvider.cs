using System.Collections.Generic;
using Ocelot.Errors;
using Ocelot.Responses;

namespace Ocelot.Infrastructure.Provider
{
    public interface IDataProvider<T>
    {
        Response Set(T value);
        Response<T> Get();
    }

    public class DataProvider<T> : IDataProvider<T>
    {
        private T _data;

        public Response Set(T value)
        {
            _data = value;
            return new OkResponse();
        }

        public Response<T> Get()
        {
            if (_data != null)
            {
                return new OkResponse<T>(_data);
            }

            return new ErrorResponse<T>(new List<Error>
            {
                new NoDataSetError()
            });
        }
    }
}
