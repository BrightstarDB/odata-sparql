using System;
using System.Web;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;

namespace ODataSparqlLib
{
    internal class ODataSparqlRequestHandler
    {
        private readonly ODataSparqlServiceSettings _config;

        public ODataSparqlRequestHandler(ODataSparqlServiceSettings serviceSettings)
        {
            _config = serviceSettings;
        }

        public void ProcessRequest(HttpContext context, Uri serviceBaseUri)
        {
            var sparqlGenerator = new SparqlGenerator(_config.Map);

            var messageWriterSettings = new ODataMessageWriterSettings(_config.BaseODataWriterSettings)
                {
                    BaseUri = serviceBaseUri
                };
            if (context.Request.AcceptTypes != null)
            {
                messageWriterSettings.SetContentType(String.Join(",", context.Request.AcceptTypes),
                                                     context.Request.Headers["Accept-Charset"]);
            }

            if (serviceBaseUri.Equals(context.Request.Url))
            {
                // We need to respond with the service document
                var responseMessage = new HttpDataResponseMessage(context.Response);
                var feedGenerator = new ODataFeedGenerator(responseMessage, _config.Map, serviceBaseUri.ToString(),
                                                           messageWriterSettings);
                feedGenerator.WriteServiceDocument();
            }
            else if (serviceBaseUri.ToString().TrimEnd('/').Equals(context.Request.Url.ToString().TrimEnd('/')))
            {
                // Trimming of trailing slash to normalize - the serviceBaseUri should always have a trailing slash, 
                // but we will redirect requests that do not to the proper service document url
                context.Response.RedirectPermanent(serviceBaseUri.ToString(), true);
            }
            else
            {
                var parsedQuery = QueryDescriptorQueryNode.ParseUri(
                    context.Request.Url,
                    serviceBaseUri,
                    _config.Model);
                sparqlGenerator.ProcessQuery(parsedQuery);
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
}