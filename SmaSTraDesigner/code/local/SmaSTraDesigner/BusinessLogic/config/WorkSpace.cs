﻿namespace SmaSTraDesigner.BusinessLogic.config
{
    public static class WorkSpace
    {
        /// <summary>
        /// The prefix for the Base elements directory.
        /// </summary>
        public const string BASE_DIR = "base";

        /// <summary>
        /// The prefix for the Created elements directory.
        /// </summary>
        public const string CREATED_DIR = "created";

        /// <summary>
        /// The Directory for Libs.
        /// </summary>
        public const string LIBS_DIR = "libs";

        /// <summary>
        /// The Directory for the Data Types.
        /// </summary>
        public const string DATA_TYPES_DIR = "datatypes";

        /// <summary>
        /// The Workspace to use
        /// </summary>
        public static string DIR { get; set; }
    }
}
