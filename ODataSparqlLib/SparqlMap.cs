using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Annotations;
using Microsoft.Data.Edm.Expressions;
using Microsoft.Data.Edm.Library.Annotations;
using Microsoft.Data.Edm.Validation;

namespace ODataSparqlLib
{
    public class SparqlMap
    {
        private string _defaultNamespace;
        public IEdmModel Model { get; private set; }

        public SparqlMap(string edmxPath, string defaultNamespace)
        {
            using (var edmxStream = new FileStream(edmxPath, FileMode.Open))
            {
                IEdmModel model;
                IEnumerable<EdmError> errors;
                if (Microsoft.Data.Edm.Csdl.EdmxReader.TryParse(new XmlTextReader(edmxStream), out model, out errors))
                {
                    Initialize(model, defaultNamespace);
                }
                else
                {
                    throw new Exception("Error parsing metadata");
                }
            }
        }

        private void Initialize(IEdmModel metadata, string defaultNamespace)
        {
            Model = metadata;
            _defaultNamespace = defaultNamespace;
        }

        public string GetUriForType(string qualifiedName)
        {
            string typeUri = null;
            var edmType = Model.FindDeclaredType(qualifiedName);
            if (edmType != null)
            {
                typeUri = GetUriAnnotation(edmType);
            }
            return typeUri ?? MakeUriFromQualifiedName(qualifiedName);
        }

        private string GetStringAnnotationValue(IEdmVocabularyAnnotatable edmType, string annotationNamespace, string annotationName)
        {
            var uriAnnotation = edmType.VocabularyAnnotations(Model).
                                            OfType<IEdmValueAnnotation>().
                                            FirstOrDefault(
                                                a =>
                                                a.Term.Namespace.Equals(annotationNamespace) &&
                                                a.Term.Name.Equals(annotationName) &&
                                                a.Value.ExpressionKind == EdmExpressionKind.StringConstant);
            if (uriAnnotation != null)
            {
                return (uriAnnotation.Value as IEdmStringConstantExpression).Value;
            }
            return null;
            
        }
        private string GetUriAnnotation(IEdmVocabularyAnnotatable edmType)
        {
            var uriAnnotation = edmType.VocabularyAnnotations(Model).
                                            OfType<IEdmValueAnnotation>().
                                            FirstOrDefault(
                                                a =>
                                                a.Term.Namespace.Equals("ODataSparqlLib.Annotations") &&
                                                a.Term.Name.Equals("Uri") &&
                                                a.Value.ExpressionKind == EdmExpressionKind.StringConstant);
            if (uriAnnotation != null)
            {
                return (uriAnnotation.Value as IEdmStringConstantExpression).Value;
            }
            return null;
        }

        public string GetUriForProperty(string qualifiedTypeName, string propertyName)
        {
            string propertyUri = null;
            var edmType = Model.FindDeclaredType(qualifiedTypeName) as IEdmEntityType;
            if (edmType != null)
            {
                var edmProperty = edmType.FindProperty(propertyName);
                if (edmProperty != null)
                {
                    propertyUri = GetUriAnnotation(edmProperty);
                }
            }
            return propertyUri ?? _defaultNamespace + propertyName;
        }

        private string MakeUriFromQualifiedName(string qualifiedName)
        {
            if (String.IsNullOrEmpty(qualifiedName))
            {
                throw new ArgumentException("Parameter must not be NULL or an empty string", "qualifiedName");
            }
            // Might be good to make this logic pluggable
            if (qualifiedName.Contains("."))
            {
                string name = qualifiedName.Substring(qualifiedName.LastIndexOf('.') + 1);
                return _defaultNamespace + name;
            }
            return _defaultNamespace + qualifiedName;
        }

        public bool TryGetUriForNavigationProperty(string qualifiedTypeName, string propertyName, out string propertyUri, out bool isInverse)
        {
            var edmType = Model.FindDeclaredType(qualifiedTypeName) as IEdmEntityType;
            if (edmType != null)
            {
                var edmProperty = edmType.FindProperty(propertyName);
                if (edmProperty.PropertyKind == EdmPropertyKind.Navigation)
                {
                    propertyUri= GetStringAnnotationValue(edmProperty, "ODataSparqlLib.Annotations", "Property");
                    if (!String.IsNullOrEmpty(propertyUri))
                    {
                        isInverse = false;
                        return true;
                    }
                    propertyUri = GetStringAnnotationValue(edmProperty, "ODataSparqlLib.Annotations",
                                                           "InverseProperty");
                    if (!String.IsNullOrEmpty(propertyUri))
                    {
                        isInverse = true;
                        return true;
                    }
                }
            }
            propertyUri = null;
            isInverse = false;
            return false;
        }

        public bool TryGetIdentifierPrefixForProperty(string qualifiedTypeName, string propertyName,
                                                   out string identifierPrefix)
        {
            var edmType = Model.FindDeclaredType(qualifiedTypeName) as IEdmEntityType;
            if (edmType != null)
            {
                var edmProperty = edmType.FindProperty(propertyName);
                if (edmProperty != null && edmProperty.PropertyKind == EdmPropertyKind.Structural)
                {
                    identifierPrefix = GetStringAnnotationValue(edmProperty, "ODataSparqlLib.Annotations",
                                                                "IdentifierPrefix");
                    if (!string.IsNullOrEmpty(identifierPrefix))
                    {
                        return true;
                    }
                }
            }
            identifierPrefix = null;
            return false;
        }

        public string GetResourceUriPrefix(string qualifiedTypeName)
        {
            var edmType = Model.FindDeclaredType(qualifiedTypeName) as IEdmEntityType;
            if (edmType == null)
            {
                throw new Exception("Cannot find metadata for type " + qualifiedTypeName);
            }
            var keyProperties = edmType.DeclaredKey.ToList();
            if (keyProperties.Count != 1)
            {
                throw new Exception("Cannot currently handle entities with composite keys");
            }
            var identifierPrefix = GetStringAnnotationValue(keyProperties.First(), "ODataSparqlLib.Annotations",
                                                                "IdentifierPrefix");
            if (String.IsNullOrEmpty(identifierPrefix))
            {
                throw new Exception("No IdentifierPrefix declaration found on key property.");
            }
            return identifierPrefix;
        }

        public string GetTypeSet(string qualifiedTypeName)
        {
            var edmType = Model.FindDeclaredType(qualifiedTypeName) as IEdmEntityType;
            if (edmType == null)
            {
                throw new Exception("Cannot find metadata for type " + qualifiedTypeName);
            }
            var entitySet = Model.EntityContainers()
                  .SelectMany(ec => ec.EntitySets().Where(es => es.ElementType.FullName().Equals(qualifiedTypeName)))
                  .FirstOrDefault();
            if (entitySet == null)
            {
                throw new Exception("Cannot find entity set for type " + qualifiedTypeName);
            }
            return entitySet.Name;
        }

        public IEnumerable<PropertyMapping> GetStructuralPropertyMappings(string qualifiedTypeName)
        {
            var edmType = AssertEntityType(qualifiedTypeName);
            return from structuralProperty in edmType.StructuralProperties() let propertyTypeUri = GetUriForProperty(qualifiedTypeName, structuralProperty.Name) select new PropertyMapping
                {
                    Name = structuralProperty.Name,
                    Uri = propertyTypeUri,
                    PropertyType = structuralProperty.Type
                };
        }

        public IEnumerable<PropertyMapping> GetAssociationPropertyMappings(string qualifiedTypeName)
        {
            var edmType = AssertEntityType(qualifiedTypeName);
            foreach (var navigationProperty in edmType.NavigationProperties())
            {
                string propertyUri;
                bool isInverse;
                if (TryGetUriForNavigationProperty(qualifiedTypeName, navigationProperty.Name, out propertyUri,
                                                   out isInverse))
                {
                    yield return new PropertyMapping
                        {
                            Name = navigationProperty.Name,
                            Uri = propertyUri
                        };
                }
            }
        } 

        private IEdmEntityType AssertEntityType(string qualifiedTypeName)
        {
            var edmType = Model.FindDeclaredType(qualifiedTypeName) as IEdmEntityType;
            if (edmType == null)
            {
                throw new Exception("Cannot find metadata for type " + qualifiedTypeName);
            }
            return edmType;
        }
    }

    public class PropertyMapping
    {
        public string Name { get; set; }
        public string Uri { get; set; }
        public IEdmTypeReference PropertyType { get; set; }
    }
}
