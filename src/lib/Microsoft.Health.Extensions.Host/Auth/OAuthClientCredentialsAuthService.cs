// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityModel.Client;

namespace Microsoft.Health.Extensions.Host.Auth
{
    public class OAuthClientCredentialsAuthService : IAuthService
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        public async Task<string> GetAccessTokenAsync()
        {
            var resource = Environment.GetEnvironmentVariable("FhirService:Resource");
            var authority = Environment.GetEnvironmentVariable("FhirService:Authority");
            var clientId = Environment.GetEnvironmentVariable("FhirService:ClientId");
            var clientSecret = Environment.GetEnvironmentVariable("FhirService:ClientSecret");

            using (var request = new ClientCredentialsTokenRequest
                {
                    Address = authority,
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = resource,
                })
            {
                var tokenResponse = await _httpClient.RequestClientCredentialsTokenAsync(request).ConfigureAwait(false);
                if (tokenResponse.IsError)
                {
                    throw new Exception($"Failed to retrieve token: {tokenResponse.Error} - {tokenResponse.ErrorDescription}.");
                }

                return tokenResponse.AccessToken;
            }
        }
    }
}
