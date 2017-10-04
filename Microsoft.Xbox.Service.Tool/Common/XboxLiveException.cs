// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using System;
    using System.Net;
    using System.Net.Http;

    public enum XboxLiveErrorStatus
    {
        /// <summary>
        /// Unexpected Error, non-transient
        /// </summary>
        UnExpectedError,

        /// <summary>
        /// Fail to sign in, non-transient
        /// </summary>
        AuthenticationFailure,

        /// <summary>
        /// The client is unauthorized to access particular resource, non-transient
        /// </summary>
        Forbidden,

        /// <summary>
        /// The client didn't find particular resource, non-transient
        /// </summary>
        NotFound,

        /// <summary>
        /// Invalid client request, non-transient
        /// </summary>
        BadRequest,

        /// <summary>
        /// Client is sending too many request, transient
        /// </summary>
        TooManyRequsts,

        /// <summary>
        /// Server error, transient
        /// </summary>
        ServerError,

        /// <summary>
        /// Client failed to establish communication with service, transient
        /// </summary>
        NetworkError,
    }

    public class XboxLiveException : Exception
    {
        public HttpResponseMessage Response;

        public XboxLiveErrorStatus ErrorStatus { get; private set; } = XboxLiveErrorStatus.UnExpectedError;

        public bool IsTransient { get; private set; } = true;

        public XboxLiveException(string message):
            base(message)
        {
        }

        public XboxLiveException(string message, HttpResponseMessage response, Exception innerException)
            : base(message, innerException)
        {
            Response = response;

            // if response 
            if (response != null)
            {
                ErrorStatusFromHttpStatus(response.StatusCode);
            }
            else
            {
                HttpRequestException httpEx = innerException as HttpRequestException;

                if (httpEx != null)
                {
                    this.ErrorStatus = XboxLiveErrorStatus.NetworkError;
                    this.IsTransient = true;
                }

                // Check spacial case to overwrite status value.
                CheckInnerWebException(innerException);
            }
        }

        private void ErrorStatusFromHttpStatus(HttpStatusCode httpStatus)
        {
            if (httpStatus == HttpStatusCode.Unauthorized)
            {
                this.ErrorStatus = XboxLiveErrorStatus.AuthenticationFailure;
                this.IsTransient = false;
            }
            else if (httpStatus == HttpStatusCode.Forbidden)
            {
                this.ErrorStatus = XboxLiveErrorStatus.Forbidden;
                this.IsTransient = false;
            }
            else if (httpStatus == HttpStatusCode.NotFound)
            {
                this.ErrorStatus = XboxLiveErrorStatus.NotFound;
                this.IsTransient = false;
            }
            else if (httpStatus == HttpStatusCode.RequestTimeout)
            {
                this.ErrorStatus = XboxLiveErrorStatus.NetworkError;
                this.IsTransient = true;
            }
            else if (httpStatus == (HttpStatusCode)429)
            {
                this.ErrorStatus = XboxLiveErrorStatus.TooManyRequsts;
                this.IsTransient = true;
            }
            else if (httpStatus >= HttpStatusCode.BadRequest && httpStatus < HttpStatusCode.InternalServerError) //between [400, 500)
            {
                this.ErrorStatus = XboxLiveErrorStatus.BadRequest;
                this.IsTransient = false;
            }
            else if (httpStatus >= HttpStatusCode.InternalServerError && httpStatus < (HttpStatusCode)600) //between [500, 600)
            {
                this.ErrorStatus = XboxLiveErrorStatus.ServerError;
                this.IsTransient = true;
            }
        }

        private void CheckInnerWebException(Exception ex)
        {
            WebException webEx = ex.InnerException as WebException;
            if (webEx != null)
            {
                HttpWebResponse wsResponse = webEx.Response as HttpWebResponse;
                if (wsResponse != null)
                {
                    ErrorStatusFromHttpStatus(wsResponse.StatusCode);
                }
            }
        }
    }
}
