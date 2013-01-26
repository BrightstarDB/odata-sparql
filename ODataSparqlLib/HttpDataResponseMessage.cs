using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Microsoft.Data.OData;

namespace ODataSparqlLib
{
    internal class HttpDataResponseMessage : IODataResponseMessage
    {
        private readonly HttpResponse _httpResponse;

        public HttpDataResponseMessage(HttpResponse response)
        {
            _httpResponse = response;
        }

        public IEnumerable<KeyValuePair<string, string>> Headers
        {
            get
            {
                return
                    _httpResponse.Headers.AllKeys.Select(k => new KeyValuePair<string, string>(k, _httpResponse.Headers[(string) k]));
            }
        }

        public int StatusCode
        {
            get { return _httpResponse.StatusCode; }
            set { _httpResponse.StatusCode = value; }
        }

        public string GetHeader(string headerName)
        {
            return _httpResponse.Headers[headerName];
        }

        public void SetHeader(string headerName, string headerValue)
        {
            _httpResponse.Headers[headerName] = headerValue;
        }

        public Stream GetStream()
        {
            return _httpResponse.OutputStream;
        }
    }
}