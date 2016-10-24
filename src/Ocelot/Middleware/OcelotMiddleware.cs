using System.Collections.Generic;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;

namespace Ocelot.Middleware
{
    public abstract class OcelotMiddleware
    {
        private readonly IDataProvider<List<Error>> _provider;

        protected OcelotMiddleware(IDataProvider<List<Error>> provider)
        {
            _provider = provider;
        }

        public void SetPipelineError(List<Error> errors)
        {
            _provider.Set(errors);
        }

        public bool PipelineError()
        {
            var errors = _provider.Get();
            return errors.Data != null && errors.Data.Count > 0;
        }

        public List<Error> GetPipelineErrors()
        {
            var errors = _provider.Get();
            return errors.Data;
        }
    }
}
