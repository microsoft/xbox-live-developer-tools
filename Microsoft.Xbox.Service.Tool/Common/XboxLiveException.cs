// Copyright (c) Microsoft Corporation
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Xbox.Services.Tool
{
    using System;
    using System.Net;
    using System.Net.Http;

    /// <summary>
    /// The XboxLive Exception.
    /// </summary>
    public class XboxLiveException : Exception
    {
        /// <summary>
        ///  The http response caused the exception, could be null if no http related.
        /// </summary>
        public HttpResponseMessage Response;

        /// <summary>
        ///  The errro status of the exception
        /// </summary>
        public XboxLiveErrorStatus ErrorStatus { get; private set; } = XboxLiveErrorStatus.UnExpectedError;

        /// <summary>
        /// Whether or not if the exception is transient.
        /// </summary>
        public bool IsTransient { get; private set; } = true;

        /// <summary>
        /// Gets a message that describes the current exception.
        /// </summary>
        public override string Message {
            get { return base.Message + $", Status: {this.ErrorStatus}"; }
        }

        internal XboxLiveException(string message):
            base(message)
        {
        }

        internal XboxLiveException(XboxLiveErrorStatus errorStatus, string message) :
            base(message)
        {
            this.ErrorStatus = errorStatus;
        }

        internal XboxLiveException(string message, XboxLiveErrorStatus errorStatus, Exception innerException) :
            base(message, innerException)
        {
            this.ErrorStatus = errorStatus;
        }

        internal  XboxLiveException(string message, HttpResponseMessage response, Exception innerException)
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
                if (innerException is HttpRequestException httpEx)
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
            if (ex.InnerException is WebException webEx)
            {
                if (webEx.Response is HttpWebResponse wsResponse)
                {
                    ErrorStatusFromHttpStatus(wsResponse.StatusCode);
                }
            }
        }
    }
}
