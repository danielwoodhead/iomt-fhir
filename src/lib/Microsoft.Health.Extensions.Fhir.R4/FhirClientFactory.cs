// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using EnsureThat;
using Hl7.Fhir.Rest;
using Microsoft.Extensions.Options;
using Microsoft.Health.Common;
using Microsoft.Health.Extensions.Fhir.Config;
using Microsoft.Health.Extensions.Host.Auth;

namespace Microsoft.Health.Extensions.Fhir
{
    public class FhirClientFactory : IFactory<IFhirClient>
    {
        private readonly bool _useManagedIdentity = false;
        private readonly bool _useClientCredentials = false;

        private FhirClientFactory()
            : this(useManagedIdentity: false, useClientCredentials: false)
        {
        }

        private FhirClientFactory(bool useManagedIdentity, bool useClientCredentials)
        {
            _useManagedIdentity = useManagedIdentity;
            _useClientCredentials = useClientCredentials;
        }

        public FhirClientFactory(IOptions<FhirClientFactoryOptions> options)
            : this(
                EnsureArg.IsNotNull(options, nameof(options)).Value.UseManagedIdentity,
                EnsureArg.IsNotNull(options, nameof(options)).Value.UseClientCredentials)
        {
        }

        public static IFactory<IFhirClient> Instance { get; } = new FhirClientFactory();

        public IFhirClient Create()
        {
            return _useManagedIdentity
                ? CreateManagedIdentityClient()
                : _useClientCredentials
                    ? CreateClientCredentialsClient()
                    : CreateConfidentialApplicationClient();
        }

        private static IFhirClient CreateClient(IAuthService authService)
        {
            var url = System.Environment.GetEnvironmentVariable("FhirService:Url");
            EnsureArg.IsNotNullOrEmpty(url, nameof(url));

            EnsureArg.IsNotNull(authService, nameof(authService));

            var client = new FhirClient(url)
            {
                PreferredFormat = ResourceFormat.Json,
            };

            client.OnBeforeRequest += (sender, e) =>
            {
                var token = authService.GetAccessTokenAsync()
                    .GetAwaiter()
                    .GetResult();
                e.RawRequest.Headers.Add("Authorization", $"Bearer {token}");
            };

            return client;
        }

        private static IFhirClient CreateManagedIdentityClient()
        {
            return CreateClient(new ManagedIdentityAuthService());
        }

        private static IFhirClient CreateConfidentialApplicationClient()
        {
            return CreateClient(new OAuthConfidentialClientAuthService());
        }

        private static IFhirClient CreateClientCredentialsClient()
        {
            return CreateClient(new OAuthClientCredentialsAuthService());
        }
    }
}
