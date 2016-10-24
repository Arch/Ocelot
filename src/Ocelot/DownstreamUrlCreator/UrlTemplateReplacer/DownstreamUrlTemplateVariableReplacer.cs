using System.Text;
using Ocelot.DownstreamRouteFinder;
using Ocelot.Responses;

namespace Ocelot.DownstreamUrlCreator.UrlTemplateReplacer
{
    public class DownstreamUrlTemplateVariableReplacer : IDownstreamUrlTemplateVariableReplacer
    {
        public Response<DownstreamUrl> ReplaceTemplateVariables(DownstreamRoute downstreamRoute)
        {
            var downstreamUrl = new StringBuilder();

            downstreamUrl.Append(downstreamRoute.ReRoute.DownstreamTemplate);

            foreach (var templateVarAndValue in downstreamRoute.TemplateVariableNameAndValues)
            {
                downstreamUrl.Replace(templateVarAndValue.TemplateVariableName, templateVarAndValue.TemplateVariableValue);
            }

            return new OkResponse<DownstreamUrl>(new DownstreamUrl(downstreamUrl.ToString()));
        }
    }
}