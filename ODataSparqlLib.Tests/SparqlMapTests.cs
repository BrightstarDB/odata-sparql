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
                // All annotations includes one from the base class
                var annotations = film.VocabularyAnnotations(model).ToList();
                Assert.AreEqual(2, annotations.Count);
                // This query should return only direct annotations
                annotations = film.VocabularyAnnotations(model).Where(a => a.Target.Equals(film)).ToList();
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
            var map = new SparqlMap("dbpedia.metadata", "http://dbpedia.org/", NameMapping.Unchanged);
            Assert.AreEqual("http://dbpedia.org/ontology/Film", map.GetUriForType("DBPedia.Film"));
        }

        [TestMethod]
        public void TestReadPropertyUriAnnotation()
        {
            var map = new SparqlMap("dbpedia.metadata", 
                "http://dbpedia.org/ontology/", NameMapping.Unchanged,
                "http://dbpedia.org/property/name", NameMapping.LowerCamelCase);
            Assert.AreEqual("http://dbpedia.org/property/name", map.GetUriForProperty("DBPedia.Film", "Name"));
        }

        [TestMethod]
        public void TestReadNavigationPropertyAnnotation()
        {
            var map = new SparqlMap("dbpedia.metadata", 
                "http://dbpedia.org/ontology/", NameMapping.Unchanged,
                "http://dbpedia.org/property/", NameMapping.LowerCamelCase);
            string propertyUri;
            bool isInverse;
            Assert.IsTrue(map.TryGetUriForNavigationProperty("DBPedia.Film", "Director", out propertyUri, out isInverse));
            Assert.IsFalse(isInverse);
            Assert.AreEqual("http://dbpedia.org/ontology/director", propertyUri);
        }

        [TestMethod]
        public void TestMapDefaults()
        {
            var map = new SparqlMap("dbpedia.metadata",
                                    "http://dbpedia.org/ontology/", NameMapping.UpperCamelCase,
                                    "http://dbpedia.org/property/", NameMapping.LowerCamelCase);
            Assert.AreEqual("http://www.w3.org/2002/07/owl#Thing", map.GetUriForType("DBPedia.Thing"));
            Assert.AreEqual("http://dbpedia.org/ontology/Work", map.GetUriForType("DBPedia.Work"));
            Assert.AreEqual("http://dbpedia.org/ontology/Film", map.GetUriForType("DBPedia.Film"));
            Assert.AreEqual("http://dbpedia.org/ontology/Place", map.GetUriForType("DBPedia.Place"));
            Assert.AreEqual("http://dbpedia.org/property/elevation", map.GetUriForProperty("DBPedia.Place", "Elevation"));
            Assert.AreEqual("http://dbpedia.org/property/annualTemperature", map.GetUriForProperty("DBPedia.Place", "AnnualTemperature"));
        }
    }
}
