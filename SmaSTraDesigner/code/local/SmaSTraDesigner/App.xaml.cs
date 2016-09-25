﻿namespace SmaSTraDesigner
{
    using System.Windows;

    using Common;

    using BusinessLogic;
    using BusinessLogic.config;
    using System.IO;
    using BusinessLogic.utils;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
	{
		#region event handlers

		private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
		}

		private void Application_Startup(object sender, StartupEventArgs e)
        {
            SwitchWorkspace("");
        }


        /// <summary>
        /// Switches the Workspace.
        /// </summary>
        /// <param name="newWorkspace"></param>
        public static void SwitchWorkspace(string newWorkspace)
        {
            //Be sure the new Workspace exists:
            if(!string.IsNullOrWhiteSpace(newWorkspace)) Directory.CreateDirectory(newWorkspace);

            //Set the new Workspace:
            SmaSTraConfiguration.WORK_SPACE = newWorkspace;

            //Copy all the Basic stuff in:
            string newGeneratedPath = Path.Combine(SmaSTraConfiguration.WORK_SPACE, "generated");
            if (  !Directory.Exists(newGeneratedPath) 
                || Directory.EnumerateFileSystemEntries(newGeneratedPath).Empty())
            {
                string orgGeneratedPath = "generated";
                DirCopy.PlainCopy(orgGeneratedPath, newGeneratedPath);
            }


            //Clear the current Tree and the GUI:
            //TODO add reload of Transformation Tree here!

            //Reload the managers:
            Singleton<SmaSTraConfiguration>.Instance.Reload();
            Singleton<ClassManager>.Instance.Reload();
            Singleton<Library>.Instance.loadLibrary();
        }

		#endregion event handlers
	}
}