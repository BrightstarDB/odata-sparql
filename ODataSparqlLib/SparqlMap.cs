using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Annotations;
using Microsoft.Data.Edm.Csdl;
using Microsoft.Data.Edm.Expressions;
using Microsoft.Data.Edm.Validation;

namespace ODataSparqlLib
{
    public class SparqlMap
    {
        public const string AnnotationsNamespace = "ODataSparqlLib.Annotations";
        private readonly NameMapping _defaultPropertyNameMapping;
        private readonly string _defaultPropertyNamepsace;
        private readonly NameMapping _defaultTypeNameMapping;
        private readonly string _defaultTypeNamespace;
        private Dictionary<string, PropertyMapping> _propertyUriMap;
        private Dictionary<string, TypeMapping> _typeUriMap;

        public SparqlMap(
            string edmxPath,
            string defaultTypeNamespace, NameMapping typeNameMapping,
            string defaultPropertyNamespace = null, NameMapping? propertyNameMapping = null)
        {
            _defaultTypeNamespace = defaultTypeNamespace;
            _defaultTypeNameMapping = typeNameMapping;
            _defaultPropertyNamepsace = defaultPropertyNamespace ?? defaultTypeNamespace;
            _defaultPropertyNameMapping = propertyNameMapping.HasValue ? propertyNameMapping.Value : typeNameMapping;

            using (var edmxStream = new FileStream(edmxPath, FileMode.Open))
            {
                IEdmModel model;
                IEnumerable<EdmError> errors;
                if (EdmxReader.TryParse(new XmlTextReader(edmxStream), out model, out errors))
                {
                    ReadModel(model);
                }
                else
                {
                    throw new Exception("Error parsing metadata");
                }
            }
        }

        public IEdmModel Model { get; private set; }

        private void ReadModel(IEdmModel metadata)
        {
            Model = metadata;
            _typeUriMap = new Dictionary<string, TypeMapping>();
            _propertyUriMap = new Dictionary<string, PropertyMapping>();
            foreach (IEdmSchemaElement schemaElement in Model.SchemaElements)
            {
                if (schemaElement is IEdmEntityType)
                {
                    ReadEntityType(schemaElement as IEdmEntityType);
                }
            }
        }

        private void ReadEntityType(IEdmEntityType entityType)
        {
            if (IsIgnored(entityType)) return;
            var typeUri = GetUriMapping(entityType);
            
            var keyList = entityType.DeclaredKey.ToList();
            if (keyList.Count != 1)
            {
                // Ignore this entity
                // TODO: Log an error
                return;
            }
            string identifierPrefix = GetStringAnnotationValue(keyList.First(), AnnotationsNamespace, "IdentifierPrefix");

            _typeUriMap[entityType.FullName()] = new TypeMapping
                {
                    Uri = typeUri,
                    IdentifierPrefix = identifierPrefix
                };
            foreach (IEdmProperty property in entityType.Properties())
            {
                ReadProperty(entityType, property);
            }
        }

        private class TypeMapping
        {
            public string Uri { get; set; }
            public string IdentifierPrefix { get; set; }
        }

        private void ReadProperty(IEdmEntityType entityType, IEdmProperty property)
        {
            string declaredPropertyName;
            string entityPropertyName = entityType.FullName() + "." + property.Name;
            if (property.DeclaringType is IEdmEntityType)
            {
                declaredPropertyName = (property.DeclaringType as IEdmEntityType).FullName() + "." + property.Name;
            }
            else
            {
                declaredPropertyName = entityPropertyName;
            }

            PropertyMapping mapping;
            if (_propertyUriMap.TryGetValue(declaredPropertyName, out mapping))
            {
                _propertyUriMap[entityPropertyName] = mapping;
            }
            else
            {
                mapping = new PropertyMapping
                    {
                        Uri = GetUriMapping(property),
                        IsInverse = GetBooleanAnnotationValue(property, AnnotationsNamespace, "IsInverse", false),
                        IdentifierPrefix = GetStringAnnotationValue(property, AnnotationsNamespace, "IdentifierPrefix")
                    };

                _propertyUriMap[entityPropertyName] = mapping;
                if (!declaredPropertyName.Equals(entityPropertyName))
                {
                    _propertyUriMap[declaredPropertyName] = mapping;
                }
            }
        }


        private string GetUriMapping(IEdmEntityType entityType)
        {
            string ret = GetStringAnnotationValue(entityType, AnnotationsNamespace, "Uri");
            return String.IsNullOrEmpty(ret)
                       ? ApplyNameMapping(_defaultTypeNameMapping, _defaultTypeNamespace, entityType.Name)
                       : ret;
        }

        private string GetUriMapping(IEdmProperty property)
        {
            string ret = GetStringAnnotationValue(property, AnnotationsNamespace, "Uri");
            return String.IsNullOrEmpty(ret)
                       ? ApplyNameMapping(_defaultPropertyNameMapping, _defaultPropertyNamepsace, property.Name)
                       : ret;
        }

        private static string ApplyNameMapping(NameMapping nameMappingKind, string mapNamespace, string name)
        {
            string mappedName = name;
            switch (nameMappingKind)
            {
                case NameMapping.LowerCase:
                    mappedName = name.ToLower();
                    break;
                case NameMapping.UpperCase:
                    mappedName = name.ToUpper();
                    break;
                case NameMapping.LowerCamelCase:
                    mappedName = Char.ToLower(name[0]) + name.Substring(1);
                    break;
                case NameMapping.UpperCamelCase:
                    mappedName = Char.ToUpper(name[0]) + name.Substring(1);
                    break;
            }
            return mapNamespace + mappedName;
        }


        private bool IsIgnored(IEdmVocabularyAnnotatable annotatable)
        {
            if (
                annotatable.VocabularyAnnotations(Model)
                           .OfType<IEdmValueAnnotation>()
                           .Any(
                               va =>
                               va.Term.Namespace.Equals(AnnotationsNamespace) &&
                               va.Term.Name.Equals("Ignore") &&
                               va.Value.ExpressionKind == EdmExpressionKind.BooleanConstant &&
                               (va.Value as IEdmBooleanConstantExpression).Value))
            {
                return true;
            }
            return false;
        }

        public string GetUriForType(string qualifiedName)
        {
            TypeMapping mapping;
            if (_typeUriMap.TryGetValue(qualifiedName, out mapping))
            {
                return mapping.Uri;
            }
            return null;
        }

        private string GetStringAnnotationValue(IEdmVocabularyAnnotatable annotatable, string annotationNamespace,
                                                string annotationName)
        {
            IEdmValueAnnotation annotation = annotatable.VocabularyAnnotations(Model).
                                                     OfType<IEdmValueAnnotation>().
                                                     FirstOrDefault(
                                                         a =>
                                                         a.Term.Namespace.Equals(annotationNamespace) &&
                                                         a.Term.Name.Equals(annotationName) &&
                                                         a.Value.ExpressionKind == EdmExpressionKind.StringConstant);
            return annotation != null ? (annotation.Value as IEdmStringConstantExpression).Value : null;
        }

        private bool? GetBooleanAnnotationValue(IEdmVocabularyAnnotatable annotatable,
                                                 string annotationNamespace, string annotaionName)
        {
            var annotation = annotatable.VocabularyAnnotations(Model)
                                        .OfType<IEdmValueAnnotation>()
                                        .FirstOrDefault(
                                            a => a.Value.ExpressionKind == EdmExpressionKind.BooleanConstant &&
                                                 a.Term.Namespace.Equals(annotationNamespace) &&
                                                 a.Term.Name.Equals(annotaionName));
            return annotation != null
                       ? (annotation.Value as IEdmBooleanConstantExpression).Value
                       : (bool?)null;
        }

        private bool GetBooleanAnnotationValue(IEdmVocabularyAnnotatable annotatable,
                                               string annotationNamespace, string annotationName, bool defaultValue)
        {
            var ret = GetBooleanAnnotationValue(annotatable, annotationNamespace, annotationName);
            return ret.HasValue ? ret.Value : defaultValue;
        }

        public string GetUriForProperty(string qualifiedTypeName, string propertyName)
        {
            string key = qualifiedTypeName + "." + propertyName;
            PropertyMapping mapping;
            return _propertyUriMap.TryGetValue(key, out mapping) ? mapping.Uri : null;
        }


        public bool TryGetUriForNavigationProperty(string qualifiedTypeName, string propertyName, out string propertyUri,
                                                   out bool isInverse)
        {
            string key = qualifiedTypeName + "." + propertyName;
            PropertyMapping mapping;
            if (_propertyUriMap.TryGetValue(key, out mapping))
            {
                propertyUri = mapping.Uri;
                isInverse = mapping.IsInverse;
                return true;
            }
            propertyUri = null;
            isInverse = false;
            return false;
        }

        public bool TryGetIdentifierPrefixForProperty(string qualifiedTypeName, string propertyName,
                                                      out string identifierPrefix)
        {
            string key = qualifiedTypeName + "." + propertyName;
            PropertyMapping mapping;
            if (_propertyUriMap.TryGetValue(key, out mapping) &&
                !String.IsNullOrEmpty(mapping.IdentifierPrefix))
            {
                identifierPrefix = mapping.IdentifierPrefix;
                return true;
            }
            identifierPrefix = null;
            return false;
        }

        public string GetResourceUriPrefix(string qualifiedTypeName)
        {
            TypeMapping mapping;
            if (_typeUriMap.TryGetValue(qualifiedTypeName, out mapping))
            {
                return mapping.IdentifierPrefix;
            }
            return null;
        }

        public string GetTypeSet(string qualifiedTypeName)
        {
            var edmType = Model.FindDeclaredType(qualifiedTypeName) as IEdmEntityType;
            if (edmType == null)
            {
                throw new Exception("Cannot find metadata for type " + qualifiedTypeName);
            }
            IEdmEntitySet entitySet = Model.EntityContainers()
                                           .SelectMany(
                                               ec =>
                                               ec.EntitySets()
                                                 .Where(es => es.ElementType.FullName().Equals(qualifiedTypeName)))
                                           .FirstOrDefault();
            if (entitySet == null)
            {
                throw new Exception("Cannot find entity set for type " + qualifiedTypeName);
            }
            return entitySet.Name;
        }

        public IEnumerable<PropertyInfo> GetStructuralPropertyMappings(string qualifiedTypeName)
        {
            var edmType = AssertEntityType(qualifiedTypeName);
            foreach (var structuralProperty in edmType.StructuralProperties())
            {
                PropertyMapping propertyMapping;
                if (_propertyUriMap.TryGetValue(qualifiedTypeName + "." + structuralProperty.Name, out propertyMapping))
                {
                    yield return new PropertyInfo
                        {
                            Name = structuralProperty.Name,
                            PropertyType = structuralProperty.Type,
                            Uri = propertyMapping.Uri
                        };
                }
            }
        }

        public IEnumerable<PropertyInfo> GetAssociationPropertyMappings(string qualifiedTypeName)
        {
            var edmType = AssertEntityType(qualifiedTypeName);
            foreach (var navigationProperty in edmType.NavigationProperties())
            {
                PropertyMapping propertyMapping;
                if (_propertyUriMap.TryGetValue(qualifiedTypeName +"."+ navigationProperty.Name, out propertyMapping))
                {
                    yield return new PropertyInfo
                        {
                            Name = navigationProperty.Name,
                            Uri = propertyMapping.Uri,
                            IsInverse = propertyMapping.IsInverse
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

        private class PropertyMapping
        {
            public string Uri { get; set; }
            public string IdentifierPrefix { get; set; }
            public bool IsInverse { get; set; }
        }
    }
}