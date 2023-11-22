using Microsoft.UI.Xaml;
using Symptum.Core.Subjects.Books;
using Symptum.Core.Subjects.QuestionBank;
using System;
using System.Diagnostics;
using System.IO;

namespace Symptum.Editor
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            LoadAllBookLists(AppDomain.CurrentDomain.BaseDirectory + "\\Books\\");
            m_window = new MainWindow();
            m_window.Activate();
        }

        private void LoadAllBookLists(string workPath)
        {
            if (!Directory.Exists(workPath)) return;

            DirectoryInfo directoryInfo = new(workPath);
            var csvfiles = directoryInfo.GetFiles("*.csv");
            foreach (var csvfile in csvfiles)
            {
                BookStore.LoadBooks(csvfile.FullName);
            }
        }

        private Window m_window;
    }
}