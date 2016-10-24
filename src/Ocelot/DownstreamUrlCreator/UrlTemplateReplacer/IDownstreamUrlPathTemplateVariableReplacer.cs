using Ocelot.DownstreamRouteFinder;
using Ocelot.Responses;

namespace Ocelot.DownstreamUrlCreator.UrlTemplateReplacer
{
    public interface IDownstreamUrlTemplateVariableReplacer
    {
        Response<DownstreamUrl> ReplaceTemplateVariables(DownstreamRoute downstreamRoute);   
    }
}