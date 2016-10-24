using Ocelot.Errors;

namespace Ocelot.Infrastructure.Provider
{
    public class NoDataSetError : Error
    {
        public NoDataSetError() 
            : base("the data was null", OcelotErrorCode.NoDataSetError)
        {
        }
    }
}
