﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using Mistral.SDK.Completions;
using Mistral.SDK.Embeddings;
using Mistral.SDK.Models;

namespace Mistral.SDK
{
    public class MistralClient: IDisposable
    {
        public string ApiUrlFormat { get; set; } = "https://api.mistral.ai/{0}/{1}";

        /// <summary>
        /// Version of the Rest Api
        /// </summary>
        public string ApiVersion { get; set; } = "v1";

        /// <summary>
        /// The API authentication information to use for API calls
        /// </summary>
        public APIAuthentication Auth { get; set; }

        /// <summary>
        /// Optionally provide a custom HttpClient to send requests.
        /// </summary>
        internal HttpClient HttpClient { get; set; }

        /// <summary>
        /// Creates a new entry point to the Mistral API, handling auth and allowing access to the various API endpoints
        /// </summary>
        /// <param name="apiKeys">
        /// The API authentication information to use for API calls,
        /// or <see langword="null"/> to attempt to use the <see cref="APIAuthentication.Default"/>,
        /// potentially loading from environment vars.
        /// </param>
        /// <param name="client">A <see cref="HttpClient"/>.</param>
        /// <remarks>
        /// <see cref="MistralClient"/> implements <see cref="IDisposable"/> to manage the lifecycle of the resources it uses, including <see cref="HttpClient"/>.
        /// When you initialize <see cref="MistralClient"/>, it will create an internal <see cref="HttpClient"/> instance if one is not provided.
        /// This internal HttpClient is disposed of when <see cref="MistralClient"/> is disposed of.
        /// If you provide an external HttpClient instance to <see cref="MistralClient"/>, you are responsible for managing its disposal.
        /// </remarks>
        public MistralClient(APIAuthentication apiKeys = null, HttpClient client = null)
        {
            HttpClient = SetupClient(client);
            this.Auth = apiKeys.ThisOrDefault();
            Completions = new CompletionsEndpoint(this);
            Models = new ModelsEndpoint(this);
            Embeddings = new EmbeddingsEndpoint(this);
        }

        internal static JsonSerializerOptions JsonSerializationOptions { get; } = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
        };

        private HttpClient SetupClient(HttpClient client)
        {
            if (client is not null)
            {
                isCustomClient = true;
                return client;
            }
#if NET6_0_OR_GREATER
            return new HttpClient(new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(15)
            });
#else
            return new HttpClient();
#endif
        }

        /// <summary>
        /// Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way you “program” the API to do a task is by simply describing the task in plain english or providing a few written examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration).
        /// </summary>
        public CompletionsEndpoint Completions { get; }

        /// <summary>
        /// Lists the core models available to the user via API.
        /// </summary>
        public ModelsEndpoint Models { get; }
        
        /// <summary>
        /// Gets model embeddings via API.
        /// </summary>
        public EmbeddingsEndpoint Embeddings { get; }


        #region IDisposable

        private bool isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!isDisposed && disposing)
            {
                if (!isCustomClient)
                {
                    HttpClient?.Dispose();
                }

                isDisposed = true;
            }
        }

        #endregion IDisposable



        private bool isCustomClient;
    }
}
