using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Validation;
using Microsoft.Data.OData.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ODataSparqlLib.Tests
{
    [TestClass]
    [DeploymentItem("dbpedia.metadata")]
    public class SparqlGeneratorTests
    {
        private readonly IEdmModel _dbpediaModel;
        private SparqlMap _dbpediaMap;
        public SparqlGeneratorTests()
        {
            using (var edmxStream = System.IO.File.OpenRead("dbpedia.metadata"))
            {
                IEnumerable<EdmError> errors;
                Microsoft.Data.Edm.Csdl.EdmxReader.TryParse(new XmlTextReader(edmxStream), out _dbpediaModel,
                                                            out errors);
            }
            _dbpediaMap = new SparqlMap("dbpedia.metadata", "http://dbpedia.org/", NameMapping.Unchanged);
        }

        [TestMethod]
        public void TestEntitySetAccess()
        {
            var dbpediaGenerator= new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                            new Uri("http://example.org/odata/Films"),
                            new Uri("http://example.org/odata/"), _dbpediaModel); 
            dbpediaGenerator.ProcessQuery(queryDescriptor);
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel);
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel.RootGraphPattern);
            Assert.AreEqual(1, dbpediaGenerator.SparqlQueryModel.RootGraphPattern.TriplePatterns.Count);
            var tp = dbpediaGenerator.SparqlQueryModel.RootGraphPattern.TriplePatterns[0];
            Assert.AreEqual("?v1 <http://www.w3.org/1999/02/22-rdf-syntax-ns#type> <http://dbpedia.org/ontology/Film>",
                tp.GetSparqlRepresentation());
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel.SelectVariables);
            Assert.AreEqual(1, dbpediaGenerator.SparqlQueryModel.SelectVariables.Count);
            Assert.IsTrue(dbpediaGenerator.SparqlQueryModel.SelectVariables.Contains("v1"));
            Assert.IsTrue(dbpediaGenerator.SparqlQueryModel.IsDescribe);
            Assert.AreEqual("DBPedia.Film", dbpediaGenerator.SparqlQueryModel.GetEntityType("v1"));
        }

        [TestMethod]
        public void TestInstanceAccess()
        {
            var dbpediaGenerator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                            new Uri("http://example.org/odata/Films('Un_Chien_Andalou')"),
                            new Uri("http://example.org/odata/"), _dbpediaModel);
            dbpediaGenerator.ProcessQuery(queryDescriptor);
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel);
            Assert.IsNotNull(dbpediaGenerator.SparqlQueryModel.DescribeResource);
            Assert.AreEqual("http://dbpedia.org/resource/Un_Chien_Andalou", dbpediaGenerator.SparqlQueryModel.DescribeResource);
            Assert.AreEqual("DBPedia.Film", dbpediaGenerator.SparqlQueryModel.GetEntityType("http://dbpedia.org/resource/Un_Chien_Andalou"));
        }

        [TestMethod]
        public void TestTop()
        {
            var generator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri("http://example.org/odata/Films?$top=2"),
                new Uri("http://example.org/odata/"), _dbpediaModel);
            generator.ProcessQuery(queryDescriptor);
            var sparql = generator.SparqlQueryModel;
            Assert.IsNotNull(sparql);
            Assert.IsTrue(sparql.IsDescribe);
            Assert.AreEqual(2, sparql.Limit);
        }

        [TestMethod]
        public void TestSkip()
        {
            var generator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri("http://example.org/odata/Films?$skip=5"),
                new Uri("http://example.org/odata/"), _dbpediaModel);
            generator.ProcessQuery(queryDescriptor);
            var sparql = generator.SparqlQueryModel;
            Assert.IsNotNull(sparql);
            Assert.IsTrue(sparql.IsDescribe);
            Assert.AreEqual(5, sparql.Offset);
        }

        [TestMethod]
        public void TestSkipAndTop()
        {
            var generator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri("http://example.org/odata/Films?$skip=50&$top=10"),
                new Uri("http://example.org/odata/"),
                _dbpediaModel);
            generator.ProcessQuery(queryDescriptor);
            var sparql = generator.SparqlQueryModel;
            Assert.IsNotNull(sparql);
            Assert.IsTrue(sparql.IsDescribe);
            Assert.AreEqual(1, sparql.RootGraphPattern.TriplePatterns.Count);
            Assert.AreEqual(10, sparql.Limit);
            Assert.AreEqual(50, sparql.Offset);
        }

        [TestMethod]
        public void TestOrderByProperty()
        {
            var generator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri("http://example.org/odata/Places?$orderby=PopulationTotal&$top=20"),
                new Uri("http://example.org/odata/"), _dbpediaModel);
            generator.ProcessQuery(queryDescriptor);
            var sparql = generator.SparqlQueryModel;
            Assert.IsNotNull(sparql);
            Assert.IsTrue(sparql.IsDescribe);
            Assert.AreEqual(20, sparql.Limit);
            Assert.IsNotNull(sparql.Ordering);
            Assert.IsTrue(sparql.Ordering.IsSimple);
            Assert.AreEqual("v2", sparql.Ordering.Variable);
            Assert.IsNull(sparql.Ordering.ThenBy);
        }

        [TestMethod]
        public void TestGtOperatorNumber()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=PopulationTotal gt 1000000");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("?v2 > 1000000", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestLtOperatorNumber()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=PopulationTotal lt 50");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("?v2 < 50", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestEqOperatorNumber()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=PopulationTotal eq 500");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("?v2 = 500", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestNeOperatorNumber()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=PopulationTotal ne 500");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("?v2 != 500", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestLeOperatorNumber()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=PopulationTotal le 500");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("?v2 <= 500", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestGeOperatorNumber()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=PopulationTotal ge 500");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("?v2 >= 500", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestLogicalAnd()
        {
            var sparql =
                ProcessQuery("http://example.org/odata/Places?$filter=PopulationTotal gt 1000000 and Elevation gt 500.0m");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("(?v2 > 1000000) && (?v3 > 500.0)", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestLogicalOr()
        {
            var sparql =
                ProcessQuery("http://example.org/odata/Places?$filter=PopulationTotal gt 1000000 or Elevation gt 500.0m");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("(?v2 > 1000000) || (?v3 > 500.0)", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestLogicalNot()
        {
            var sparql =
                ProcessQuery("http://example.org/odata/Places?$filter=not (PopulationTotal gt 1000000 or Elevation gt 500.0m)");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("!((?v2 > 1000000) || (?v3 > 500.0))", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestAddition()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=AnnualTemperature add 32 gt 80");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("(?v2 + 32) > 80", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestSubtraction()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=AnnualTemperature sub 32 gt 30");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("(?v2 - 32) > 30", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestMultiplication()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=AnnualTemperature mul 9 gt 80");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("(?v2 * 9) > 80", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestDivision()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=AnnualTemperature div 5 gt 80");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("(?v2 / 5) > 80", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestCombinedArithmeticOperators()
        {
            var sparql = ProcessQuery("http://example.org/odata/Places?$filter=AnnualTemperature mul (9 div 5) add 32 gt 80");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("((?v2 * (9 / 5)) + 32) > 80", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestFnSubstringOf()
        {
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=substringof('foo', Name) eq true");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("contains(?v2, 'foo') = true", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestFnEndsWith()
        {
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=endswith(Name, 'foo') eq true");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("strends(?v2, 'foo') = true", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestFnStartsWith()
        {
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=startswith(Name, 'foo') eq true");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("strstarts(?v2, 'foo') = true", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestFnLength()
        {
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=length(Name) gt 19");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("strlen(?v2) > 19", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestFnIndexOf()
        {
            // No equivalent to indexof in SPARQL
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=indexof(Name, 'foo') gt 19");
        }

        [TestMethod]
        public void TestFnSubstring()
        {
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=substring(Name, 1) eq 'foo'");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("substr(?v2, 1) = 'foo'", sparql.RootGraphPattern.FilterExpressions[0]);

            sparql = ProcessQuery("http://example.org/odata/Films?$filter=substring(Name, 1, 3) eq 'foo'");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("substr(?v2, 1, 3) = 'foo'", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestFnToLower()
        {
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=tolower(Name) eq 'foo'");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("lcase(?v2) = 'foo'", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestFnToUpper()
        {
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=toupper(Name) eq 'foo'");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual("ucase(?v2) = 'foo'", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestFnTrim()
        {
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=trim(Name) eq 'foo'");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual(@"replace(?v2, '^\s+|\s+$', '') = 'foo'", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        [TestMethod]
        public void TestFnConcat()
        {
            var sparql = ProcessQuery("http://example.org/odata/Films?$filter=concat(Name, 'bar') eq 'foobar'");
            Assert.IsNotNull(sparql);
            Console.WriteLine(sparql.GetSparqlRepresentation());
            Assert.AreEqual(1, sparql.RootGraphPattern.FilterExpressions.Count);
            Assert.AreEqual(@"concat(?v2, 'bar') = 'foobar'", sparql.RootGraphPattern.FilterExpressions[0]);
        }

        private SparqlModel ProcessQuery(string odataQuery)
        {
            var generator = new SparqlGenerator(_dbpediaMap);
            var queryDescriptor = QueryDescriptorQueryNode.ParseUri(
                new Uri(odataQuery),
                new Uri("http://example.org/odata/"), _dbpediaModel);
            generator.ProcessQuery(queryDescriptor);
            return generator.SparqlQueryModel;
        }
    }
}
