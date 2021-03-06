using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Parser;
using Ocelot.Configuration.Validator;
using Ocelot.Responses;
using Ocelot.Utilities;

namespace Ocelot.Configuration.Creator
{
    /// <summary>
    /// Register as singleton
    /// </summary>
    public class FileOcelotConfigurationCreator : IOcelotConfigurationCreator
    {
        private readonly IOptions<FileConfiguration> _options;
        private readonly IConfigurationValidator _configurationValidator;
        private const string RegExMatchEverything = ".*";
        private const string RegExMatchEndString = "$";
        private const string RegExIgnoreCase = "(?i)";

        private readonly IClaimToThingConfigurationParser _claimToThingConfigurationParser;
        private readonly ILogger<FileOcelotConfigurationCreator> _logger;

        public FileOcelotConfigurationCreator(
            IOptions<FileConfiguration> options, 
            IConfigurationValidator configurationValidator, 
            IClaimToThingConfigurationParser claimToThingConfigurationParser, 
            ILogger<FileOcelotConfigurationCreator> logger)
        {
            _options = options;
            _configurationValidator = configurationValidator;
            _claimToThingConfigurationParser = claimToThingConfigurationParser;
            _logger = logger;
        }

        public Response<IOcelotConfiguration> Create()
        {     
            var config = SetUpConfiguration();

            return new OkResponse<IOcelotConfiguration>(config);
        }

        /// <summary>
        /// This method is meant to be tempoary to convert a config to an ocelot config...probably wont keep this but we will see
        /// will need a refactor at some point as its crap
        /// </summary>
        private IOcelotConfiguration SetUpConfiguration()
        {
            var response = _configurationValidator.IsValid(_options.Value);

            if (response.Data.IsError)
            {
                var errorBuilder = new StringBuilder();

                foreach (var error in response.Errors)
                {
                    errorBuilder.AppendLine(error.Message);
                }

                throw new Exception($"Unable to start Ocelot..configuration, errors were {errorBuilder}");
            }

            var reRoutes = new List<ReRoute>();

            foreach (var reRoute in _options.Value.ReRoutes)
            {
                var ocelotReRoute = SetUpReRoute(reRoute, _options.Value.GlobalConfiguration);
                reRoutes.Add(ocelotReRoute);
            }
            
            return new OcelotConfiguration(reRoutes);
        }

        private ReRoute SetUpReRoute(FileReRoute reRoute, FileGlobalConfiguration globalConfiguration)
        {
            var globalRequestIdConfiguration = !string.IsNullOrEmpty(globalConfiguration?.RequestIdKey);

            var upstreamTemplate = BuildUpstreamTemplate(reRoute);

            var isAuthenticated = !string.IsNullOrEmpty(reRoute.AuthenticationOptions?.Provider);

            var isAuthorised = reRoute.RouteClaimsRequirement?.Count > 0;

            var isCached = reRoute.FileCacheOptions.TtlSeconds > 0;

            var requestIdKey = globalRequestIdConfiguration
                ? globalConfiguration.RequestIdKey
                : reRoute.RequestIdKey;

            if (isAuthenticated)
            {
                var authOptionsForRoute = new AuthenticationOptions(reRoute.AuthenticationOptions.Provider,
                    reRoute.AuthenticationOptions.ProviderRootUrl, reRoute.AuthenticationOptions.ScopeName,
                    reRoute.AuthenticationOptions.RequireHttps, reRoute.AuthenticationOptions.AdditionalScopes,
                    reRoute.AuthenticationOptions.ScopeSecret);

                var claimsToHeaders = GetAddThingsToRequest(reRoute.AddHeadersToRequest);
                var claimsToClaims = GetAddThingsToRequest(reRoute.AddClaimsToRequest);
                var claimsToQueries = GetAddThingsToRequest(reRoute.AddQueriesToRequest);

                return new ReRoute(reRoute.DownstreamTemplate, reRoute.UpstreamTemplate,
                    reRoute.UpstreamHttpMethod, upstreamTemplate, isAuthenticated,
                    authOptionsForRoute, claimsToHeaders, claimsToClaims,
                    reRoute.RouteClaimsRequirement, isAuthorised, claimsToQueries,
                    requestIdKey, isCached, new CacheOptions(reRoute.FileCacheOptions.TtlSeconds));
            }

            return new ReRoute(reRoute.DownstreamTemplate, reRoute.UpstreamTemplate, 
                reRoute.UpstreamHttpMethod, upstreamTemplate, isAuthenticated, 
                null, new List<ClaimToThing>(), new List<ClaimToThing>(), 
                reRoute.RouteClaimsRequirement, isAuthorised, new List<ClaimToThing>(),
                    requestIdKey, isCached, new CacheOptions(reRoute.FileCacheOptions.TtlSeconds));
        }

        private string BuildUpstreamTemplate(FileReRoute reRoute)
        {
            var upstreamTemplate = reRoute.UpstreamTemplate;

            upstreamTemplate = upstreamTemplate.SetLastCharacterAs('/');

            var placeholders = new List<string>();

            for (var i = 0; i < upstreamTemplate.Length; i++)
            {
                if (IsPlaceHolder(upstreamTemplate, i))
                {
                    var postitionOfPlaceHolderClosingBracket = upstreamTemplate.IndexOf('}', i);
                    var difference = postitionOfPlaceHolderClosingBracket - i + 1;
                    var variableName = upstreamTemplate.Substring(i, difference);
                    placeholders.Add(variableName);
                }
            }

            foreach (var placeholder in placeholders)
            {
                upstreamTemplate = upstreamTemplate.Replace(placeholder, RegExMatchEverything);
            }

            var route = reRoute.ReRouteIsCaseSensitive 
                ? $"{upstreamTemplate}{RegExMatchEndString}" 
                : $"{RegExIgnoreCase}{upstreamTemplate}{RegExMatchEndString}";

            return route;
        }

        private List<ClaimToThing> GetAddThingsToRequest(Dictionary<string,string> thingBeingAdded)
        {
            var claimsToTHings = new List<ClaimToThing>();

            foreach (var add in thingBeingAdded)
            {
                var claimToHeader = _claimToThingConfigurationParser.Extract(add.Key, add.Value);

                if (claimToHeader.IsError)
                {
                    _logger.LogCritical(new EventId(1, "Application Failed to start"),
                        $"Unable to extract configuration for key: {add.Key} and value: {add.Value} your configuration file is incorrect");

                    throw new Exception(claimToHeader.Errors[0].Message);
                }
                claimsToTHings.Add(claimToHeader.Data);
            }

            return claimsToTHings;
        }

        private bool IsPlaceHolder(string upstreamTemplate, int i)
        {
            return upstreamTemplate[i] == '{';
        }
    }
}