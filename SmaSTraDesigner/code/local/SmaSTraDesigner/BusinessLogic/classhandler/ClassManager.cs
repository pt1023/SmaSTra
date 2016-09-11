﻿namespace SmaSTraDesigner.BusinessLogic
{
    using classhandler;
    using nodes;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Json;
    using System.Linq;
    using System.Windows;
    using static classhandler.CombinedNodeClass;

    /// <summary>
    /// Loads and manages node classes from given metadata generated by SmaSTra base library.
    /// </summary>
    public class ClassManager : INotifyPropertyChanged
	{
		#region constants

		/// <summary>
		/// Name of the description property field in JSON metadata.
		/// </summary>
		private const string JSON_PROP_DESCRIPTION = "description";

		/// <summary>
		/// Name of the display name property field in JSON metadata.
		/// </summary>
		private const string JSON_PROP_DISPLAY = "display";

		/// <summary>
		/// Name of the input type(s) property field in JSON metadata.
		/// </summary>
		private const string JSON_PROP_INPUT = "input";

		/// <summary>
		/// Name of the output type property field in JSON metadata.
		/// </summary>
		private const string JSON_PROP_OUTPUT = "output";

        /// <summary>
        /// Name of the node type (sensor/transformation) property field in JSON metadata.
        /// </summary>
        private const string JSON_PROP_TYPE = "type";

        /// <summary>
        /// Name of the connections if this is a Combined node.
        /// </summary>
        private const string JSON_PROP_CONNECTIONS = "connections";

        /// <summary>
        /// Name of the path to the sub-Elements if this is a CombinedNode.
        /// </summary>
        private const string JSON_PROP_SUB_ELEMENTS = "subElements";

        /// <summary>
        /// Name of the path for the Output Node in a Combined Node.
        /// </summary>
        private const string JSON_PROP_OUTPUT_NODE_NAME = "outputNodeName";

        /// <summary>
        /// Filename for metadata.
        /// </summary>
        private const string METADATA_FILENAME = "metadata.json";

        /// <summary>
        /// Possible value for node type.
        /// </summary>
        private const string NODE_TYPE_SENSOR = "sensor";

        /// <summary>
        /// Possible value for node type.
        /// </summary>
        private const string NODE_TYPE_TRANSFORMATION = "transformation";

        /// <summary>
        /// Possible value for node type.
        /// </summary>
        private const string NODE_TYPE_COMBINED = "combined";

        #endregion constants

        #region static methods

        /// <summary>
        /// Determins and verifies node type read from metadata.
        /// </summary>
        /// <param name="type">Node type as string.</param>
        /// <returns>Verified node type</returns>
        private static NodeType GetNodeType(string type)
		{
			switch (type)
			{
				case NODE_TYPE_TRANSFORMATION:
					return NodeType.Transformation;

                case NODE_TYPE_SENSOR:
                    return NodeType.Sensor;

                case NODE_TYPE_COMBINED:
                    return NodeType.Combined;

                default:
					throw new Exception(String.Format("Unrecognized node type \"{0}\".", type));
			}
		}

		#endregion static methods

		#region fields

		/// <summary>
		/// List of all loaded transformations that represent a simple conversion (one input one output).
		/// </summary>
		private Transformation[] baseConversions = null;

		/// <summary>
		/// List of all loaded data sources.
		/// </summary>
		private DataSource[] baseDataSources = null;

		/// <summary>
		/// List of all transformations (that do not fall in the conversion category).
		/// </summary>
		private Transformation[] baseTransformations = null;


        /// <summary>
        /// List of all Combined Nodes.
        /// </summary>
        private CombinedNode[] baseCombinedNodes = null;

        /// <summary>
        /// Dictionary that keeps track of loaded node classes to ensure no ambiguity.
        /// </summary>
        private Dictionary<string, NodeClass> classes = new Dictionary<string, NodeClass>();

		/// <summary>
		/// Dictionary that keeps track of loaded data types to ensure no ambiguity.
		/// </summary>
		private Dictionary<string, DataType> dataTypes = new Dictionary<string, DataType>();

		#endregion fields

		#region events

		/// <summary>
		/// Is raised whenever a compatible property changes its value.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		#endregion events

		#region properties

		/// <summary>
		/// Gets the BaseConversions instance (creates one if none exists).
		/// List of all loaded transformations that represent a simple conversion (one input one output).
		/// </summary>
		public Transformation[] BaseConversions
		{
			get
			{
				if (this.baseConversions == null)
				{
					this.baseConversions = this.classes.Values.Where(cls => cls.BaseNode is Transformation && cls.InputTypes.Length == 1)
						.Select(cls => (Transformation)cls.BaseNode).ToArray();
				}

				return this.baseConversions;
			}
		}

		/// <summary>
		/// Gets the BaseDataSources instance (creates one if none exists).
		/// List of all loaded data sources.
		/// </summary>
		public DataSource[] BaseDataSources
		{
			get
			{
				if (this.baseDataSources == null)
				{
					this.baseDataSources = this.classes.Values.Where(cls => cls.BaseNode is DataSource)
						.Select(cls => (DataSource)cls.BaseNode).ToArray();
				}

				return this.baseDataSources;
			}
		}

		/// <summary>
		/// Gets the BaseTransformations instance (creates one if none exists).
		/// List of all transformations (that do not fall in the conversion category).
		/// </summary>
		public Transformation[] BaseTransformations
		{
			get
			{
				if (this.baseTransformations == null)
				{
					this.baseTransformations = this.classes.Values.Where(cls => cls.BaseNode is Transformation && cls.InputTypes.Length > 1)
						.Select(cls => (Transformation)cls.BaseNode).ToArray();
				}

				return this.baseTransformations;
			}
		}


        /// <summary>
        /// Gets the Base Combined Nodes instance (creates one if none exists).
        /// List of all Combined Nodes.
        /// </summary>
        public CombinedNode[] BaseCombinedNodes
        {
            get
            {
                if (this.baseCombinedNodes == null)
                {
                    this.baseCombinedNodes = this.classes.Values.Where(cls => cls.BaseNode is CombinedNode)
                        .Select(cls => (CombinedNode)cls.BaseNode).ToArray();
                }

                return this.baseCombinedNodes;
            }
        }

        #endregion properties

        #region methods


        /// <summary>
        /// Adds the Node-Class passed.
        /// </summary>
        /// <param name="nodeClass"> to add. </param>
        /// <returns></returns>
        public NodeClass AddClass(NodeClass nodeClass)
        {
            if (nodeClass == null) return null;

            //If already present -> Do not add!
            NodeClass result;
            if (this.classes.TryGetValue(nodeClass.Name, out result)) return result;

            this.classes.Add(nodeClass.Name, nodeClass);

            // Send property change notifications for appropreate list property, so the GUI can react.
            switch (nodeClass.NodeType)
            {
                case NodeType.Transformation:
                    if (nodeClass.InputTypes.Length == 1)
                    {
                        this.baseConversions = null;
                        this.OnPropertyChanged("BaseConversions");
                    }
                    else
                    {
                        this.baseTransformations = null;
                        this.OnPropertyChanged("TransformationClasses");
                    }
                    break;

                case NodeType.Sensor:
                    this.baseDataSources = null;
                    this.OnPropertyChanged("BaseDataSources");
                    break;

                case NodeType.Combined:
                    this.baseConversions = null;
                    this.OnPropertyChanged("BaseDataConversions");
                    break;
            }

            return nodeClass;
        }


		/// <summary>
		/// Add a node class resulting from the interpretation of the input strings.
		/// </summary>
		/// <param name="name">Name of the node class</param>
		/// <param name="type">Node type (sensor/transformation)</param>
		/// <param name="outputType">Node output type name.</param>
		/// <param name="inputTypes">Input type names</param>
		/// <param name="displayName">Node class's display name for the GUI.</param>
		/// <param name="description"></param>
		/// <returns>The interpreted node class.</returns>
		public NodeClass AddClass(string name, string type, string outputType, string[] inputTypes, string displayName = null, string description = null, List<SimpleSubNode> subNodes = null, List<SimpleConnection> connections = null, string outputNodeName = null)
		{
			NodeClass result;
			if (this.classes.TryGetValue(name, out result))
			{
				return result;
			}


            // Use displayname if possible for base node.
            if (String.IsNullOrWhiteSpace(displayName))
            {
                displayName = name;
            }


            // Build baseNode for use with the nodeclass from type parameter.
            Node baseNode;
			DataType[] actualInputTypes = null;
			NodeType nodeType = GetNodeType(type);
			switch (nodeType)
			{
				default:
				case NodeType.Transformation:
					baseNode = new Transformation();
					actualInputTypes = inputTypes.Select(this.AddDataType).ToArray();
					break;

				case NodeType.Sensor:
					baseNode = new DataSource();
					break;

                case NodeType.Combined:
                    actualInputTypes = inputTypes.Select(this.AddDataType).ToArray();
                    baseNode = new CombinedNode();
                    break;
			}


            baseNode.Name = displayName;

            // Build NodeClass instance.
            switch (nodeType)
            {
                default:
                case NodeType.Transformation:
                case NodeType.Sensor:
                    result = new NodeClass(nodeType, name, baseNode, this.AddDataType(outputType), actualInputTypes)
                    {
                        DisplayName = displayName,
                        Description = description
                    };
                    break;

                case NodeType.Combined:
                    result = new CombinedNodeClass(nodeType, name, baseNode, subNodes, connections, this.AddDataType(outputType), outputNodeName, actualInputTypes)
                    {
                        DisplayName = displayName,
                        Description = description
                    };
                    break;
            }
			

            AddClass(result);
			return result;
		}

		/// <summary>
		/// Interpret and load a DataType from given name.
		/// </summary>
		/// <param name="dataTypeName"></param>
		/// <returns>Interpreted DataType instance.</returns>
		public DataType AddDataType(string dataTypeName)
		{
			if (String.IsNullOrWhiteSpace(dataTypeName))
			{
				throw new ArgumentException("String argument 'dataTypeName' must not be null or empty (incl. whitespace).", "dataTypeName");
			}

			// Create DataType instance from name if it does not already exist.
			if (!this.dataTypes.ContainsKey(dataTypeName))
			{
				return this.dataTypes[dataTypeName] = new DataType(dataTypeName);
			}

			return this.dataTypes[dataTypeName];
		}

		/// <summary>
		/// Load all node classes from a directory path.
		/// </summary>
		/// <param name="path">Directory path containing metadata for node classes to load.</param>
		public void LoadClasses(string path)
		{
			if (!Directory.Exists(path)) return;

			// Subdirectories are presumed to contain a node class each.
			string[] dirs = Directory.GetDirectories(path);
			foreach (string dir in dirs)
			{
				string dirName = Path.GetFileName(dir);
                string metaDataFilePath = Path.Combine(dir, METADATA_FILENAME);

                //Check if metadata file exists first!
                if (!File.Exists(metaDataFilePath)) continue;

                try
                {
                    // Load JSON file as JsonObject.
                    JsonObject jso;
                    using (var stream = File.OpenRead(metaDataFilePath))
                    {
                        jso = (JsonObject)JsonObject.Load(stream);
                    }

                    // Read necessary data from JSON.
                    string type = jso[JSON_PROP_TYPE].ReadAs<string>();
                    string[] inputTypes = new string[0];
                    if (jso.ContainsKey(JSON_PROP_INPUT))
                    {
                        inputTypes = jso[JSON_PROP_INPUT].Select(kvp => kvp.Value.ReadAs<string>()).ToArray();
                    }

					JsonValue value;
					string displayName = null;
					if (jso.TryGetValue(JSON_PROP_DISPLAY, out value))
					{
						displayName = value.ReadAs<string>();
					}

					string description = null;
					if (jso.TryGetValue(JSON_PROP_DESCRIPTION, out value))
					{
						description = value == null ? "No description" : value.ReadAs<string>();
					}

                    //If combined, read these:
                    string outputNodeName = null;
                    if (jso.TryGetValue(JSON_PROP_OUTPUT_NODE_NAME, out value))
                    {
                        outputNodeName = value.ReadAs<string>();
                    }
                    
                    List<SimpleConnection> connections = null;
                    if (jso.TryGetValue(JSON_PROP_CONNECTIONS, out value))
                    {
                        connections = new List<SimpleConnection>();
                        foreach (dynamic obj in jso[JSON_PROP_CONNECTIONS])
                        {
                            dynamic obj2 = obj.Value;
                            connections.Add(new SimpleConnection(obj2.firstNode.ReadAs<string>().Replace("\"",""), obj2.secondNode.ReadAs<string>().Replace("\"",""), Int32.Parse(obj2.position.ToString())));
                        }
                    }

                    List<SimpleSubNode> subElements = null;
                    if (jso.TryGetValue(JSON_PROP_SUB_ELEMENTS, out value))
                    {
                        subElements = new List<SimpleSubNode>();
                        foreach (dynamic obj in jso[JSON_PROP_SUB_ELEMENTS])
                        {
                            dynamic obj2 = obj.Value.Properties;
                            Dictionary<string, string> properties = new Dictionary<string,string>();
                            foreach (KeyValuePair<string, JsonValue> kvp in obj2) properties.Add(kvp.Key.Replace("\"",""), kvp.Value.ReadAs<string>().Replace("\"",""));
                            subElements.Add(new SimpleSubNode(properties));
                        }
                    }

                    // Use data to create new node class.
                    this.AddClass(dirName, type, jso["output"].ReadAs<string>(), inputTypes, displayName, description, subElements, connections, outputNodeName);
				}
				catch (Exception ex)
				{
                    MessageBox.Show("Could not load " + dirName + " in: \n" + dir + "\nSkipping this element.", "Error in loading " + dirName);
					//throw new Exception(String.Format("Unable to read metadata for node class \"{0}\" in \"{1}\".", dirName, path), ex);
				}
			}
		}

        /// <summary>
        /// Gets the First node found with that type name.
        /// Node types should be unique.
        /// Returns null if none found.
        /// </summary>
        /// <param name="typeName">To search.</param>
        /// <returns>The first found node with that name, null if none found.</returns>
        public Node GetNewNodeForType(String typeName)
        {
            Node node = this.BaseConversions
                .Concat<Node>(BaseDataSources)
                .Concat<Node>(BaseTransformations)
                .Concat<Node>(BaseCombinedNodes)
                .FirstOrDefault(x => x.Class.Name == typeName);
            return node == null ? node : (Node)node.Clone();
        }


		/// <summary>
		/// Raises the PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">Name of the property that changed values.</param>
		private void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion methods

		#region enumerations

		/// <summary>
		/// Possible node types.
		/// </summary>
		public enum NodeType
		{
			Transformation,
			Sensor,
            Combined
		}

		#endregion enumerations
	}
}