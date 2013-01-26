using System;
using System.Web;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;

namespace ODataSparqlLib
{
    internal class ODataSparqlRequestHandler
    {
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
            if (context.Request.AcceptTypes != null)
            {
                messageWriterSettings.SetContentType(String.Join(",", context.Request.AcceptTypes),
                                                     context.Request.Headers["Accept-Charset"]);
            }
            var responseMessage = new HttpDataResponseMessage(context.Response);
            var feedGenerator = new ODataFeedGenerator(
                responseMessage, 
                _config.Map,
                serviceBaseUri.ToString(),
                messageWriterSettings);
            sparqlGenerator.SparqlQueryModel.Execute(_config.SparqlEndpoint, feedGenerator);
            context.Response.ContentType = "application/atom+xml";
        }

    }
}