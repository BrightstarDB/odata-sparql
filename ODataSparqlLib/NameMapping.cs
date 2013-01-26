namespace ODataSparqlLib
{
    /// <summary>
    /// An enumeration specifying the different ways of turning an OData 
    /// entity / property name into an RDF identifier
    /// </summary>
    public enum NameMapping
    {
        /// <summary>
        /// Use the name exactly as written
        /// </summary>
        Unchanged,
        /// <summary>
        /// Use the name as written but with the first character forced to lower case
        /// </summary>
        LowerCamelCase,
        /// <summary>
        /// Use the name as written but with the first character forced to upper case
        /// </summary>
        UpperCamelCase,
        /// <summary>
        /// Use the name forced entirely to lower case
        /// </summary>
        LowerCase,
        /// <summary>
        /// Use the name forced entirely to upper case
        /// </summary>
        UpperCase,
    }
}
