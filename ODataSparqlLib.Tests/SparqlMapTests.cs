using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Annotations;
using Microsoft.Data.Edm.Expressions;
using Microsoft.Data.Edm.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ODataSparqlLib.Tests
{
    [TestClass]
    [DeploymentItem("dbpedia.metadata")]
    public class SparqlMapTests
    {
        [TestMethod]
        public void TestAnnotationAccess()
        {
            using (var edmxStream = new FileStream("dbpedia.metadata", FileMode.Open))
            {
                IEdmModel model;
                IEnumerable<EdmError> errors;
                Assert.IsTrue(Microsoft.Data.Edm.Csdl.EdmxReader.TryParse(new XmlTextReader(edmxStream), out model,
                                                                          out errors));
                var film = model.FindDeclaredType("DBPedia.Film");
                Assert.IsNotNull(film);
                var annotations = film.VocabularyAnnotations(model).ToList();
                Assert.AreEqual(1, annotations.Count);
                var annotation = annotations[0];
                Assert.IsInstanceOfType(annotation, typeof(IEdmValueAnnotation));
                Assert.IsFalse(annotation.IsBad());
                Assert.AreEqual("ODataSparqlLib.Annotations", annotation.Term.Namespace);
                Assert.AreEqual("Uri", annotation.Term.Name);
                Assert.AreEqual(EdmExpressionKind.StringConstant,
                                (annotation as IEdmValueAnnotation).Value.ExpressionKind);
                var stringExpression = ((annotation as IEdmValueAnnotation).Value as IEdmStringConstantExpression).Value;
                Assert.AreEqual("http://dbpedia.org/ontology/Film", stringExpression);
            }
        }

        [TestMethod]
        public void TestReadTypeUriAnnotation()
        {
            var map = new SparqlMap("dbpedia.metadata", "http://dbpedia.org/");
            Assert.AreEqual("http://dbpedia.org/ontology/Film", map.GetUriForType("DBPedia.Film"));
        }

        [TestMethod]
        public void TestReadPropertyUriAnnotation()
        {
            var map = new SparqlMap("dbpedia.metadata", "http://dbpedia.org/");
            Assert.AreEqual("http://dbpedia.org/property/name", map.GetUriForProperty("DBPedia.Film", "Name"));
        }

        [TestMethod]
        public void TestReadNavigationPropertyAnnotation()
        {
            var map = new SparqlMap("dbpedia.metadata", "http://dbpedia.org/");
            string propertyUri;
            bool isInverse;
            Assert.IsTrue(map.TryGetUriForNavigationProperty("DBPedia.Film", "Director", out propertyUri, out isInverse));
            Assert.IsFalse(isInverse);
            Assert.AreEqual("http://dbpedia.org/ontology/director", propertyUri);
        }
    }
}
