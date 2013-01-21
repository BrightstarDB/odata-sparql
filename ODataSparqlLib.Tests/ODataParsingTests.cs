using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Validation;
using Microsoft.Data.OData.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ODataSparqlLib.Tests
{
    [TestClass]
    [DeploymentItem("dbpedia.metadata")]
    public class ODataParsingTests
    {
        private readonly IEdmModel _dbpediaModel;
        public ODataParsingTests()
        {
            using (var edmxStream = System.IO.File.OpenRead("dbpedia.metadata"))
            {
                IEnumerable<EdmError> errors;
                Microsoft.Data.Edm.Csdl.EdmxReader.TryParse(new XmlTextReader(edmxStream), out _dbpediaModel,
                                                            out errors);
            }
        }

        [TestMethod]
        public void TestParseTypeSetAccess()
        {
            var parseTree = QueryDescriptorQueryToken.ParseUri(new Uri("http://example.org/odata/Films"), new Uri("http://example.org/odata/"));
            Assert.IsNotNull(parseTree);
            Assert.AreEqual(QueryTokenKind.Segment, parseTree.Path.Kind);
            var segment = parseTree.Path as SegmentQueryToken;
            Assert.AreEqual("Films", segment.Name);
            Assert.IsNull(segment.NamedValues);
        }

        [TestMethod]
        public void TestParseInstanceAccess()
        {
            var parseTree = QueryDescriptorQueryToken.ParseUri(
                new Uri("http://example.org/odata/Films('Un_Chien_Andalou')"),
                new Uri("http://example.org/odata/"));
            Assert.IsNotNull(parseTree);
            Assert.IsNotNull(parseTree.Path);
            Assert.AreEqual(QueryTokenKind.Segment, parseTree.Path.Kind);
            Assert.IsInstanceOfType(parseTree.Path, typeof(SegmentQueryToken));
            var sqt = parseTree.Path as SegmentQueryToken;
            Assert.AreEqual("Films", sqt.Name);
            Assert.IsNotNull(sqt.NamedValues);
            var namedValues = sqt.NamedValues.ToList();
            Assert.AreEqual(1, namedValues.Count);
            Assert.IsNull(namedValues[0].Name);
            Assert.IsNotNull(namedValues[0].Value);
            Assert.IsNotNull(namedValues[0].Value.Value);
            Assert.AreEqual(QueryTokenKind.Literal, namedValues[0].Value.Kind);
            Assert.AreEqual("Un_Chien_Andalou", namedValues[0].Value.Value);
        }

        [TestMethod]
        public void TestBindTypeSetAccess()
        {
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri("http://example.org/odata/Films"),
                new Uri("http://example.org/odata/"), _dbpediaModel);
            Assert.IsNotNull(queryDescriptor);
            Assert.IsNotNull(queryDescriptor.Query);
            Assert.AreEqual(QueryNodeKind.EntitySet, queryDescriptor.Query.Kind);
            var entitySetQueryNode = queryDescriptor.Query as EntitySetQueryNode;
            Assert.IsNotNull(entitySetQueryNode);
            Assert.AreEqual("Films",entitySetQueryNode.EntitySet.Name);
            Assert.AreEqual("DBPedia.Film", entitySetQueryNode.ItemType.FullName());
        }

        [TestMethod]
        public void TestBindInstanceAccess()
        {
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri("http://example.org/odata/Films('Un_Chien_Andalou')"),
                new Uri("http://example.org/odata/"),
                _dbpediaModel);
            Assert.IsNotNull(queryDescriptor);
            Assert.IsNotNull(queryDescriptor.Query);
            Assert.AreEqual(QueryNodeKind.KeyLookup, queryDescriptor.Query.Kind);
        }
    }
}
