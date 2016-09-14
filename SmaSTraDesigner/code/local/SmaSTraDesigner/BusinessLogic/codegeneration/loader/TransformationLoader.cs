﻿using Newtonsoft.Json.Linq;
using SmaSTraDesigner.BusinessLogic.utils;

namespace SmaSTraDesigner.BusinessLogic.codegeneration.loader
{
    class TransformationLoader : AbstractNodeLoader
    {
        public TransformationLoader(ClassManager cManager) 
            : base(ClassManager.NodeType.Transformation, cManager)
        {}

        public override NodeClass loadFromJson(string name, JObject root)
        {
            string displayName = ReadDisplayName(root).EmptyDefault(name);
            string description = ReadDescription(root).EmptyDefault("No Description");
            DataType output = ReadOutput(root);
            DataType[] inputs = ReadInputs(root);
            Node baseNode = new Transformation()
            {
                Name = displayName
            };

            return new NodeClass(nodeType, name, baseNode, output, inputs)
            {
                DisplayName = displayName,
                Description = description
            };
        }

    }
}
