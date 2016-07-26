﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmaSTraDesigner.BusinessLogic.serializers;
using System.Dynamic;
using System;
using Newtonsoft.Json.Linq;
using Common;
using SmaSTraDesigner.Controls;
using System.Threading.Tasks;

namespace SmaSTraDesigner.BusinessLogic
{


    class TreeSerilizer
    {

        private static TransformationTree tree;

        private static List<Node> newNodes = new List<Node>();
        private static List<Connection> newConnections = new List<Connection>();


        public static void Serialize(TransformationTree transformationTree, string targetFile)
        {
            tree = transformationTree;
            //Before we start -> Check if we are connected at all:
            if (tree == null || tree.OutputNode.InputNode == null)
            {
                Console.WriteLine("Root node is not connected to any node! Can not save!");
                return;
            }


            //Generate the Serializer:
            var nodeSerializer = new NodeSerializer();

            //Combine the JSON:
            dynamic json = new ExpandoObject();
            json.nodes = tree.Nodes.Select(nodeSerializer.serializeNode);
            json.connections = tree.Connections.Select(nodeSerializer.serializeNodeConnection);


            //Create the Json settings:
            var jsonSettings = new JsonSerializerSettings()
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.Indented
            };

            //Generate a string and write it to the file:
            string text = JsonConvert.SerializeObject(json, jsonSettings);
            File.WriteAllText(targetFile, text);
        }


        public static void Deserialize(TransformationTree transformationTree, string targetFile)
        {
            tree = transformationTree;
            //Read the singleton ClassManager.
            ClassManager classManager = Singleton<ClassManager>.Instance;

            //First clear old tree:
            foreach (Node node in new List<Node>(tree.Nodes)) if(!(node is OutputNode)) tree.DesignTree.RemoveNode(node);
            foreach (Connection connection in new List<Connection>(tree.Connections)) tree.DesignTree.RemoveConnection(connection);

            //Generate the Serializer + lists:
            var nodeSerializer = new NodeSerializer();
            UcTreeDesigner treeDesigner = tree.DesignTree;

            newNodes = new List<Node>();
            newConnections = new List<Connection>();

            dynamic json = JObject.Parse(File.ReadAllText(targetFile));

            //Seems like this is the only way this is synthactically correct....
            //Read Nodes:
            foreach (dynamic obj in json["nodes"])
            {
                Node node = nodeSerializer.deserializeNode(obj, classManager);
                if (node != null) newNodes.Add(node);
            }

            //Read connections:
            foreach (dynamic obj in json["connections"])
            {
                Connection? connection = nodeSerializer.deserializeConnection(obj, newNodes);
                if (connection.HasValue)
                {
                    //Apply the connection:
                    applyConnection(connection.Value);
                    newConnections.Add(connection.Value);
                }
                
            }

            //Then add new ones. FIRST THE NODES!
            foreach (Node node in newNodes)
            {
                if(node is OutputNode)
                {
                    OutputNode outNode = tree.OutputNode;
                    outNode.PosX = node.PosX;
                    outNode.PosY = node.PosY;
                }
                else treeDesigner.AddNode(node, true);
            }

            //TODO Fix this somehow:
            addConnections();
        }

        public static void addConnections()
        {
            foreach (Connection connection in newConnections) tree.DesignTree.AddConnection(connection);
        }

        private static void applyConnection(Connection connection)
        {
            Node input = connection.InputNode;
            Node output = connection.OutputNode;
            if (input == null) return;

            //Add the input:
            if (input is OutputNode) ((OutputNode)tree.OutputNode).InputNode = connection.OutputNode;
            if (input is Transformation) ((Transformation)input).InputNodes[connection.InputIndex] = output;
        }
    }
}