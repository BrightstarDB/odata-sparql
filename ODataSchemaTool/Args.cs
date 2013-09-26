using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace ODataSchemaTool
{
    internal class Args
    {
        [Argument(ArgumentType.AtMostOnce, DefaultValue = "schema.edmx",
            HelpText = "Specifies the name of the output EDMX file", LongName = "output", ShortName = "o")] 
        public string Output = "schema.edmx";

        [Argument(ArgumentType.MultipleUnique, DefaultValue = new string[]{"en"},
            HelpText = "List of preferred languages in preference order",
            LongName = "language", ShortName = "l")] 
        public string[] PreferredLanguage = new string[]{"en"};

        [Argument(ArgumentType.AtMostOnce, HelpText = "Default resource URI prefix", LongName = "resourcePrefix",
            ShortName = "rp")] 
        public string DefaultResourcePrefix;

        [Argument(ArgumentType.MultipleUnique, HelpText = "Resource type URI to resource URI prefix mapping. Each instance of this argument should be in the form {resource_type_uri}=>{resource_prefix_uri}",
            LongName = "resourcePrefixMap", ShortName = "rpm")] 
        public string[] ResourcePrefixMap;

        [Argument(ArgumentType.AtMostOnce, HelpText = "Default resource identifier property name",
            LongName = "identifierProperty", ShortName = "id", DefaultValue = "Id")] public string
            IdentifierPropertyName = "Id";

        [DefaultArgument(ArgumentType.MultipleUnique, HelpText = "The source files to process")]
        public string[] Inputs;
    }
}
