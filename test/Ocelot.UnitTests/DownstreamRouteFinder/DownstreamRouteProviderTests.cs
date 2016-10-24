using System.Collections.Generic;
using Ocelot.Configuration.Builder;
using Ocelot.DownstreamRouteFinder;
using Ocelot.DownstreamRouteFinder.UrlMatcher;
using Ocelot.Errors;
using Ocelot.Infrastructure.Provider;
using Ocelot.Responses;
using Shouldly;
using TestStack.BDDfy;
using Xunit;

namespace Ocelot.UnitTests.DownstreamRouteFinder
{
    public class DownstreamRouteProviderTests
    {
        private readonly IDataProvider<DownstreamRoute> _provider;
        private Response<DownstreamRoute> _result;

        public DownstreamRouteProviderTests()
        {
            _provider = new DataProvider<DownstreamRoute>();
        }

        [Fact]
        public void should_set_route()
        {
            var reRoute = new ReRouteBuilder()
                .Build();

            var downstreamRoute = new DownstreamRoute(new List<TemplateVariableNameAndValue>(), reRoute);

            this.Given(x => x.GivenTheRouteIsSet(downstreamRoute))
                .When(x => x.WhenICallTheProdvider())
                .Then(x => x.ThenTheRouteIsReturned(new OkResponse<DownstreamRoute>(downstreamRoute)))
                .BDDfy();
        }

        [Fact]
        public void should_get_route()
        {
            var reRoute = new ReRouteBuilder()
                .WithUpstreamTemplate("blahhhh")
                .Build();

            var downstreamRoute = new DownstreamRoute(new List<TemplateVariableNameAndValue>(), reRoute);

            this.Given(x => x.GivenTheRouteIsSet(downstreamRoute))
                .When(x => x.WhenICallTheProdvider())
                .Then(x => x.ThenTheRouteIsReturned(new OkResponse<DownstreamRoute>(downstreamRoute)))
                .BDDfy();
        }

        [Fact]
        public void should_return_error()
        {
            this.Given(x => x.GivenTheRouteIsNotSet())
               .When(x => x.WhenICallTheProdvider())
               .Then(x => x.ThenTheErrorReturned(new ErrorResponse<DownstreamRoute>(new List<Error>
               {
                    new NoDataSetError()
               })))
               .BDDfy();
        }

        private void GivenTheRouteIsNotSet()
        {
            //nothing doing...
        }

        private void GivenTheRouteIsSet(DownstreamRoute downstreamRoute)
        {
            _provider.Set(downstreamRoute);
        }

        private void WhenICallTheProdvider()
        {
            _result = _provider.Get();
        }

        private void ThenTheErrorReturned(Response<DownstreamRoute> expected)
        {
            _result.Errors.Count.ShouldBe(expected.Errors.Count);
            _result.IsError.ShouldBe(expected.IsError);
        }

        private void ThenTheRouteIsReturned(Response<DownstreamRoute> expected)
        {
            _result.Data.ReRoute.UpstreamTemplate.ShouldBe(expected.Data.ReRoute.UpstreamTemplate);
        }
    }
}
