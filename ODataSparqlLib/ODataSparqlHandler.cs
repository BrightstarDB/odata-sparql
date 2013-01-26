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
        private readonly Dictionary<string, ODataSparqlServiceConfiguration> _contexts;
        private readonly object _contextLock = new object();
        public ODataSparqlHandler()
        {
            _contexts = new Dictionary<string, ODataSparqlServiceConfiguration>();
        }

        public void ProcessRequest(HttpContext context)
        {
            HttpRequest request = context.Request;
            var pathSegments = request.Path.Split('/');
            ODataSparqlServiceConfiguration serviceConfiguration = null;
            var baseUriBuilder = new StringBuilder();
            foreach (var pathSegment in pathSegments)
            {
                baseUriBuilder.Append(pathSegment);
                baseUriBuilder.Append('/');
                if (pathSegment.EndsWith(".sparql"))
                {
                    var serviceName = pathSegment.Substring(0, pathSegment.LastIndexOf('.'));
                    serviceConfiguration = AssertContext(serviceName, context);
                    break;
                }
            }
            
            var handler = new ODataSparqlRequestHandler(serviceConfiguration);
            handler.ProcessRequest(context, new Uri(context.Request.Url, baseUriBuilder.ToString()));
        }

        public bool IsReusable { get { return true; } }

        private ODataSparqlServiceConfiguration AssertContext(string name, HttpContext context)
        {
            lock (_contextLock)
            {
                ODataSparqlServiceConfiguration ret;
                if (!_contexts.TryGetValue(name, out ret))
                {
                    ret = new ODataSparqlServiceConfiguration(name, context.Server);
                    _contexts[name] = ret;
                }
                return ret;
            }
        }
    }
}
