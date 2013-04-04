using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ODataSparqlLib.Tests
{
    [TestClass]
    public class PatternItemTests
    {
        [TestMethod]
        public void TestUriEscaping()
        {
            var unencodedUris = new[]
                {
                    "http://dbpedia.org/resource/Luis_Buñuel",
                };
            var sparqlValues = new[]
                {
                    "<http://dbpedia.org/resource/Luis_Bu%C3%B1uel>"
                };

            for (int i = 0; i < unencodedUris.Length; i++)
            {
                var patternItem = new UriPatternItem(unencodedUris[i]);
                Assert.AreEqual(sparqlValues[i], patternItem.SparqlRepresentation);
            }
        }
    }
}
