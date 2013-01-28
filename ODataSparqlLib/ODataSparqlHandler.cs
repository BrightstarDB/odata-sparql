using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ODataSparqlLib
{
    public class ODataSparqlHandler : IHttpHandler
    {
        private readonly Dictionary<string, ODataSparqlServiceSettings> _contexts;
        private readonly object _contextLock = new object();
        public ODataSparqlHandler()
        {
            _contexts = new Dictionary<string, ODataSparqlServiceSettings>();
        }

        /// <summary>
        /// Processes an incoming HTTP request to the correct configured
        /// OData SPARQL service endpoint
        /// </summary>
        /// <param name="context"></param>
        public void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            var pathSegments = request.Path.Split('/');
            ODataSparqlServiceSettings serviceSettings = null;
            var baseUriBuilder = new StringBuilder();
            foreach (var pathSegment in pathSegments)
            {
                baseUriBuilder.Append(pathSegment);
                baseUriBuilder.Append('/');
                if (pathSegment.EndsWith(".sparql"))
                {
                    var serviceName = pathSegment.Substring(0, pathSegment.LastIndexOf('.'));
                    serviceSettings = AssertContext(serviceName, context);
                    break;
                }
            }
            
            var handler = new ODataSparqlRequestHandler(serviceSettings);
            handler.ProcessRequest(context, new Uri(context.Request.Url, baseUriBuilder.ToString()));
        }

        public bool IsReusable { get { return true; } }

        private ODataSparqlServiceSettings AssertContext(string name, HttpContext context)
        {
            lock (_contextLock)
            {
                ODataSparqlServiceSettings ret;
                if (!_contexts.TryGetValue(name, out ret))
                {
                    ret = new ODataSparqlServiceSettings(name, context.Server);
                    _contexts[name] = ret;
                }
                return ret;
            }
        }
    }
}
