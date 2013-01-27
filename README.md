odata-sparql
============

This project is intended to provide a gateway interface that allows OData client applications to consume data from a SPARQL endpoint. It consists of four separate parts:

1) A fixed and repackaged version of the ODataLib code from codeplex. Primarily this has been done to allow this project to use the OData query parser and other core OData functionality from ODataLib and ODataLibContrib. The primary change made is to merge all the code from ODataLibContrib into ODataLib.

2) A library (ODataSparqlLib) for converting an OData query into a SPARQL query based on a mapping defined through annotations on an EDMX file; and for converting the resulting SPARQL results set into an OData feed. This library also contains the HTTP Handler and configuration components used to provide this functionality as a service through IIS.

3) Some basic unit tests for (2)

4) A sample web application configured up to provide access to a limited subset of data from DBPedia.
