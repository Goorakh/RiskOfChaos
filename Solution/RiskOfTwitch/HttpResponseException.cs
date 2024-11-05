using System;
using System.Net;
using System.Net.Http;

namespace RiskOfTwitch
{
    public sealed class HttpResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public string ReasonPhrase { get; }

        public HttpResponseException(HttpStatusCode statusCode, string reasonPhrase) : base($"{statusCode} ({reasonPhrase})")
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
        }

        public HttpResponseException(HttpResponseMessage responseMessage) : this(responseMessage.StatusCode, responseMessage.ReasonPhrase)
        {
        }
    }
}
