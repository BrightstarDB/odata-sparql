using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.Data.OData;

namespace ODataSparqlLib
{
    public class HttpDataRequestMessage : IODataRequestMessage
    {
        private readonly HttpRequest _request;

        public IEnumerable<KeyValuePair<string, string>> Headers { get; private set; }
        public Uri Url { get; set; }
        public string Method { get; set; }

        public HttpDataRequestMessage(HttpRequest request)
        {
            _request = request;
            Headers = _request.Headers.AllKeys.Select(key => new KeyValuePair<string, string>(key, _request.Headers[key]));
            Url = _request.Url;
            Method = request.HttpMethod;
        }

        public string GetHeader(string headerName)
        {
            return _request.Headers[headerName];
        }

        public void SetHeader(string headerName, string headerValue)
        {
            _request.Headers[headerName] = headerValue;
        }

        public Stream GetStream()
        {
            return _request.GetBufferlessInputStream();
        }
    }
}
