// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.DevTools.XblConfig
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;
    using System.Security;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Xbox.Services.DevTools.Authentication;
    using Microsoft.Xbox.Services.DevTools.Common;
    using Microsoft.Xbox.Services.DevTools.XblConfig.Contracts;

    /// <summary>
    /// Provides methods for configuring Xbox Live.
    /// </summary>
    public static class ConfigurationManager
    {
        private static Uri xconUri = new Uri(ClientSettings.Singleton.XConEndpoint);
        private static Uri xorcUri = new Uri(ClientSettings.Singleton.XOrcEndpoint);
        private static Uri xcertUri = new Uri(ClientSettings.Singleton.XCertEndpoint);
        private static Uri xachUri = new Uri(ClientSettings.Singleton.XAchEndpoint);
        private static Uri xfusUri = new Uri(ClientSettings.Singleton.XFusEndpoint);

        // Certain document types are used solely for testing. These should be filtered from responses.
        private static string[] testTypes = new string[] { "acctest1", "accpartest1", "test1", "test2", "demodoc" };

        /// <summary>
        /// Asynchronously gets sandbox level documents for a given SCID.
        /// </summary>
        /// <param name="scid">The service configuration ID.</param>
        /// <param name="sandbox">The sandbox.</param>
        public static async Task<DocumentsResponse> GetSandboxDocumentsAsync(Guid scid, string sandbox)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid, sandbox))
            {
                using (HttpResponseMessage response = (await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xconUri, $"/scids/{scid}/sandboxes/{sandbox}"));
                })).Response)
                {
                    response.EnsureSuccessStatusCode();
                    Stream content = await response.Content.ReadAsStreamAsync();
                    return new DocumentsResponse(content, response.Headers);
                }
            }
        }

        /// <summary>
        /// Asynchronously commits sandbox level documents back to Xbox Live.
        /// </summary>
        /// <param name="files">A collection of <see cref="FileStream"/> objects pointing to files to commit.</param>
        /// <param name="scid">The service configuration ID to commit to.</param>
        /// <param name="sandbox">The sandbox to commit to.</param>
        /// <param name="eTag">The ETag of the previous commit used for concurrency checks. If null, the ETag from the latest commit will be used.</param>
        /// <param name="validateOnly">Set to 'true' if a test commit should be performed without persisting the data resulting in validation only; 'false' persists the commit.</param>
        /// <param name="message">The commit message asssociated with this changeset.</param>
        public static async Task<ConfigResponse<ValidationResponse>> CommitSandboxDocumentsAsync(IEnumerable<FileStream> files, Guid scid, string sandbox, string eTag, bool validateOnly, string message)
        {
            using (Stream zipStream = await CreateZipArchiveAsync(files))
            {
                using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid, sandbox))
                {
                    if (string.IsNullOrEmpty(eTag))
                    {
                        History history = (await GetHistoryAsync(scid, sandbox, View.Working, 1)).Result.FirstOrDefault();
                        if (history != null)
                        {
                            eTag = history.ETag;
                        }
                    }
                    
                    XboxLiveHttpResponse response = await request.SendAsync(() =>
                    {
                        string operation = validateOnly ? "Validate" : "Commit";
                        return CreateCommitRequest(zipStream, new Uri(xconUri, $"/scids/{scid}/sandboxes/{sandbox}?view=working&op={operation}"), HttpMethod.Put, eTag, message);
                    });
                    using (HttpResponseMessage httpResponse = response.Response)
                    {
                        if (httpResponse.Content.Headers.ContentLength > 0)
                        {
                            return new ConfigResponse<ValidationResponse>()
                            {
                                CorrelationId = response.CorrelationId,
                                Result = await httpResponse.Content.DeserializeJsonAsync<ValidationResponse>()
                            };
                        }
                        else
                        {
                            string errorMessage = "An unknown error has occurred.";
                            if (httpResponse.Headers.TryGetValues("X-Err", out IEnumerable<string> values))
                            {
                                errorMessage = values.First();
                            }

                            throw new XboxLiveException(errorMessage);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously commits sandbox level documents back to Xbox Live.
        /// </summary>
        /// <param name="filePaths">A collection of file paths to commit.</param>
        /// <param name="scid">The service configuration ID to commit to.</param>
        /// <param name="sandbox">The sandbox to commit to.</param>
        /// <param name="eTag">The ETag of the previous commit used for concurrency checks. If null, the ETag from the latest commit will be used.</param>
        /// <param name="validateOnly">Set to 'true' if a test commit should be performed without persisting the data resulting in validation only; 'false' persists the commit.</param>
        /// <param name="message">The commit message asssociated with this changeset.</param>
        public static async Task<ConfigResponse<ValidationResponse>> CommitSandboxDocumentsAsync(IEnumerable<string> filePaths, Guid scid, string sandbox, string eTag, bool validateOnly, string message)
        {
            List<FileStream> streams = new List<FileStream>();
            foreach (string path in filePaths)
            {
                streams.Add(File.OpenRead(path));
            }

            return await CommitSandboxDocumentsAsync(streams, scid, sandbox, eTag, validateOnly, message);
        }

        /// <summary>
        /// Asynchronously gets a specific account level document.
        /// </summary>
        /// <param name="accountId">The account ID of the user to obtain documents for.</param>
        /// <param name="filename">The filename of the file to retrieve.</param>
        public static async Task<DocumentsResponse> GetAccountDocumentsAsync(Guid accountId, string filename)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, AccountId = accountId }))
            {
                using (HttpResponseMessage response = (await request.SendAsync(() =>
                {
                    string filter = string.Empty;
                    if (filename != null)
                    {
                        filter = $"&documentFilter={filename}";
                    }
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xconUri, $"/accounts/{accountId}/?view=working{filter}"));
                })).Response)
                {
                    response.EnsureSuccessStatusCode();
                    Stream content = await response.Content.ReadAsStreamAsync();
                    return new DocumentsResponse(content, response.Headers);
                }
            }
        }

        /// <summary>
        /// Asynchronously gets account level documents.
        /// </summary>
        /// <param name="accountId">The account ID of the user to obtain documents for.</param>
        public static async Task<DocumentsResponse> GetAccountDocumentsAsync(Guid accountId)
        {
            return await GetAccountDocumentsAsync(accountId, null);
        }

        /// <summary>
        /// Commits account level documents back to Xbox Live.
        /// </summary>
        /// <param name="files">A collection of <see cref="FileStream"/> objects containing the files to commit.</param>
        /// <param name="accountId">The account ID of the user account which owns these files.</param>
        /// <param name="eTag">The ETag of the previous commit used for concurrency checks. If null, the ETag from the latest commit will be used.</param>
        /// <param name="validateOnly">Set to 'true' if a test commit should be performed without persisting the data resulting in validation only; 'false' persists the commit.</param>
        /// <param name="message">The commit message asssociated with this changeset.</param>
        public static async Task<ConfigResponse<ValidationResponse>> CommitAccountDocumentsAsync(IEnumerable<FileStream> files, Guid accountId, string eTag, bool validateOnly, string message)
        {
            using (Stream zipStream = await CreateZipArchiveAsync(files))
            {
                using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AccountId = accountId, AutoAttachAuthHeader = true }))
                {
                    if (string.IsNullOrEmpty(eTag))
                    {
                        DocumentsResponse emptySet = await GetAccountDocumentsAsync(accountId, "notused.xxx");
                        eTag = emptySet.ETag;
                    }

                    XboxLiveHttpResponse response = await request.SendAsync(() =>
                    {
                        string operation = validateOnly ? "Validate" : "Commit";
                        return CreateCommitRequest(zipStream, new Uri(xconUri, $"/accounts/{accountId}?view=working&op={operation}"), HttpMethod.Put, eTag, message);
                    });

                    using (HttpResponseMessage httpResponse = response.Response)
                    {
                        if (httpResponse.Content.Headers.ContentLength > 0)
                        {
                            return new ConfigResponse<ValidationResponse>()
                            {
                                CorrelationId = response.CorrelationId,
                                Result = await httpResponse.Content.DeserializeJsonAsync<ValidationResponse>()
                            };
                        }
                        else
                        {
                            string errorMessage = "An unknown error has occurred.";
                            if (httpResponse.Headers.TryGetValues("X-Err", out IEnumerable<string> values))
                            {
                                errorMessage = values.First();
                            }

                            throw new XboxLiveException(errorMessage);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Commits account level documents back to Xbox Live.
        /// </summary>
        /// <param name="filePaths">A collection of file paths to commit.</param>
        /// <param name="accountId">The account ID of the user account which owns these files.</param>
        /// <param name="eTag">The ETag of the previous commit used for concurrency checks. If null, the ETag from the latest commit will be used.</param>
        /// <param name="validateOnly">Set to 'true' if a test commit should be performed without persisting the data resulting in validation only; 'false' persists the commit.</param>
        /// <param name="message">The commit message asssociated with this changeset.</param>
        public static async Task<ConfigResponse<ValidationResponse>> CommitAccountDocumentsAsync(IEnumerable<string> filePaths, Guid accountId, string eTag, bool validateOnly, string message)
        {
            List<FileStream> streams = new List<FileStream>();
            foreach (string path in filePaths)
            {
                streams.Add(File.OpenRead(path));
            }

            return await CommitAccountDocumentsAsync(streams, accountId, eTag, validateOnly, message);
        }

        /// <summary>
        /// Asynchronously gets a list of relying party documents for a given account.
        /// </summary>
        /// <param name="accountId">The account ID of the account that owns the documents.</param>
        public static async Task<ConfigResponse<IEnumerable<RelyingParty>>> GetRelyingPartiesAsync(Guid accountId)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, AccountId = accountId }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xorcUri, $"/relyingparties?accountId={accountId}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    IEnumerable<RelyingParty> relyingParties = await httpResponse.Content.DeserializeJsonAsync<IEnumerable<RelyingParty>>();
                    return new ConfigResponse<IEnumerable<RelyingParty>>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = relyingParties
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a specific relying party document for a given account.
        /// </summary>
        /// <param name="accountId">The account ID of the account that owns the document.</param>
        /// <param name="filename">The filename of the document to retrieve.</param>
        public static async Task<DocumentsResponse> GetRelyingPartyDocumentAsync(Guid accountId, string filename)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, AccountId = accountId }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xconUri, $"/accounts/{accountId}/?documentFilter={filename}&view=working"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    using (Stream content = await httpResponse.Content.ReadAsStreamAsync())
                    {
                        return new DocumentsResponse(content, httpResponse.Headers);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a collection of web services for a given account.
        /// </summary>
        /// <param name="accountId">The account ID associated with the web services.</param>
        public static async Task<ConfigResponse<IEnumerable<WebService>>> GetWebServicesAsync(Guid accountId)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, AccountId = accountId }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xorcUri, $"/services?accountId={accountId}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    IEnumerable<WebService> webServices = await httpResponse.Content.DeserializeJsonAsync<IEnumerable<WebService>>();
                    return new ConfigResponse<IEnumerable<WebService>>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = webServices
                    };
                }
            }
        }

        /// <summary>
        /// Creates a web service.
        /// </summary>
        /// <param name="accountId">The account ID associated with the web service.</param>
        /// <param name="name">The name of the web service.</param>
        /// <param name="telemetryAccess">A boolean value which enables your service to retrieve game telemetry data for any of your games.</param>
        /// <param name="appChannelsAccess">A boolean value which gives the media provider owning the service the authority to programmatically publish app channels for consumption on console through the OneGuide twist.</param>
        public static async Task<ConfigResponse<WebService>> CreateWebServiceAsync(Guid accountId, string name, bool telemetryAccess, bool appChannelsAccess)
        {
            return await CreateWebServiceAsync(new WebService()
            {
                AccountId = accountId,
                Name = name,
                TelemetryAccess = telemetryAccess,
                AppChannelsAccess = appChannelsAccess
            });
        }

        /// <summary>
        /// Creates a web service.
        /// </summary>
        /// <param name="webService">An object containing the parameters for the web service.</param>
        public static async Task<ConfigResponse<WebService>> CreateWebServiceAsync(WebService webService)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, AccountId = webService.AccountId }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(xorcUri, "/services/"))
                    {
                        Content = new StringContent(webService.ToJson())
                    };
                    requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    return requestMessage;
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    WebService newWebService = await httpResponse.Content.DeserializeJsonAsync<WebService>();
                    return new ConfigResponse<WebService>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = newWebService
                    };
                }
            }
        }

        /// <summary>
        /// Updates the properties of a web service.
        /// </summary>
        /// <param name="serviceId">The web service ID.</param>
        /// <param name="accountId">The account ID associated with the web service.</param>
        /// <param name="name">The name of the web service.</param>
        /// <param name="telemetryAccess">A boolean value which enables your service to retrieve game telemetry data for any of your games.</param>
        /// <param name="appChannelsAccess">A boolean value which gives the media provider owning the service the authority to programmatically publish app channels for consumption on console through the OneGuide twist.</param>
        public static async Task<ConfigResponse<WebService>> UpdateWebServiceAsync(Guid serviceId, Guid accountId, string name, bool telemetryAccess, bool appChannelsAccess)
        {
            return await UpdateWebServiceAsync(new WebService()
            {
                AccountId = accountId,
                Name = name,
                TelemetryAccess = telemetryAccess,
                AppChannelsAccess = appChannelsAccess,
                ServiceId = serviceId
            });
        }

        /// <summary>
        /// Updates the properties of a web service.
        /// </summary>
        /// <param name="webService">An object containing the parameters for the web service.</param>
        public static async Task<ConfigResponse<WebService>> UpdateWebServiceAsync(WebService webService)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, AccountId = webService.AccountId }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, new Uri(xorcUri, $"/services/{webService.ServiceId}"))
                    {
                        Content = new StringContent(webService.ToJson())
                    };
                    requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    return requestMessage;
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    WebService newWebService = await httpResponse.Content.DeserializeJsonAsync<WebService>();
                    return new ConfigResponse<WebService>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = newWebService
                    };
                }
            }
        }

        /// <summary>
        /// Deletes a web service.
        /// </summary>
        /// <param name="accountId">The account ID associated with the web service.</param>
        /// <param name="serviceId">The web service ID.</param>
        public static async Task<ConfigResponse> DeleteWebServiceAsync(Guid accountId, Guid serviceId)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, AccountId = accountId }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Delete, new Uri(xorcUri, $"/services/{serviceId}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    return new ConfigResponse
                    {
                        CorrelationId = response.CorrelationId
                    };
                }
            }
        }

        /// <summary>
        /// Generates a certificate with corresponding private key for a given service ID and password.
        /// </summary>
        /// <param name="accountId">The account ID of the user.</param>
        /// <param name="serviceId">The web service ID.</param>
        /// <param name="password">The password to secure the certificate with.</param>
        public static async Task<ConfigResponse<Stream>> GenerateWebServiceCertificateAsync(Guid accountId, Guid serviceId, SecureString password)
        {
            using (CertificateHelper certHelper = new CertificateHelper())
            {
                string csrRequest = certHelper.GenerateCertRequest();
                using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, AccountId = accountId }))
                {
                    XboxLiveHttpResponse response = await request.SendAsync(() =>
                    {
                        CertRequest certRequest = new CertRequest()
                        {
                            Properties = new Dictionary<string, string>
                            {
                                { "publickey", csrRequest },
                                { "serviceid", serviceId.ToString() }
                            },
                            CertType = "BUSINESSPARTNERCERT"
                        };

                        HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(xcertUri, $"/certificate/create"))
                        {
                            Content = new StringContent(certRequest.ToJson())
                        };
                        message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                        return message;
                    });
                    using (HttpResponseMessage httpResponse = response.Response)
                    {
                        httpResponse.EnsureSuccessStatusCode();
                        CertResponse certResponse = await httpResponse.Content.DeserializeJsonAsync<CertResponse>();
                        byte[] cert = Convert.FromBase64String(certResponse.Certificate);
                        byte[] pfx = certHelper.ExportPfx(cert, password);
                        MemoryStream stream = new MemoryStream(pfx);
                        return new ConfigResponse<Stream>
                        {
                            CorrelationId = response.CorrelationId,
                            Result = stream
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously gets the history of past commits.
        /// </summary>
        /// <param name="scid">The service configuration ID.</param>
        /// <param name="sandbox">The sandbox to obtain history for.</param>
        /// <param name="view">Which view to obtain history commits for.</param>
        /// <param name="count">The max number of history entries to obtain.</param>
        public static async Task<ConfigResponse<IEnumerable<History>>> GetHistoryAsync(Guid scid, string sandbox, View view, int count)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid, sandbox))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xconUri, $"/scids/{scid}/sandboxes/{sandbox}/history?view={view.ToString().ToLowerInvariant()}&count={count}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    HistoryResponse historyEntries = await httpResponse.Content.DeserializeJsonAsync<HistoryResponse>();
                    return new ConfigResponse<IEnumerable<History>>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = historyEntries.History
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a collection of products associated with an account.
        /// </summary>
        /// <param name="accountId">The account ID associated with the products.</param>
        public static async Task<ConfigResponse<IEnumerable<Product>>> GetProductsAsync(Guid accountId)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, AccountId = accountId }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xorcUri, $"/products?accountId={accountId}&count=500"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    IEnumerable<Product> products = await httpResponse.Content.DeserializeJsonAsync<IEnumerable<Product>>();
                    return new ConfigResponse<IEnumerable<Product>>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = products
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a single product by product ID.
        /// </summary>
        /// <param name="productId">The ID of the product to obtain.</param>
        public static async Task<ConfigResponse<Product>> GetProductAsync(Guid productId)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, Scid = productId }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xorcUri, $"/products/{productId}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    Product product = await httpResponse.Content.DeserializeJsonAsync<Product>();
                    return new ConfigResponse<Product>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = product
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a single product by a title ID.
        /// </summary>
        /// <param name="titleId">The Title ID of the product to obtain.</param>
        public static async Task<ConfigResponse<Product>> GetProductAsync(string titleId)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, TitleId = titleId }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xorcUri, $"/products/?titleId={titleId}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    IEnumerable<Product> products = await httpResponse.Content.DeserializeJsonAsync<IEnumerable<Product>>();
                    Product product = null;
                    if (products.Count() == 1)
                    {
                        product = products.First();
                    }

                    return new ConfigResponse<Product>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = product
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a single product by scid.
        /// </summary>
        /// <param name="scid">The service configuration ID to obtain.</param>
        public static async Task<ConfigResponse<Product>> GetProductFromScidAsync(Guid scid)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AutoAttachAuthHeader = true, Scid = scid }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xorcUri, $"/products?serviceConfigId={scid}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    IEnumerable<Product> products = await httpResponse.Content.DeserializeJsonAsync<IEnumerable<Product>>();
                    return new ConfigResponse<Product>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = products.FirstOrDefault()
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a collection of document schema types. 
        /// </summary>
        public static async Task<ConfigResponse<IEnumerable<string>>> GetSchemaTypesAsync()
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest())
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xconUri, "/schema"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    SchemaTypes responseObject = await httpResponse.Content.DeserializeJsonAsync<SchemaTypes>();

                    // Filter out the test document types as they are not needed by end developers.
                    return new ConfigResponse<IEnumerable<string>>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = responseObject.AvailableSchemaTypes.Except(testTypes).OrderBy(x => x)
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a collection of versions available for a given document schema type.
        /// </summary>
        /// <param name="schemaType">The type of the schema to obtain the versions for.</param>
        public static async Task<ConfigResponse<IEnumerable<SchemaVersion>>> GetSchemaVersionsAsync(string schemaType)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest())
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xconUri, $"/schema/{schemaType}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    SchemaVersions responseObject = await httpResponse.Content.DeserializeJsonAsync<SchemaVersions>();
                    List<SchemaVersion> result = new List<SchemaVersion>();
                    foreach (string item in responseObject.AvailableSchema)
                    {
                        Uri uri = new Uri(item);
                        int version = int.Parse(uri.Segments[uri.Segments.Length - 1]);
                        result.Add(new SchemaVersion() { Namespace = item, Version = version });
                    }

                    return new ConfigResponse<IEnumerable<SchemaVersion>>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = result
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a specific schema.
        /// </summary>
        /// <param name="schemaType">The type of the document schema to obtain.</param>
        /// <param name="version">The version of the document schema.</param>
        public static async Task<ConfigResponse<Stream>> GetSchemaAsync(string schemaType, int version)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest())
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xconUri, $"/schema/{schemaType}/{version}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    using (Stream schema = await httpResponse.Content.ReadAsStreamAsync())
                    {
                        return new ConfigResponse<Stream>()
                        {
                            CorrelationId = response.CorrelationId,
                            Result = schema.Copy()
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a list of sandboxes for a given account.
        /// </summary>
        /// <param name="accountId">The account ID associated with the list of sandboxes.</param>
        public static async Task<ConfigResponse<IEnumerable<string>>> GetSandboxesAsync(Guid accountId)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(new RequestParameters() { AccountId = accountId, AutoAttachAuthHeader = true }))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xorcUri, $"/sandboxes?accountId={accountId}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    // Sort the sandboxes by moniker first, then numeric portion so it appears in the correct order.
                    // For example, MSFT.1, MSFT.2, MSFT.10 instead of MSFT.1, MSFT.10, MSFT.2.
                    var sandboxes = (await httpResponse.Content.DeserializeJsonAsync<IEnumerable<Sandbox>>())
                        .Select(c =>
                        {
                            string[] sandbox = c.SandboxId.Split('.');
                            if (sandbox.Length == 2 && int.TryParse(sandbox[1], out int x))
                            {
                                return new
                                {
                                    Moniker = sandbox[0],
                                    Index = x,
                                    Sandbox = c.SandboxId
                                };
                            }
                            return new
                            {
                                Moniker = c.SandboxId,
                                Index = 0,
                                Sandbox = c.SandboxId
                            };
                        })
                        .OrderBy(c => c.Moniker)
                        .ThenBy(c => c.Index);

                    return new ConfigResponse<IEnumerable<string>>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = sandboxes.Select(c => c.Sandbox)
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously performs a publish.
        /// </summary>
        /// <param name="scid">The service configuration ID to publish.</param>
        /// <param name="sourceSandbox">The sandbox to publish from.</param>
        /// <param name="destinationSandbox">The sandbox to publish to.</param>
        /// <param name="validateOnly">A boolean value indicating whether to only validate a publish call.</param>
        /// <param name="version">The changeset version to publish.</param>
        public static async Task<ConfigResponse<PublishResponse>> PublishAsync(Guid scid, string sourceSandbox, string destinationSandbox, bool validateOnly, ulong version)
        {
            ConfigResponse<Product> product = await GetProductFromScidAsync(scid);
            Guid productId = product.Result.ProductId;
            string option = validateOnly ? "Validate" : "PublishForce";
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid, sourceSandbox, destinationSandbox))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Post, new Uri(xorcUri, $"/products/{productId}/sandboxes/{destinationSandbox}/publish?version={version}&source={sourceSandbox}&option={option}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    PublishResponse publishResponse = await httpResponse.Content.DeserializeJsonAsync<PublishResponse>();
                    return new ConfigResponse<PublishResponse>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = publishResponse
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously performs a publish with the latest version.
        /// </summary>
        /// <param name="scid">The service configuration ID to publish.</param>
        /// <param name="sourceSandbox">The sandbox to publish from.</param>
        /// <param name="destinationSandbox">The sandbox to publish to.</param>
        /// <param name="validateOnly">A boolean value indicating whether to only validate a publish call.</param>
        public static async Task<ConfigResponse<PublishResponse>> PublishAsync(Guid scid, string sourceSandbox, string destinationSandbox, bool validateOnly)
        {
            ConfigResponse<IEnumerable<History>> history = await GetHistoryAsync(scid, sourceSandbox, View.Working, 1);
            return await PublishAsync(scid, sourceSandbox, destinationSandbox, validateOnly, history.Result.First().Version);
        }
        
        /// <summary>
        /// Gets the publish status for a given sandbox.
        /// </summary>
        /// <param name="scid">The service configuration ID to get the status for.</param>
        /// <param name="destinationSandbox">The sandbox that configuration was published to.</param>
        public static async Task<ConfigResponse<PublishResponse>> GetPublishStatusAsync(Guid scid, string destinationSandbox)
        {
            ConfigResponse<Product> product = await GetProductFromScidAsync(scid);
            Guid productId = product.Result.ProductId;
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid, destinationSandbox))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xorcUri, $"/products/{productId}/sandboxes/{destinationSandbox}/publish"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    PublishResponse publishResponse = await httpResponse.Content.DeserializeJsonAsync<PublishResponse>();
                    return new ConfigResponse<PublishResponse>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = publishResponse
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets the details of an achievement image.
        /// </summary>
        /// <param name="scid">The service configuration ID that this image is for.</param>
        /// <param name="assetId">The asset ID associated with this image.</param>
        public static async Task<ConfigResponse<AchievementImage>> GetAchievementImageAsync(Guid scid, Guid assetId)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xachUri, $"/scids/{scid}/images/{assetId}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    AchievementImage image = await httpResponse.Content.DeserializeJsonAsync<AchievementImage>();
                    return new ConfigResponse<AchievementImage>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = image
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously gets a list of achievement images associated with a given SCID.
        /// </summary>
        /// <param name="scid">The service configuration ID that these images are for.</param>
        public static async Task<ConfigResponse<IEnumerable<AchievementImage>>> GetAchievementImagesAsync(Guid scid)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Get, new Uri(xachUri, $"/scids/{scid}/images/"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    AchievementImagesResponse imageResponse = await httpResponse.Content.DeserializeJsonAsync<AchievementImagesResponse>();
                    return new ConfigResponse<IEnumerable<AchievementImage>>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = imageResponse.Images
                    };
                }
            }
        }

        /// <summary>
        /// Asynchronously uploads an achievement image for a given sandbox.
        /// </summary>
        /// <param name="scid">The service configuration ID that this image is for.</param>
        /// <param name="filename">The name of the file. i.e. file.png</param>
        /// <param name="file">A stream containing the achievement image.</param>
        /// <exception cref="ArgumentException">Throws when image is not 1920x1080 pixels.</exception>
        /// <exception cref="ArgumentNullException">Throws if filename is null or empty.</exception>
        public static async Task<ConfigResponse<AchievementImage>> UploadAchievementImageAsync(Guid scid, string filename, Stream file)
        {
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException(nameof(filename));
            }

            Image image = Image.FromStream(file);
            if (image.Width != 1920 || image.Height != 1080)
            {
                throw new ArgumentException("The provided image must be 1920x1080 pixels in size.", nameof(file));
            }

            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Post, new Uri(xachUri, "/assets/initialize"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    XachInitializeResponse initializeResponse = await httpResponse.Content.DeserializeJsonAsync<XachInitializeResponse>();
                    SetMetadataResponse setMetadataResponse = await SetMetadata(scid, filename, file.Length, initializeResponse.XfusId, initializeResponse.XfusToken);

                    int[] chunkList = setMetadataResponse.ChunkList;
                    int chunkSize = setMetadataResponse.ChunkSize;
                    byte[] fileBytes = file.ToArray();

                    do
                    {
                        await Task.WhenAll(chunkList.Select(c => UploadChunk(scid, setMetadataResponse.Id, c, initializeResponse.XfusToken, fileBytes.Skip((c - 1) * chunkSize).Take(chunkSize).ToArray())));
                        UploadImageResponse finishResponse = await FinishXfusUpload(scid, setMetadataResponse.Id, initializeResponse.XfusToken);
                        chunkList = finishResponse.MissingChunks;
                    }
                    while (chunkList?.Length > 0);
                    
                    AchievementImage finalUploadResponse = await FinishAchievementImageUpload(scid, initializeResponse.XfusId);

                    return new ConfigResponse<AchievementImage>()
                    {
                        CorrelationId = response.CorrelationId,
                        Result = finalUploadResponse
                    };
                }
            }
        }

        private static async Task<SetMetadataResponse> SetMetadata(Guid scid, string filename, long filesize, Guid id, string token)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Post, new Uri(xfusUri, $"/upload/SetMetadata?filename={filename}&fileSize={filesize}&id={id}&token={HttpUtility.UrlEncode(token)}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    return await httpResponse.Content.DeserializeJsonAsync<SetMetadataResponse>();
                }
            }
        }

        private static async Task<UploadImageResponse> UploadChunk(Guid scid, Guid id, int blockNumber, string token, byte[] fileContents)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(xfusUri, $"/upload/uploadchunk/{id}?blockNumber={blockNumber}&token={HttpUtility.UrlEncode(token)}"))
                    {
                        Content = new ByteArrayContent(fileContents)
                    };
                    message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                    return message;
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    return await httpResponse.Content.DeserializeJsonAsync<UploadImageResponse>();
                }
            }
        }

        private static async Task<UploadImageResponse> FinishXfusUpload(Guid scid, Guid id, string token)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    return new HttpRequestMessage(HttpMethod.Post, new Uri(xfusUri, $"/upload/finished/{id}?token={HttpUtility.UrlEncode(token)}"));
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    return await httpResponse.Content.DeserializeJsonAsync<UploadImageResponse>();
                }
            }
        }

        private static async Task<AchievementImage> FinishAchievementImageUpload(Guid scid, Guid xfusId)
        {
            using (XboxLiveHttpRequest request = new XboxLiveHttpRequest(true, scid))
            {
                XboxLiveHttpResponse response = await request.SendAsync(() =>
                {
                    HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Post, new Uri(xachUri, $"/scids/{scid}/images"));
                    AchievementImageUploadRequest requestBody = new AchievementImageUploadRequest() { XfusId = xfusId };
                    message.Content = new StringContent(requestBody.ToJson());
                    message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    return message;
                });
                using (HttpResponseMessage httpResponse = response.Response)
                {
                    httpResponse.EnsureSuccessStatusCode();
                    return await httpResponse.Content.DeserializeJsonAsync<AchievementImage>();
                }
            }
        }

        private static async Task<Stream> CreateZipArchiveAsync(IEnumerable<FileStream> files)
        {
            MemoryStream memoryStream = new MemoryStream();
            using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, false))
            {
                foreach (FileStream file in files)
                {
                    using (file)
                    {
                        ZipArchiveEntry fileEntry = archive.CreateEntry(Path.GetFileName(file.Name));

                        using (Stream fileEntryStream = fileEntry.Open())
                        {
                            await file.CopyToAsync(fileEntryStream);
                        }
                    }
                }
            }

            return memoryStream;
        }

        private static HttpRequestMessage CreateCommitRequest(Stream content, Uri uri, HttpMethod method, string eTag, string commitMessage)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage(method, uri);
            byte[] contentBytes = content.ToArray();
            ByteArrayContent byteArrayContent = new ByteArrayContent(contentBytes);
            requestMessage.Content = byteArrayContent;

            requestMessage.Headers.Add("If-Match", eTag);
            if (!string.IsNullOrEmpty(commitMessage))
            {
                requestMessage.Headers.Add("X-Message", commitMessage);
            }

            return requestMessage;
        }
    }
}