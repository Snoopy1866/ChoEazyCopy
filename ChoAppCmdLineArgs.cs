﻿using Cinchoo.Core;
using Cinchoo.Core.Diagnostics;
using Cinchoo.Core.IO;
using Cinchoo.Core.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace ChoEazyCopy
{
    [ChoCommandLineArgObject(DisplayDefaultValue = true, UsageSwitch = "_")]
    public class ChoShellExtCmdLineArgs : ChoCommandLineArgObject
    {
        [ChoCommandLineArg("d", Description = "Source directory.", Order = -2)]
        public string Directory
        {
            get;
            set;
        }
    }

    [ChoCommandLineArgObject(DisplayDefaultValue = true, UsageSwitch = "_")]
    public class ChoFileAssociationCmdLineArgs : ChoCommandLineArgObject
    {
        [ChoPositionalCommandLineArg(1, "SettingsFilePath", Description = "Settings file path.")]
        public string SettingsFilePath
        {
            get;
            set;
        }

        public bool IsAppFile;

        protected override void OnAfterCommandLineArgObjectLoaded(string[] commandLineArgs)
        {
            if (!SettingsFilePath.IsNullOrWhiteSpace())
            {
                IsAppFile = Path.GetExtension(SettingsFilePath) == AppHost.AppFileExt;
            }
        }
    }

    [ChoCommandLineArgObject(DisplayDefaultValue=true)]
    public class ChoAppCmdLineArgs : ChoCommandLineArgObject
    {
        [ChoPositionalCommandLineArg(1, "SettingsFilePath", Description = "Settings file path.", IsRequired = true)]
        public string SettingsFilePath
        {
            get;
            set;
        }

        [ChoCommandLineArg("s", Description = "Source directory.", Order = 2, NoOfTabsSwitchDescFormatSeparator=3)]
        public string SourceDirectory
        {
            get;
            set;
        }

        [ChoCommandLineArg("d", Description = "Destination directory.", Order = 3, NoOfTabsSwitchDescFormatSeparator = 3)]
        public string DestDirectory
        {
            get;
            set;
        }

        protected override void OnAfterCommandLineArgObjectLoaded(string[] commandLineArgs)
        {
            if (!SettingsFilePath.IsNullOrWhiteSpace())
            {
                SettingsFilePath = ChoPath.GetFullPath(SettingsFilePath);
            }
        }

        public void StartFileCopy(string sourceDirectory = null, string destDirectory = null)
        {
            try
            {
                ChoAppSettings appSettings = new ChoAppSettings();
                if (!SettingsFilePath.IsNullOrWhiteSpace())
                {
                    if (!File.Exists(SettingsFilePath))
                        throw new ArgumentException("找不到设置文件 '{0}' 。".FormatString(SettingsFilePath));

                    appSettings.LoadXml(File.ReadAllText(SettingsFilePath));
                }

                ChoConsole.WriteLine();

                ChoRoboCopyManager _roboCopyManager = new ChoRoboCopyManager(SettingsFilePath);
                _roboCopyManager.Status += (sender, e) =>
                {
                    ChoTrace.Write(e.Message);
                    ChoConsole.Write(e.Message, ConsoleColor.Yellow);
                };
                _roboCopyManager.AppStatus += (sender, e) =>
                {
                    ChoTrace.Write(e.Message);
                    ChoConsole.Write(e.Message, ConsoleColor.Yellow);
                };

                _roboCopyManager.Process(appSettings.RoboCopyFilePath, appSettings.GetCmdLineParams(), 
                    appSettings, true);
            }
            catch (ThreadAbortException)
            {
                Console.WriteLine("RoboCopy 处理被用户取消。");
            }
            catch (Exception ex)
            {
                Console.WriteLine("RoboCopy 处理失败。" + Environment.NewLine + ChoApplicationException.ToString(ex));
            }
        }
    }
}
