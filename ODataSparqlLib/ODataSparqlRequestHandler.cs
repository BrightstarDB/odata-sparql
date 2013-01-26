using System;
using System.Linq;
using System.Web;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;

namespace ODataSparqlLib
{
    internal class ODataSparqlRequestHandler
    {
        private static readonly string[] AtomMimeTypes = new string[] {"application/atom+xml", "application/xml"};
        private static readonly string[] JsonMimeTypes = new string[] {"application/json"};
        private static readonly string[] AllMimeTypes = AtomMimeTypes.Union(JsonMimeTypes).ToArray();
        private readonly ODataSparqlServiceConfiguration _config;

        public ODataSparqlRequestHandler(ODataSparqlServiceConfiguration serviceConfiguration)
        {
            _config = serviceConfiguration;
        }

        public void ProcessRequest(HttpContext context, Uri serviceBaseUri)
        {
            var sparqlGenerator = new SparqlGenerator(_config.Map);
            var parsedQuery = QueryDescriptorQueryNode.ParseUri(
                context.Request.Url,
                serviceBaseUri,
                _config.Model);
            sparqlGenerator.ProcessQuery(parsedQuery);
            var messageWriterSettings = new ODataMessageWriterSettings(_config.BaseODataWriterSettings)
                {
                    BaseUri = serviceBaseUri
                };
            //var formatKind = SelectOutputFormat(context.Request);
            //messageWriterSettings.SetContentType(formatKind);
            messageWriterSettings.SetContentType(String.Join(",", context.Request.AcceptTypes), context.Request.Headers["Accept-Charset"]);
            var responseMessage = new HttpDataResponseMessage(context.Response);
            var feedGenerator = new ODataFeedGenerator(
                responseMessage, 
                _config.Map,
                serviceBaseUri.ToString(),
                messageWriterSettings);
            sparqlGenerator.SparqlQueryModel.Execute(_config.SparqlEndpoint, feedGenerator);
            context.Response.ContentType = "application/atom+xml";
        }

        ODataFormat SelectOutputFormat(HttpRequest request)
        {
            var header = request.Headers["AcceptType"];
            var bestMatch = MimeParse.BestMatch(AllMimeTypes, request.Headers["Accept"]);
            if (string.IsNullOrEmpty(bestMatch))
            {
                throw new Exception("No valid mime-type in request header");
            }
            if (AtomMimeTypes.Contains(bestMatch)) return ODataFormat.Atom;
            if (JsonMimeTypes.Contains(bestMatch)) return ODataFormat.VerboseJson;
            throw new Exception("Could not match mime-types in request header");
        }
    }
}