odata-sparql
============

This project is intended to provide a gateway interface that allows OData client applications to consume data from a SPARQL endpoint. It consists of four separate parts:

1) A fixed and repackaged version of the ODataLib code from codeplex. Primarily this has been done to allow this project to use the OData query parser and other core OData functionality from ODataLib and ODataLibContrib. The primary change made is to merge all the code from ODataLibContrib into ODataLib.

2) A library (ODataSparqlLib) for converting an OData query into a SPARQL query based on a mapping defined through annotations on an EDMX file; and for converting the resulting SPARQL results set into an OData feed. This library also contains the HTTP Handler and configuration components used to provide this functionality as a service through IIS.

3) Some basic unit tests for (2)

4) A sample web application configured up to provide access to a limited subset of data from DBPedia.

Current Status
--------------

The following is currently implemented
* Addressing entity sets : e.g. http://example.org/odata/Films
* Addressing entity by key predicate e.g. http://example.org/odata/Films('someid')
* Navigating a property to a single entity. e.g. http://example.org/odata/Persons/BirthPlace
* $top and $skip
* $orderby
* $filter
  * all operators are implemented except for Mod which has no functional equivalent in SPARQL
  * all functions are implemented except for:
    * indexof() which also has no functional equivalent in SPARQL
	* isof() which is currently unsupported by the OData parser library

The following is currently not implemented
* Navigating a property to a collection of entities
* $expand (not supported in the OData parser library)
* $select (not supported in the OData parser library)
* $inlinecount
