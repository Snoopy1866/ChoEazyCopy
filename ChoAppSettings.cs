namespace ChoEazyCopy
{
    #region NameSpaces

    using System;
    using System.Text;
    using System.Collections.Generic;
    using Cinchoo.Core.Configuration;
    using System.ComponentModel;
    using System.Runtime.Remoting.Contexts;
    using System.Dynamic;
    using Cinchoo.Core.Text.RegularExpressions;
    using System.Text.RegularExpressions;
    using Cinchoo.Core.Diagnostics;
    using Cinchoo.Core;
    using System.Diagnostics;
    using Cinchoo.Core.Xml.Serialization;
    using Cinchoo.Core.IO;
    using System.IO;
    using System.Xml.Serialization;
    using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
    using System.Windows;
    using Cinchoo.Core.WPF;
    using System.Windows.Data;
    using System.Linq;
    using System.Collections.ObjectModel;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using System.Runtime.CompilerServices;
    using System.Reflection;

    #endregion NameSpaces

    [Flags]
    public enum ChoCopyFlags
    {
        [Description("D")]
        Data,
        [Description("A")]
        Attributes,
        [Description("T")]
        Timestamps,
        [Description("S")]
        SecurityNTFSACLs,
        [Description("O")]
        OwnerInfo,
        [Description("U")]
        AuditingInfo
    }

    public enum ChoFileAttributes
    {
        [Description("R")]
        ReadOnly,
        [Description("H")]
        Hidden,
        [Description("A")]
        Archive,
        [Description("S")]
        System,
        [Description("C")]
        Compressed,
        [Description("N")]
        NotContentIndexed,
        [Description("E")]
        Encrypted,
        [Description("T")]
        Temporary
    }

    public enum ChoFileSelectionAttributes
    {
        [Description("R")]
        ReadOnly,
        [Description("A")]
        Archive,
        [Description("S")]
        System,
        [Description("H")]
        Hidden,
        [Description("C")]
        Compressed,
        [Description("N")]
        NotContentIndexed,
        [Description("E")]
        Encrypted,
        [Description("T")]
        Temporary,
        [Description("O")]
        Offline
    }

    public enum ChoFileMoveAttributes
    {
        [Description("")]
        None,
        [Description("/MOV")]
        MoveFilesOnly,
        [Description("/MOVE")]
        MoveDirectoriesAndFiles,
    }
    public abstract class ChoViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    [ChoNameValueConfigurationSection("applicationSettings" /*, BindingMode = ChoConfigurationBindingMode.OneWayToSource */, Silent = false)]
    public class ChoAppSettings : ChoViewModelBase, ICloneable<ChoAppSettings> //: ChoConfigurableObject
    {
        private readonly static XmlSerializer _xmlSerializer = new XmlSerializer(typeof(ChoAppSettings));
        private readonly static Dictionary<string, PropertyInfo> _propInfos = new Dictionary<string, PropertyInfo>();
        private readonly static Dictionary<PropertyInfo, object> _defaultValues = new Dictionary<PropertyInfo, object>();
        private readonly static ChoAppSettings DefaultInstance = new ChoAppSettings();
        static ChoAppSettings()
        {
            ChoPropertyInfoAttribute attr = null;
            foreach (var pi in ChoType.GetProperties(typeof(ChoAppSettings)))
            {
                attr = pi.GetCustomAttributes(false).OfType<ChoPropertyInfoAttribute>().FirstOrDefault();
                if (attr != null)
                {
                    _propInfos.Add(pi.Name, pi);
                    _defaultValues.Add(pi, attr.DefaultValue);
                }
            }

            ChoObject.ResetObject(DefaultInstance);
        }

        public ChoAppSettings()
        {
        }

        #region Instance Data Members (Others)

        private bool _showOutputLineNumbers;
        [Browsable(false)]
        [ChoPropertyInfo("showOutputLineNumbers")]
        public bool ShowOutputLineNumbers
        {
            get { return _showOutputLineNumbers; }
            set
            {
                _showOutputLineNumbers = value;
                NotifyPropertyChanged();
            }
        }

        private int _maxStatusMsgSize;
        [Browsable(false)]
        [ChoPropertyInfo("maxStatusMsgSize", DefaultValue = "1000")]
        public int MaxStatusMsgSize
        {
            get { return _maxStatusMsgSize; }
            set
            {
                if (value > 0)
                {
                    _maxStatusMsgSize = value;
                    NotifyPropertyChanged();
                }
            }
        }

        string _sourceDirectory;
        [Browsable(false)]
        [ChoPropertyInfo("sourceDirectory")]
        public string SourceDirectory
        {
            get { return _sourceDirectory; }
            set
            {
                _sourceDirectory = value;
                NotifyPropertyChanged();
            }
        }

        string _destDirectory;
        [Browsable(false)]
        [ChoPropertyInfo("destDirectory")]
        public string DestDirectory
        {
            get { return _destDirectory; }
            set
            {
                _destDirectory = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Others)

        #region Instance Data Members (Common Options)

        string _roboCopyFilePath;
        [Category("1. 通用选项")]
        [Description("RoboCopy 程序路径")]
        [DisplayName("RoboCopy 程序路径")]
        [ChoPropertyInfo("roboCopyFilePath", DefaultValue = "RoboCopy.exe")]
        public string RoboCopyFilePath
        {
            get { return _roboCopyFilePath; }
            set
            {
                _roboCopyFilePath = value;
                NotifyPropertyChanged();
            }
        }

        string _files;
        [Category("1. 通用选项")]
        [Description("要复制的文件（名称/通配符：默认为“*.*”）")]
        [DisplayName("Files")]
        [ChoPropertyInfo("files", DefaultValue = "*.*")]
        public string Files
        {
            get { return _files; }
            set
            {
                _files = value;
                NotifyPropertyChanged();
            }
        }

        string _additionalParams;
        [Category("1. 通用选项")]
        [Description("附加命令行参数（可选）")]
        [DisplayName("附加参数")]
        [ChoPropertyInfo("additionalParams", DefaultValue = "")]
        public string AdditionalParams
        {
            get { return _additionalParams; }
            set
            {
                _additionalParams = value;
                NotifyPropertyChanged();
            }
        }

        string _precommands;
        [Category("1. 通用选项")]
        [Description("指定在 robocopy 操作之前运行的 MS-DOS 命令，以 ; 分隔（可选）")]
        [DisplayName("预处理命令")]
        [ChoPropertyInfo("precommands", DefaultValue = "")]
        public string Precommands
        {
            get { return _precommands; }
            set
            {
                _precommands = value;
                NotifyPropertyChanged();
            }
        }

        string _postcommands;
        [Category("1. 通用选项")]
        [Description("指定在 robocopy 操作之后运行的 MS-DOS 命令，以 ; 分隔（可选）")]
        [DisplayName("后处理命令")]
        [ChoPropertyInfo("postcommands", DefaultValue = "")]
        public string Postcommands
        {
            get { return _postcommands; }
            set
            {
                _postcommands = value;
                NotifyPropertyChanged();
            }
        }

        string _comments;
        [Category("1. 通用选项")]
        [Description("备份任务的简短描述")]
        [DisplayName("备注")]
        [ChoPropertyInfo("comments", DefaultValue = "")]
        public string Comments
        {
            get { return _comments; }
            set
            {
                _comments = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Common Options)

        #region Instance Data Members (Source Options)

        bool _copyNoEmptySubDirectories;
        [Category("2. 源目录选项")]
        [Description("复制子目录，但不复制空的子目录 (/S)")]
        [DisplayName("复制非空子目录")]
        [ChoPropertyInfo("copyNoEmptySubDirectories")]
        public bool CopyNoEmptySubDirectories
        {
            get { return _copyNoEmptySubDirectories; }
            set
            {
                _copyNoEmptySubDirectories = value;
                NotifyPropertyChanged();
            }
        }

        bool _copySubDirectories;
        [Category("2. 源目录选项")]
        [Description("复制子目录，包括空的子目录 (/E)")]
        [DisplayName("复制子目录")]
        [ChoPropertyInfo("copySubDirectories", DefaultValue = "true")]
        public bool CopySubDirectories
        {
            get { return _copySubDirectories; }
            set
            {
                _copySubDirectories = value;
                NotifyPropertyChanged();
            }
        }

        string _copyFlags;
        [Category("2. 源目录选项")]
        [Description("要复制的文件内容 (默认为 /COPY:DAT)。 (复制标记 : D=数据, A=属性, T=时间戳, S=安全=NTFS ACLs, O=所有者信息, U=审核信息)。(/COPY:复制标记)")]
        [DisplayName("复制标记")]
        [ChoPropertyInfo("copyFlags", DefaultValue = "Data,Attributes,Timestamps")]
        [Editor(typeof(CopyFlagsEditor), typeof(CopyFlagsEditor))]
        public string CopyFlags
        {
            get { return _copyFlags; }
            set
            {
                _copyFlags = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyFilesWithSecurity;
        [Category("2. 源目录选项")]
        [Description("复制具有安全性的文件 (等同于 /COPY:DATS)。 (/SEC)")]
        [DisplayName("复制具有安全性的文件")]
        [ChoPropertyInfo("copyFilesWithSecurity")]
        public bool CopyFilesWithSecurity
        {
            get { return _copyFilesWithSecurity; }
            set
            {
                _copyFilesWithSecurity = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyDirTimestamp;
        [Category("2. 源目录选项")]
        [Description("复制目录时间戳。 (/DCOPY:T)")]
        [DisplayName("复制目录时间戳")]
        [ChoPropertyInfo("copyDirTimestamp")]
        public bool CopyDirTimestamp
        {
            get { return _copyDirTimestamp; }
            set
            {
                _copyDirTimestamp = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyFilesWithFileInfo;
        [Category("2. 源目录选项")]
        [Description("复制所有文件信息 (等同于 /COPY:DATSOU)。 (/COPYALL)")]
        [DisplayName("复制所有文件信息")]
        [ChoPropertyInfo("copyFilesWithFileInfo")]
        public bool CopyFilesWithFileInfo
        {
            get { return _copyFilesWithFileInfo; }
            set
            {
                _copyFilesWithFileInfo = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyFilesWithNoFileInfo;
        [Category("2. 源目录选项")]
        [Description("不复制任何文件信息 (与 /PURGE 一起使用)。 (/NOCOPY)")]
        [DisplayName("不复制任何文件信息")]
        [ChoPropertyInfo("copyFilesWithNoFileInfo")]
        public bool CopyFilesWithNoFileInfo
        {
            get { return _copyFilesWithNoFileInfo; }
            set
            {
                _copyFilesWithNoFileInfo = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyOnlyFilesWithArchiveAttributes;
        [Category("2. 源目录选项")]
        [Description("仅复制具有存档属性集的文件。 (/A)")]
        [DisplayName("仅复制具有存档属性集的文件")]
        [ChoPropertyInfo("copyOnlyFilesWithArchiveAttributes")]
        public bool CopyOnlyFilesWithArchiveAttributes
        {
            get { return _copyOnlyFilesWithArchiveAttributes; }
            set
            {
                _copyOnlyFilesWithArchiveAttributes = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyOnlyFilesWithArchiveAttributesAndReset;
        [Category("2. 源目录选项")]
        [Description("仅复制具有存档属性的文件并重置存档属性。 (/M)")]
        [DisplayName("仅复制具有存档属性的文件并重置存档属性")]
        [ChoPropertyInfo("copyOnlyFilesWithArchiveAttributesAndReset")]
        public bool CopyOnlyFilesWithArchiveAttributesAndReset
        {
            get { return _copyOnlyFilesWithArchiveAttributesAndReset; }
            set
            {
                _copyOnlyFilesWithArchiveAttributesAndReset = value;
                NotifyPropertyChanged();
            }
        }

        uint _onlyCopyNLevels;
        [Category("2. 源目录选项")]
        [Description("仅复制源目录树的前 n 层。 0 - 任意层。 (/LEV:n)")]
        [DisplayName("仅复制前 n 层")]
        [ChoPropertyInfo("onlyCopyNLevels")]
        public uint OnlyCopyNLevels
        {
            get { return _onlyCopyNLevels; }
            set
            {
                _onlyCopyNLevels = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesOlderThanNDays;
        [Category("2. 源目录选项")]
        [Description("最长的文件存在时间 - 排除早于 n 天/日期的文件。 (/MAXAGE:n)")]
        [DisplayName("排除早于 n 天的文件")]
        [ChoPropertyInfo("excludeFilesOlderThanNDays")]
        public uint ExcludeFilesOlderThanNDays
        {
            get { return _excludeFilesOlderThanNDays; }
            set
            {
                _excludeFilesOlderThanNDays = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesNewerThanNDays;
        [Category("2. 源目录选项")]
        [Description("最短的文件存在时间 - 排除晚于 n 天/日期的文件。 (/MINAGE:n)")]
        [DisplayName("排除晚于 n 天的文件")]
        [ChoPropertyInfo("excludeFilesNewerThanNDays")]
        public uint ExcludeFilesNewerThanNDays
        {
            get { return _excludeFilesNewerThanNDays; }
            set
            {
                _excludeFilesNewerThanNDays = value;
                NotifyPropertyChanged();
            }
        }

        bool _assumeFATFileTimes;
        [Category("2. 源目录选项")]
        [Description("假设 FAT 文件时间(2 秒粒度)。 (/FFT)")]
        [DisplayName("假设 FAT 文件时间")]
        [ChoPropertyInfo("assumeFATFileTimes")]
        public bool AssumeFATFileTimes
        {
            get { return _assumeFATFileTimes; }
            set
            {
                _assumeFATFileTimes = value;
                NotifyPropertyChanged();
            }
        }

        bool _turnOffLongPath;
        [Category("2. 源目录选项")]
        [Description("关闭超长路径(> 256 个字符)支持。 (/256)")]
        [DisplayName("关闭超长路径支持")]
        [ChoPropertyInfo("turnOffLongPath")]
        public bool TurnOffLongPath
        {
            get { return _turnOffLongPath; }
            set
            {
                _turnOffLongPath = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Source Options)

        #region Instance Data Members (Destination Options)

        string _addFileAttributes;
        [Category("3. 目标目录选项")]
        [Description("将给定的属性添加到复制的文件。 (/A+:[RASHCNET])")]
        [DisplayName("添加文件属性")]
        [ChoPropertyInfo("addFileAttributes", DefaultValue = "")]
        [Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        public string AddFileAttributes
        {
            get { return _addFileAttributes; }
            set
            {
                _addFileAttributes = value;
                NotifyPropertyChanged();
            }
        }

        string _removeFileAttributes;
        [Category("3. 目标目录选项")]
        [Description("从复制的文件中删除给定的属性。 (/A-:[RASHCNET])")]
        [DisplayName("删除文件属性")]
        [ChoPropertyInfo("removeFileAttributes", DefaultValue = "")]
        [Editor(typeof(FileAttributesEditor), typeof(FileAttributesEditor))]
        public string RemoveFileAttributes
        {
            get { return _removeFileAttributes; }
            set
            {
                _removeFileAttributes = value;
                NotifyPropertyChanged();
            }
        }

        bool _createFATFileNames;
        [Category("3. 目标目录选项")]
        [Description("仅使用 8.3 FAT 文件名创建目标文件。 (/FAT)")]
        [DisplayName("创建 FAT 文件名")]
        [ChoPropertyInfo("createFATFileNames")]
        public bool CreateFATFileNames
        {
            get { return _createFATFileNames; }
            set
            {
                _createFATFileNames = value;
                NotifyPropertyChanged();
            }
        }

        bool _createDirTree;
        [Category("3. 目标目录选项")]
        [Description("仅创建目录树和长度为零的文件。 (/CREATE)")]
        [DisplayName("创建目录树")]
        [ChoPropertyInfo("createDirTree")]
        public bool CreateDirTree
        {
            get { return _createDirTree; }
            set
            {
                _createDirTree = value;
                NotifyPropertyChanged();
            }
        }

        bool _compensateOneHourDSTTimeDiff;
        [Category("3. 目标目录选项")]
        [Description("弥补 1 小时的 DST 时间差。 (/DST)")]
        [DisplayName("弥补 1 小时的 DST 时间差")]
        [ChoPropertyInfo("compensateOneHourDSTTimeDiff")]
        public bool CompensateOneHourDSTTimeDiff
        {
            get { return _compensateOneHourDSTTimeDiff; }
            set
            {
                _compensateOneHourDSTTimeDiff = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Destination Options)

        #region Instance Data Members (Copy Options)

        bool _listOnly;
        [Category("4. 复制选项")]
        [Description("仅列出 - 不复制、添加时间戳或删除任何文件。 (/L)")]
        [DisplayName("仅列出")]
        [ChoPropertyInfo("listOnly")]
        public bool ListOnly
        {
            get { return _listOnly; }
            set
            {
                _listOnly = value;
                NotifyPropertyChanged();
            }
        }

        string _moveFilesAndDirectories;
        [Category("4. 复制选项")]
        [Description("移动文件(复制后从源中删除)。 (/MOV or /MOVE)")]
        [DisplayName("移动文件和目录")]
        [ChoPropertyInfo("moveFilesAndDirectories", DefaultValue = "None")]
        [Editor(typeof(FileMoveSelectionAttributesEditor), typeof(FileMoveSelectionAttributesEditor))]
        public string MoveFilesAndDirectories
        {
            get { return _moveFilesAndDirectories; }
            set
            {
                _moveFilesAndDirectories = value;
                NotifyPropertyChanged();
            }
        }

        bool _copySymbolicLinks;
        [Category("4. 复制选项")]
        [Description("将符号链接复制为链接而非链接目标。 (/SL)")]
        [DisplayName("复制符号链接")]
        [ChoPropertyInfo("copySymbolicLinks")]
        public bool CopySymbolicLinks
        {
            get { return _copySymbolicLinks; }
            set
            {
                _copySymbolicLinks = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyFilesRestartableMode;
        [Category("4. 复制选项")]
        [Description("在可重新启动模式下复制文件。 (/Z)")]
        [DisplayName("可重新启动模式")]
        [ChoPropertyInfo("copyFilesRestartableMode")]
        public bool CopyFilesRestartableMode
        {
            get { return _copyFilesRestartableMode; }
            set
            {
                _copyFilesRestartableMode = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyFilesBackupMode;
        [Category("4. 复制选项")]
        [Description("在备份模式下复制文件。 (/B)")]
        [DisplayName("备份模式")]
        [ChoPropertyInfo("copyFilesBackupMode")]
        public bool CopyFilesBackupMode
        {
            get { return _copyFilesBackupMode; }
            set
            {
                _copyFilesBackupMode = value;
                NotifyPropertyChanged();
            }
        }

        bool _unbufferredIOCopy;
        [Category("4. 复制选项")]
        [Description("复制时使用未缓冲的 I/O (推荐在复制大文件时使用)。 (/J)")]
        [DisplayName("非缓冲的 I/O")]
        [ChoPropertyInfo("unbufferredIOCopy")]
        public bool UnbufferredIOCopy
        {
            get { return _unbufferredIOCopy; }
            set
            {
                _unbufferredIOCopy = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyWithoutWindowsCopyOffload;
        [Category("4. 复制选项")]
        [Description("在不使用 Windows 复制卸载机制的情况下复制文件。 (/NOOFFLOAD)")]
        [DisplayName("不使用 Windows 复制卸载机制")]
        [ChoPropertyInfo("copyWithoutWindowsCopyOffload")]
        public bool CopyWithoutWindowsCopyOffload
        {
            get { return _copyWithoutWindowsCopyOffload; }
            set
            {
                _copyWithoutWindowsCopyOffload = value;
                NotifyPropertyChanged();
            }
        }

        bool _encrptFileEFSRawMode;
        [Category("4. 复制选项")]
        [Description("在 EFS RAW 模式下复制所有加密的文件。 (/EFSRAW)")]
        [DisplayName("EFS RAW 模式")]
        [ChoPropertyInfo("encrptFileEFSRawMode")]
        public bool EncrptFileEFSRawMode
        {
            get { return _encrptFileEFSRawMode; }
            set
            {
                _encrptFileEFSRawMode = value;
                NotifyPropertyChanged();
            }
        }

        bool _fixFileTimeOnFiles;
        [Category("4. 复制选项")]
        [Description("修复所有文件的文件时间，即使是跳过的文件。 (/TIMFIX)")]
        [DisplayName("修复文件时间")]
        [ChoPropertyInfo("fixFileTimeOnFiles")]
        public bool FixFileTimeOnFiles
        {
            get { return _fixFileTimeOnFiles; }
            set
            {
                _fixFileTimeOnFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeOlderFiles;
        [Category("4. 复制选项")]
        [Description("排除较旧的文件。 (/XO)")]
        [DisplayName("排除较旧的文件")]
        [ChoPropertyInfo("excludeOlderFiles")]
        public bool ExcludeOlderFiles
        {
            get { return _excludeOlderFiles; }
            set
            {
                _excludeOlderFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeChangedFiles;
        [Category("4. 复制选项")]
        [Description("排除已更改的文件。 (/XC)")]
        [DisplayName("排除已更改的文件")]
        [ChoPropertyInfo("excludeChangedFiles")]
        public bool ExcludeChangedFiles
        {
            get { return _excludeChangedFiles; }
            set
            {
                _excludeChangedFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeNewerFiles;
        [Category("4. 复制选项")]
        [Description("排除较新的文件。 (/XN)")]
        [DisplayName("排除较新的文件")]
        [ChoPropertyInfo("excludeNewerFiles")]
        public bool ExcludeNewerFiles
        {
            get { return _excludeNewerFiles; }
            set
            {
                _excludeNewerFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeExtraFilesAndDirs;
        [Category("4. 复制选项")]
        [Description("排除多余的文件和目录。 (/XX)")]
        [DisplayName("排除多余的文件和目录")]
        [ChoPropertyInfo("excludeExtraFilesAndDirs")]
        public bool ExcludeExtraFilesAndDirs
        {
            get { return _excludeExtraFilesAndDirs; }
            set
            {
                _excludeExtraFilesAndDirs = value;
                NotifyPropertyChanged();
            }
        }

        string _excludeFilesWithGivenNames;
        [Category("4. 复制选项")]
        [Description("排除与给定名称/路径/通配符匹配的文件。以 ; 分隔。 (/XF)")]
        [DisplayName("排除与给定名称/路径/通配符匹配的文件")]
        [ChoPropertyInfo("excludeFilesWithGivenNames", DefaultValue = "")]
        [Editor(typeof(ChoPropertyGridFilePicker), typeof(ChoPropertyGridFilePicker))]
        public string ExcludeFilesWithGivenNames
        {
            get { return _excludeFilesWithGivenNames; }
            set
            {
                _excludeFilesWithGivenNames = value;
                NotifyPropertyChanged();
            }
        }

        string _excludeDirsWithGivenNames;
        [Category("4. 复制选项")]
        [Description("排除与给定名称/路径匹配的目录。以 ; 分隔。 (/XD)")]
        [DisplayName("排除与给定名称/路径匹配的目录")]
        [ChoPropertyInfo("excludeDirsWithGivenNames", DefaultValue = "")]
        [Editor(typeof(ChoPropertyGridFolderPicker), typeof(ChoPropertyGridFolderPicker))]
        public string ExcludeDirsWithGivenNames
        {
            get { return _excludeDirsWithGivenNames; }
            set
            {
                _excludeDirsWithGivenNames = value;
                NotifyPropertyChanged();
            }
        }

        string _includeFilesWithGivenAttributes;
        [Category("4. 复制选项")]
        [Description("仅包含具有任意给定属性集的文件。 (/IA:[RASHCNETO])")]
        [DisplayName("仅包含具有任意给定属性集的文件")]
        [ChoPropertyInfo("includeFilesWithGivenAttributes", DefaultValue = "")]
        [Editor(typeof(FileSelectionAttributesEditor), typeof(FileSelectionAttributesEditor))]
        public string IncludeFilesWithGivenAttributes
        {
            get { return _includeFilesWithGivenAttributes; }
            set
            {
                _includeFilesWithGivenAttributes = value;
                NotifyPropertyChanged();
            }
        }

        string _excludeFilesWithGivenAttributes;
        [Category("4. 复制选项")]
        [Description("排除具有任意给定属性集的文件。 (/XA:[RASHCNETO])")]
        [DisplayName("排除具有任意给定属性集的文件")]
        [ChoPropertyInfo("excludeFilesWithGivenAttributes", DefaultValue = "")]
        [Editor(typeof(FileSelectionAttributesEditor), typeof(FileSelectionAttributesEditor))]
        public string ExcludeFilesWithGivenAttributes
        {
            get { return _excludeFilesWithGivenAttributes; }
            set
            {
                _excludeFilesWithGivenAttributes = value;
                NotifyPropertyChanged();
            }
        }

        bool _overrideModifiedFiles;
        [Category("4. 复制选项")]
        [Description("包含已修改的文件(更改时间不同)。否则，默认情况下不会复制相同的文件。 (/IM)")]
        [DisplayName("覆盖已修改的文件")]
        [ChoPropertyInfo("overrideModifiedFiles")]
        public bool OverrideModifiedFiles
        {
            get { return _overrideModifiedFiles; }
            set
            {
                _overrideModifiedFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _includeSameFiles;
        [Category("4. 复制选项")]
        [Description("包含相同文件。覆盖文件，即使它们已经相同。 (/IS)")]
        [DisplayName("包含相同文件")]
        [ChoPropertyInfo("includeSameFiles")]
        public bool IncludeSameFiles
        {
            get { return _includeSameFiles; }
            set
            {
                _includeSameFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _includeTweakedFiles;
        [Category("4. 复制选项")]
        [Description("包含已调整的文件。 (/IT)")]
        [DisplayName("包含已调整的文件")]
        [ChoPropertyInfo("includeTweakedFiles")]
        public bool IncludeTweakedFiles
        {
            get { return _includeTweakedFiles; }
            set
            {
                _includeTweakedFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeJunctionPoints;
        [Category("4. 复制选项")]
        [Description("排除(文件和目录的)符号链接和接合点。(通常默认不排除)。 (/XJ)")]
        [DisplayName("排除(文件和目录的)符号链接和接合点")]
        [ChoPropertyInfo("excludeJunctionPoints")]
        public bool ExcludeJunctionPoints
        {
            get { return _excludeJunctionPoints; }
            set
            {
                _excludeJunctionPoints = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeJunctionPointsForDirs;
        [Category("4. 复制选项")]
        [Description("排除目录的符号链接和接合点。 (/XJD)")]
        [DisplayName("排除目录的符号链接和接合点")]
        [ChoPropertyInfo("excludeJunctionPointsForDirs")]
        public bool ExcludeJunctionPointsForDirs
        {
            get { return _excludeJunctionPointsForDirs; }
            set
            {
                _excludeJunctionPointsForDirs = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeJunctionPointsForFiles;
        [Category("4. 复制选项")]
        [Description("排除文件的符号链接和接合点。 (/XJF)")]
        [DisplayName("排除目录的符号链接和接合点")]
        [ChoPropertyInfo("excludeJunctionPointsForFiles")]
        public bool ExcludeJunctionPointsForFiles
        {
            get { return _excludeJunctionPointsForFiles; }
            set
            {
                _excludeJunctionPointsForFiles = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesBiggerThanNBytes;
        [Category("4. 复制选项")]
        [Description("最大的文件大小 - 排除大于 n 字节的文件。 (/MAX:n)。")]
        [DisplayName("排除大于 n 字节的文件")]
        [ChoPropertyInfo("excludeFilesBiggerThanNBytes")]
        public uint ExcludeFilesBiggerThanNBytes
        {
            get { return _excludeFilesBiggerThanNBytes; }
            set
            {
                _excludeFilesBiggerThanNBytes = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesSmallerThanNBytes;
        [Category("4. 复制选项")]
        [Description("最小的文件大小 - 排除小于 n 字节的文件。 (/MIN:n)")]
        [DisplayName("排除小于 n 字节的文件")]
        [ChoPropertyInfo("excludeFilesSmallerThanNBytes")]
        public uint ExcludeFilesSmallerThanNBytes
        {
            get { return _excludeFilesSmallerThanNBytes; }
            set
            {
                _excludeFilesSmallerThanNBytes = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesUnusedSinceNDays;
        [Category("4. 复制选项")]
        [Description("最大的最后访问日期 - 排除自 n 以来未使用的文件。 (/MAXLAD:n)")]
        [DisplayName("排除自 n 以来未使用的文件")]
        [ChoPropertyInfo("excludeFilesUnusedSinceNDays")]
        public uint ExcludeFilesUnusedSinceNDays
        {
            get { return _excludeFilesUnusedSinceNDays; }
            set
            {
                _excludeFilesUnusedSinceNDays = value;
                NotifyPropertyChanged();
            }
        }

        uint _excludeFilesUsedSinceNDays;
        [Category("4. 复制选项")]
        [Description("最小的最后访问日期 - 排除自 n 以来使用的文件。 (If n < 1900 then n = n days, else n = YYYYMMDD date)。 (/MINLAD:n)")]
        [DisplayName("排除自 n 以来使用的文件")]
        [ChoPropertyInfo("excludeFilesUsedSinceNDays")]
        public uint ExcludeFilesUsedSinceNDays
        {
            get { return _excludeFilesUsedSinceNDays; }
            set
            {
                _excludeFilesUsedSinceNDays = value;
                NotifyPropertyChanged();
            }
        }

        bool _mirrorDirTree;
        [Category("4. 复制选项")]
        [Description("镜像目录树(等同于 /E 加 /PURGE)。 (/MIR)")]
        [DisplayName("镜像目录树")]
        [ChoPropertyInfo("mirrorDirTree")]
        public bool MirrorDirTree
        {
            get { return _mirrorDirTree; }
            set
            {
                _mirrorDirTree = value;
                NotifyPropertyChanged();
            }
        }

        bool _delDestFileDirIfNotExistsInSource;
        [Category("4. 复制选项")]
        [Description("删除源中不再存在的目标文件/目录。 (/PURGE)")]
        [DisplayName("删除源中不再存在的目标文件/目录")]
        [ChoPropertyInfo("delDestFileDirIfNotExistsInSource")]
        public bool DelDestFileDirIfNotExistsInSource
        {
            get { return _delDestFileDirIfNotExistsInSource; }
            set
            {
                _delDestFileDirIfNotExistsInSource = value;
                NotifyPropertyChanged();
            }
        }

        bool _excludeLonelyFilesAndDirs;
        [Category("4. 复制选项")]
        [Description("排除孤立的文件和目录。 (/XL)")]
        [DisplayName("排除孤立的文件和目录")]
        [ChoPropertyInfo("excludeLonelyFilesAndDirs")]
        public bool ExcludeLonelyFilesAndDirs
        {
            get { return _excludeLonelyFilesAndDirs; }
            set
            {
                _excludeLonelyFilesAndDirs = value;
                NotifyPropertyChanged();
            }
        }

        bool _fixFileSecurityOnFiles;
        [Category("4. 复制选项")]
        [Description("修复所有文件的文件安全性，即使是跳过的文件。 (/SECFIX)")]
        [DisplayName("修复文件安全性")]
        [ChoPropertyInfo("fixFileSecurityOnFiles")]
        public bool FixFileSecurityOnFiles
        {
            get { return _fixFileSecurityOnFiles; }
            set
            {
                _fixFileSecurityOnFiles = value;
                NotifyPropertyChanged();
            }
        }

        bool _fallbackCopyFilesMode;
        [Category("4. 复制选项")]
        [Description("使用可重新启动模式；如果拒绝访问，请使用备份模式。 (/ZB)")]
        [DisplayName("回退文件复制模式")]
        [ChoPropertyInfo("fallbackCopyFilesMode")]
        public bool FallbackCopyFilesMode
        {
            get { return _fallbackCopyFilesMode; }
            set
            {
                _fallbackCopyFilesMode = value;
                NotifyPropertyChanged();
            }
        }

        uint _interPacketGapInMS;
        [Category("4. 复制选项")]
        [Description("程序包间的间距(ms)，以释放低速线路上的带宽。 (/IPG:n)")]
        [DisplayName("包间距(ms)")]
        [ChoPropertyInfo("interPacketGapInMS")]
        public uint InterPacketGapInMS
        {
            get { return _interPacketGapInMS; }
            set
            {
                _interPacketGapInMS = value;
                NotifyPropertyChanged();
            }
        }

        private uint _multithreadCopy;
        [Category("4. 复制选项")]
        [Description("使用 n 个线程进行多线程复制(默认值为 8)。n 必须至少为 1，但不得大于 128。该选项与 /IPG 和 /EFSRAW 选项不兼容。使用 /LOG 选项重定向输出以便获得最佳性能。 (/MT[:n])")]
        [DisplayName("多线程复制")]
        [ChoPropertyInfo("multithreadCopy", DefaultValue = "0")]
        public uint MultithreadCopy
        {
            get { return _multithreadCopy; }
            set
            {
                if (value < 1 || value > 128)
                    _multithreadCopy = 0;
                else
                    _multithreadCopy = value;
                NotifyPropertyChanged();
            }
        }

        bool _copyNODirInfo;
        [Category("4. 复制选项")]
        [Description("不复制任何目录信息(默认情况下，执行 /DCOPY:DA)。 (/NODCOPY)")]
        [DisplayName("不复制目录信息")]
        [ChoPropertyInfo("copyNODirInfo")]
        public bool CopyNODirInfo
        {
            get { return _copyNODirInfo; }
            set
            {
                _copyNODirInfo = value;
                NotifyPropertyChanged();
            }
        }
        #endregion Instance Data Members (Copy Options)

        #region Instance Data Members (Monitoring Options)

        const string DefaultNoOfRetries = "1000000";

        uint _noOfRetries;
        [Category("5. 监视选项")]
        [Description("失败副本的重试次数: 默认为 1 百万。 (/R:n)")]
        [DisplayName("重试次数")]
        [ChoPropertyInfo("noOfRetries", DefaultValue = DefaultNoOfRetries)]
        public uint NoOfRetries
        {
            get { return _noOfRetries; }
            set
            {
                _noOfRetries = value;
                NotifyPropertyChanged();
            }
        }

        const string DefaultWaitTimeBetweenRetries = "30";

        uint _waitTimeBetweenRetries;
        [Category("5. 监视选项")]
        [Description("两次重试间的等待时间: 默认为 30 秒。 (/W:n)")]
        [DisplayName("重试等待时间")]
        [ChoPropertyInfo("waitTimeBetweenRetries", DefaultValue = DefaultWaitTimeBetweenRetries)]
        public uint WaitTimeBetweenRetries
        {
            get { return _waitTimeBetweenRetries; }
            set
            {
                _waitTimeBetweenRetries = value;
                NotifyPropertyChanged();
            }
        }

        bool _saveRetrySettingsToRegistry;
        [Category("5. 监视选项")]
        [Description("将 /R:n 和 /W:n 保存为注册表的默认设置。 (/REG)")]
        [DisplayName("保存重试参数到注册表")]
        [ChoPropertyInfo("saveRetrySettingsToRegistry")]
        public bool SaveRetrySettingsToRegistry
        {
            get { return _saveRetrySettingsToRegistry; }
            set
            {
                _saveRetrySettingsToRegistry = value;
                NotifyPropertyChanged();
            }
        }

        bool _waitForSharenames;
        [Category("5. 监视选项")]
        [Description("等待定义共享名称(重试错误 67)。 (/TBD)")]
        [DisplayName("等待定义共享名称")]
        [ChoPropertyInfo("waitForSharenames")]
        public bool WaitForSharenames
        {
            get { return _waitForSharenames; }
            set
            {
                _waitForSharenames = value;
                NotifyPropertyChanged();
            }
        }

        uint _runAgainWithNoChangesSeen;
        [Category("5. 监视选项")]
        [Description("监视源；发现多于 n 个更改时再次运行。 (/MON:n)")]
        [DisplayName("多于 n 个更改时再次运行")]
        [ChoPropertyInfo("runAgainWithNoChangesSeen")]
        public uint RunAgainWithNoChangesSeen
        {
            get { return _runAgainWithNoChangesSeen; }
            set
            {
                _runAgainWithNoChangesSeen = value;
                NotifyPropertyChanged();
            }
        }

        uint _runAgainWithChangesSeenInMin;
        [Category("5. 监视选项")]
        [Description("监视源；如果更改，在 m 分钟时间后再次运行。 (/MOT:m)")]
        [DisplayName("如果更改，在 m 分钟时间后再次运行")]
        [ChoPropertyInfo("runAgainWithChangesSeenInMin")]
        public uint RunAgainWithChangesSeenInMin
        {
            get { return _runAgainWithChangesSeenInMin; }
            set
            {
                _runAgainWithChangesSeenInMin = value;
                NotifyPropertyChanged();
            }
        }

        bool _checkRunHourPerFileBasis;
        [Category("5. 监视选项")]
        [Description("基于每个文件(而不是每个步骤)来检查运行小时数。 (/PF)")]
        [DisplayName("基于文件检查运行小时数")]
        [ChoPropertyInfo("checkRunHourPerFileBasis")]
        public bool CheckRunHourPerFileBasis
        {
            get { return _checkRunHourPerFileBasis; }
            set
            {
                _checkRunHourPerFileBasis = value;
                NotifyPropertyChanged();
            }
        }

        #endregion Instance Data Members (Monitoring Options)

        #region Instance Data Members (Scheduling Options)

        private TimeSpan _runHourStartTime;
        [Category("6. 定时选项")]
        [Description("Run Hours StartTime, when new copies may be started after then. (/RH:hhmm-hhmm).")]
        [DisplayName("RunHourStartTime")]
        [ChoPropertyInfo("runHourStartTime")]
        [XmlIgnore]
        public TimeSpan RunHourStartTime
        {
            get { return _runHourStartTime; }
            set { _runHourStartTime = value; NotifyPropertyChanged(); }
        }

        [Browsable(false)]
        public long RunHourStartTimeTicks
        {
            get { return _runHourStartTime.Ticks; }
            set { _runHourStartTime = new TimeSpan(value); }
        }

        private TimeSpan _runHourEndTime;
        [Category("6. 定时选项")]
        [Description("Run Hours EndTime, when new copies may be Ended before then. (/RH:hhmm-hhmm).")]
        [DisplayName("RunHourEndTime")]
        [ChoPropertyInfo("runHourEndTime")]
        [XmlIgnore]
        public TimeSpan RunHourEndTime
        {
            get { return _runHourEndTime; }
            set { _runHourEndTime = value; NotifyPropertyChanged(); }
        }

        [Browsable(false)]
        public long RunHourEndTimeTicks
        {
            get { return _runHourEndTime.Ticks; }
            set { _runHourEndTime = new TimeSpan(value); }
        }


        #endregion Instance Data Members (Scheduling Options)

        #region Instance Data Members (Logging Options)

        private bool _noProgress;
        [Category("7. 日志选项")]
        [Description("无进度 - 不显示已复制的百分比。禁止显示进度信息。当输出重定向到文件时，这很有用。 (/NP)")]
        [DisplayName("无进度")]
        [ChoPropertyInfo("noProgress")]
        public bool NoProgress
        {
            get { return _noProgress; }
            set { _noProgress = value; NotifyPropertyChanged(); }
        }

        private bool _unicode;
        [Category("7. 日志选项")]
        [Description("以 UNICODE 方式输出状态。 (/unicode)")]
        [DisplayName("Unicode")]
        [ChoPropertyInfo("unicode")]
        public bool Unicode
        {
            get { return _unicode; }
            set { _unicode = value; NotifyPropertyChanged(); }
        }

        private string _outputLogFilePath;
        [Category("7. 日志选项")]
        [Description("将状态输出到日志文件(覆盖现有日志)。 (/LOG:file)")]
        [DisplayName("输出日志文件路径")]
        [ChoPropertyInfo("outputLogFilePath", DefaultValue = "")]
        public string OutputLogFilePath
        {
            get { return _outputLogFilePath; }
            set { _outputLogFilePath = value; NotifyPropertyChanged(); }
        }

        private string _unicodeOutputLogFilePath;
        [Category("7. 日志选项")]
        [Description("以 UNICODE 方式将状态输出到日志文件(覆盖现有日志)。 (/UNILOG:file)")]
        [DisplayName("输出日志文件路径（Unicode）")]
        [ChoPropertyInfo("unicodeOutputLogFilePath", DefaultValue = "")]
        public string UnicodeOutputLogFilePath
        {
            get { return _unicodeOutputLogFilePath; }
            set { _unicodeOutputLogFilePath = value; NotifyPropertyChanged(); }
        }

        private string _appendOutputLogFilePath;
        [Category("7. 日志选项")]
        [Description("将状态输出到日志文件(附加到现有日志中)。 (/LOG+:file)")]
        [DisplayName("追加日志文件路径")]
        [ChoPropertyInfo("appendOutputLogFilePath", DefaultValue = "")]
        public string AppendOutputLogFilePath
        {
            get { return _appendOutputLogFilePath; }
            set { _appendOutputLogFilePath = value; NotifyPropertyChanged(); }
        }

        private string _appendUnicodeOutputLogFilePath;
        [Category("7. 日志选项")]
        [Description("以 UNICODE 方式将状态输出到日志文件(附加到现有日志中)。 (/UNILOG+:file)")]
        [DisplayName("追加日志文件路径（Unicode）")]
        [ChoPropertyInfo("appendUnicodeOutputLogFilePath", DefaultValue = "")]
        public string AppendUnicodeOutputLogFilePath
        {
            get { return _appendUnicodeOutputLogFilePath; }
            set { _appendUnicodeOutputLogFilePath = value; NotifyPropertyChanged(); }
        }

        private bool _includeSourceFileTimestamp;
        [Category("7. 日志选项")]
        [Description("在输出中包含源文件的时间戳。 (/TS)")]
        [DisplayName("包含源文件时间戳")]
        [ChoPropertyInfo("includeSourceFileTimestamp")]
        public bool IncludeSourceFileTimestamp
        {
            get { return _includeSourceFileTimestamp; }
            set { _includeSourceFileTimestamp = value; NotifyPropertyChanged(); }
        }

        private bool _includeFullPathName;
        [Category("7. 日志选项")]
        [Description("在输出中包含文件的完整路径名称。 (/FP)")]
        [DisplayName("包含完整路径名称")]
        [ChoPropertyInfo("includeFullPathName")]
        public bool IncludeFullPathName
        {
            get { return _includeFullPathName; }
            set { _includeFullPathName = value; NotifyPropertyChanged(); }
        }

        private bool _noFileSizeLog;
        [Category("7. 日志选项")]
        [Description("无大小 - 不记录文件大小。 (/NS)")]
        [DisplayName("不记录文件大小")]
        [ChoPropertyInfo("noFileSizeLog")]
        public bool NoFileSizeLog
        {
            get { return _noFileSizeLog; }
            set { _noFileSizeLog = value; NotifyPropertyChanged(); }
        }

        private bool _noFileClassLog;
        [Category("7. 日志选项")]
        [Description("无类别 - 不记录文件类别。 (/NC)")]
        [DisplayName("不记录文件类别")]
        [ChoPropertyInfo("noFileClassLog")]
        public bool NoFileClassLog
        {
            get { return _noFileClassLog; }
            set { _noFileClassLog = value; NotifyPropertyChanged(); }
        }

        private bool _noFileNameLog;
        [Category("7. 日志选项")]
        [Description("无文件列表 - 不记录文件名。 隐藏文件名。尽管如此，处理失败的文件名仍然会被输出到日志中。 如果参数 /L 被省略，始终记录任何被删除或将要被删除的文件。 (/NFL).")]
        [DisplayName("不记录文件名")]
        [ChoPropertyInfo("noFileNameLog")]
        public bool NoFileNameLog
        {
            get { return _noFileNameLog; }
            set { _noFileNameLog = value; NotifyPropertyChanged(); }
        }

        private bool _noDirListLog;
        [Category("7. 日志选项")]
        [Description("无目录列表 - 不记录目录名称。 隐藏输出的目录列表。输出完整的文件路径名称有助于追踪存在问题的文件。 (/NDL).")]
        [DisplayName("不记录目录名称")]
        [ChoPropertyInfo("noDirListLog")]
        public bool NoDirListLog
        {
            get { return _noDirListLog; }
            set { _noDirListLog = value; NotifyPropertyChanged(); }
        }

        //[Category("日志选项")]
        //[Description("输出到控制台窗口和日志文件。 (/TEE)")]
        //[DisplayName("输出到控制台窗口和日志文件")]
        //[ChoPropertyInfo("noDirListLog")]
        //public bool NoDirListLog
        //{
        //    get;
        //    set;
        //}

        private bool _noJobHeader;
        [Category("7. 日志选项")]
        [Description("没有作业标头。 (/NJH)")]
        [DisplayName("无作业标头")]
        [ChoPropertyInfo("noJobHeader")]
        public bool NoJobHeader
        {
            get { return _noJobHeader; }
            set { _noJobHeader = value; NotifyPropertyChanged(); }
        }

        private bool _noJobSummary;
        [Category("7. 日志选项")]
        [Description("没有作业摘要。 (/NJS)")]
        [DisplayName("无作业摘要")]
        [ChoPropertyInfo("noJobSummary")]
        public bool NoJobSummary
        {
            get { return _noJobSummary; }
            set { _noJobSummary = value; NotifyPropertyChanged(); }
        }

        private bool _printByteSizes;
        [Category("7. 日志选项")]
        [Description("打印字节大小。 (/BYTES)")]
        [DisplayName("打印字节大小")]
        [ChoPropertyInfo("printByteSizes")]
        public bool PrintByteSizes
        {
            get { return _printByteSizes; }
            set { _printByteSizes = value; NotifyPropertyChanged(); }
        }

        private bool _reportExtraFiles;
        [Category("7. 日志选项")]
        [Description("报告所有多余的文件，而不只是选中的文件。 (/X)")]
        [DisplayName("报告多余文件")]
        [ChoPropertyInfo("reportExtraFiles")]
        public bool ReportExtraFiles
        {
            get { return _reportExtraFiles; }
            set { _reportExtraFiles = value; NotifyPropertyChanged(); }
        }

        private bool _verboseOutput;
        [Category("7. 日志选项")]
        [Description("生成详细输出，同时显示跳过的文件。 (/V)")]
        [DisplayName("详细输出")]
        [ChoPropertyInfo("verboseOutput")]
        public bool VerboseOutput
        {
            get { return _verboseOutput; }
            set { _verboseOutput = value; NotifyPropertyChanged(); }
        }

        private bool _showEstTimeOfArrival;
        [Category("7. 日志选项")]
        [Description("显示复制文件的预期到达时间。 (/ETA)")]
        [DisplayName("显示预期到达时间")]
        [ChoPropertyInfo("showEstTimeOfArrival")]
        public bool ShowEstTimeOfArrival
        {
            get { return _showEstTimeOfArrival; }
            set { _showEstTimeOfArrival = value; NotifyPropertyChanged(); }
        }

        private bool _showDebugVolumeInfo;
        [Category("7. 日志选项")]
        [Description("显示调试卷信息。 (/DEBUG)")]
        [DisplayName("显示调试卷信息")]
        [ChoPropertyInfo("showDebugVolumeInfo")]
        public bool ShowDebugVolumeInfo
        {
            get { return _showDebugVolumeInfo; }
            set { _showDebugVolumeInfo = value; NotifyPropertyChanged(); }
        }

        #endregion Instance Data Members (Logging Options)

        #region Commented

        //[Category("Copy Options")]
        //[Description("Move files (delete from source after copying). (/MOV).")]
        //[DisplayName("MoveFiles")]
        //[ChoPropertyInfo("moveFiles")]
        //public bool MoveFiles
        //{
        //    get;
        //    set;
        //}

        //[Category("Copy Options")]
        //[Description("Move files and dirs (delete from source after copying). (/MOVE).")]
        //[DisplayName("MoveFilesNDirs")]
        //[ChoPropertyInfo("moveFilesNDirs")]
        //public bool MoveFilesNDirs
        //{
        //    get;
        //    set;
        //}

        #endregion Commented

        public void Reset()
        {
            Copy(DefaultInstance, this);
            //ChoObject.ResetObject(this);
            //Persist();
            MultithreadCopy = 0;
            Precommands = null;
            Postcommands = null;
            Comments = null;
        }
        
        public string GetCmdLineText()
        {
            return "{0} {1}".FormatString(RoboCopyFilePath, GetCmdLineParams());
        }

        public string GetCmdLineTextEx()
        {
            return "{0} {1} {2} {3}".FormatString(RoboCopyFilePath, GetCmdLineParams(), GetExCmdLineParams(), Comments);
        }

        string DirSafeguard(string path)
        {
            // Escape the last '\' from the path if it is not escaped yet.
            if (path.Length > 1 && path.Last() == '\\' && (path[path.Length - 2] != '\\'))
                path += '\\';
            return path;
        }

        internal string GetExCmdLineParams()
        {
            StringBuilder cmdText = new StringBuilder();

            if (!Postcommands.IsNullOrWhiteSpace())
                cmdText.Append(Postcommands);
            if (!Precommands.IsNullOrWhiteSpace())
                cmdText.Append(Precommands);

            return cmdText.ToString();
        }
        internal string GetCmdLineParams(string sourceDirectory = null, string destDirectory = null)
        {
            StringBuilder cmdText = new StringBuilder();
            
            if (!sourceDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", DirSafeguard(sourceDirectory));
            else if (!SourceDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", DirSafeguard(SourceDirectory));

            if (!destDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", DirSafeguard(destDirectory));
            else if (!DestDirectory.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" \"{0}\"", DirSafeguard(DestDirectory));

            if (!Files.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" {0}", Files);
            else
                cmdText.Append("*.*");

            //Copy Options
            if (CopyNoEmptySubDirectories)
                cmdText.Append(" /S");
            if (CopySubDirectories)
                cmdText.Append(" /E");
            if (OnlyCopyNLevels > 0)
                cmdText.AppendFormat(" /LEV:{0}", OnlyCopyNLevels);
            if (CopyFilesRestartableMode)
                cmdText.Append(" /Z");
            if (CopyFilesBackupMode)
                cmdText.Append(" /B");
            if (FallbackCopyFilesMode)
                cmdText.Append(" /ZB");
            if (UnbufferredIOCopy)
                cmdText.Append(" /J");

            if (EncrptFileEFSRawMode)
                cmdText.Append(" /EFSRAW");
            if (!CopyFlags.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /COPY:{0}", (from f in CopyFlags.SplitNTrim()
                                                    where !f.IsNullOrWhiteSpace()
                                                    select ((ChoCopyFlags)Enum.Parse(typeof(ChoCopyFlags), f)).ToDescription()).Join(""));
            }
            if (CopyDirTimestamp)
                cmdText.Append(" /DCOPY:T");
            if (CopyFilesWithSecurity)
                cmdText.Append(" /SEC");

            if (CopyFilesWithFileInfo)
                cmdText.Append(" /COPYALL");
            if (CopyFilesWithNoFileInfo)
                cmdText.Append(" /NOCOPY");
            if (FixFileSecurityOnFiles)
                cmdText.Append(" /SECFIX");
            if (FixFileTimeOnFiles)
                cmdText.Append(" /TIMFIX");

            if (DelDestFileDirIfNotExistsInSource)
                cmdText.Append(" /PURGE");
            if (MirrorDirTree)
                cmdText.Append(" /MIR");
            //if (MoveFiles)
            //    cmdText.Append(" /MOV");
            //if (MoveFilesNDirs)
            //    cmdText.Append(" /MOVE");
            if (!MoveFilesAndDirectories.IsNullOrWhiteSpace())
            {
                ChoFileMoveAttributes value = ChoFileMoveAttributes.None;
                if (Enum.TryParse<ChoFileMoveAttributes>(MoveFilesAndDirectories, out value))
                {
                    switch (value)
                    {
                        case ChoFileMoveAttributes.MoveFilesOnly:
                            cmdText.Append(" /MOV");
                            break;
                        case ChoFileMoveAttributes.MoveDirectoriesAndFiles:
                            cmdText.Append(" /MOVE");
                            break;
                        default:
                            break;
                    }
                }
            }
            if (!AddFileAttributes.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /A+:{0}", (from f in AddFileAttributes.SplitNTrim()
                                                  where !f.IsNullOrWhiteSpace()
                                                  select ((ChoFileAttributes)Enum.Parse(typeof(ChoFileAttributes), f)).ToDescription()).Join(""));
            }
            if (!RemoveFileAttributes.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /A-:{0}", (from f in RemoveFileAttributes.SplitNTrim()
                                                  where !f.IsNullOrWhiteSpace()
                                                  select ((ChoFileAttributes)Enum.Parse(typeof(ChoFileAttributes), f)).ToDescription()).Join(""));
            }
            if (CreateDirTree)
                cmdText.Append(" /CREATE");
            if (CreateFATFileNames)
                cmdText.Append(" /FAT");
            if (TurnOffLongPath)
                cmdText.Append(" /256");

            if (RunAgainWithNoChangesSeen > 0)
                cmdText.AppendFormat(" /MON:{0}", RunAgainWithNoChangesSeen);
            if (RunAgainWithChangesSeenInMin > 0)
                cmdText.AppendFormat(" /MOT:{0}", RunAgainWithChangesSeenInMin);
            if (RunHourStartTime != TimeSpan.Zero
                && RunHourEndTime != TimeSpan.Zero
                && RunHourStartTime < RunHourEndTime)
                cmdText.AppendFormat(" /RH:{0}-{1}", RunHourStartTime.ToString("hhmm"), RunHourEndTime.ToString("hhmm"));
            if (CheckRunHourPerFileBasis)
                cmdText.Append(" /PF");
            if (InterPacketGapInMS > 0)
                cmdText.AppendFormat(" /IPG:{0}", InterPacketGapInMS);
            if (CopySymbolicLinks)
                cmdText.Append(" /SL");
            if (MultithreadCopy > 0)
                cmdText.AppendFormat(" /MT:{0}", MultithreadCopy);
            if (CopyNODirInfo)
                cmdText.Append(" /NODCOPY");
            if (CopyWithoutWindowsCopyOffload)
                cmdText.Append(" /NOOFFLOAD");
            if (OverrideModifiedFiles)
                cmdText.Append(" /IM");
            //File Selection Options
            if (CopyOnlyFilesWithArchiveAttributes)
                cmdText.Append(" /A");
            if (CopyOnlyFilesWithArchiveAttributesAndReset)
                cmdText.Append(" /M");
            if (!IncludeFilesWithGivenAttributes.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /IA:{0}", (from f in IncludeFilesWithGivenAttributes.SplitNTrim()
                                                  where !f.IsNullOrWhiteSpace()
                                                  select ((ChoFileSelectionAttributes)Enum.Parse(typeof(ChoFileSelectionAttributes), f)).ToDescription()).Join(""));
            }
            if (!ExcludeFilesWithGivenAttributes.IsNullOrWhiteSpace())
            {
                cmdText.AppendFormat(" /XA:{0}", (from f in ExcludeFilesWithGivenAttributes.SplitNTrim()
                                                  where !f.IsNullOrWhiteSpace()
                                                  select ((ChoFileSelectionAttributes)Enum.Parse(typeof(ChoFileSelectionAttributes), f)).ToDescription()).Join(""));
            }
            if (!ExcludeFilesWithGivenNames.IsNullOrWhiteSpace())
                cmdText.AppendFormat(@" /XF {0}", String.Join(" ", ExcludeFilesWithGivenNames.Split(";").Select(f => f).Select(f => f.Contains(" ") ? String.Format(@"""{0}""", f) : f)));
            if (!ExcludeDirsWithGivenNames.IsNullOrWhiteSpace())
                cmdText.AppendFormat(@" /XD {0}", String.Join(" ", ExcludeDirsWithGivenNames.Split(";").Select(f => f).Select(f => f.Contains(" ") ? String.Format(@"""{0}""", f) : f)));
            if (ExcludeChangedFiles)
                cmdText.Append(" /XC");
            if (ExcludeNewerFiles)
                cmdText.Append(" /XN");
            if (ExcludeOlderFiles)
                cmdText.Append(" /XO");
            if (ExcludeExtraFilesAndDirs)
                cmdText.Append(" /XX");
            if (ExcludeLonelyFilesAndDirs)
                cmdText.Append(" /XL");
            if (IncludeSameFiles)
                cmdText.Append(" /IS");
            if (IncludeTweakedFiles)
                cmdText.Append(" /IT");

            if (ExcludeFilesBiggerThanNBytes > 0)
                cmdText.AppendFormat(" /MAX:{0}", ExcludeFilesBiggerThanNBytes);
            if (ExcludeFilesSmallerThanNBytes > 0)
                cmdText.AppendFormat(" /MIN:{0}", ExcludeFilesSmallerThanNBytes);

            if (ExcludeFilesOlderThanNDays > 0)
                cmdText.AppendFormat(" /MAXAGE:{0}", ExcludeFilesOlderThanNDays);
            if (ExcludeFilesNewerThanNDays > 0)
                cmdText.AppendFormat(" /MINAGE:{0}", ExcludeFilesNewerThanNDays);
            if (ExcludeFilesUnusedSinceNDays > 0)
                cmdText.AppendFormat(" /MAXLAD:{0}", ExcludeFilesUnusedSinceNDays);
            if (ExcludeFilesUsedSinceNDays > 0)
                cmdText.AppendFormat(" /MINLAD:{0}", ExcludeFilesUsedSinceNDays);

            if (ExcludeJunctionPoints)
                cmdText.Append(" /XJ");
            if (AssumeFATFileTimes)
                cmdText.Append(" /FFT");
            if (CompensateOneHourDSTTimeDiff)
                cmdText.Append(" /DST");
            if (ExcludeJunctionPointsForDirs)
                cmdText.Append(" /XJD");
            if (ExcludeJunctionPointsForFiles)
                cmdText.Append(" /XJF");

            //Retry Options
            if (NoOfRetries.ToString() != DefaultNoOfRetries && NoOfRetries >= 0)
                cmdText.AppendFormat(" /R:{0}", NoOfRetries);
            if (WaitTimeBetweenRetries.ToString() != DefaultWaitTimeBetweenRetries && WaitTimeBetweenRetries >= 0)
                cmdText.AppendFormat(" /W:{0}", WaitTimeBetweenRetries);
            if (SaveRetrySettingsToRegistry)
                cmdText.Append(" /REG");
            if (WaitForSharenames)
                cmdText.Append(" /TBD");

            //Logging Options
            if (ListOnly)
                cmdText.Append(" /L");
            if (ReportExtraFiles)
                cmdText.Append(" /X");
            if (VerboseOutput)
                cmdText.Append(" /V");
            if (IncludeSourceFileTimestamp)
                cmdText.Append(" /TS");
            if (IncludeFullPathName)
                cmdText.Append(" /FP");
            if (PrintByteSizes)
                cmdText.Append(" /BYTES");
            if (NoFileSizeLog)
                cmdText.Append(" /NS");
            if (NoFileClassLog)
                cmdText.Append(" /NC");
            if (NoFileNameLog)
                cmdText.Append(" /NFL");
            if (NoDirListLog)
                cmdText.Append(" /NDL");
            if (NoProgress)
                cmdText.Append(" /NP");
            if (Unicode)
                cmdText.Append(" /unicode");
            if (ShowEstTimeOfArrival)
                cmdText.Append(" /ETA");
            if (ShowDebugVolumeInfo)
                cmdText.Append(" /DEBUG");
            if (!OutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /LOG:\"{0}\"", OutputLogFilePath);
            if (!AppendOutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /LOG+:\"{0}\"", AppendOutputLogFilePath);
            if (!UnicodeOutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /UNILOG:\"{0}\"", UnicodeOutputLogFilePath);
            if (!AppendUnicodeOutputLogFilePath.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" /UNILOG+:\"{0}\"", AppendUnicodeOutputLogFilePath);
            if (NoJobHeader)
                cmdText.Append(" /NJH");
            if (NoJobSummary)
                cmdText.Append(" /NJS");

            if (!AdditionalParams.IsNullOrWhiteSpace())
                cmdText.AppendFormat(" {0}", AdditionalParams);

            return cmdText.ToString();
        }

        //TODO
        //protected override void OnAfterConfigurationObjectLoaded()
        //{
        //    if (RoboCopyFilePath.IsNullOrWhiteSpace())
        //        RoboCopyFilePath = "RoboCopy.exe";
        //}

        public void Persist()
        {

        }

        //public string GetXml()
        //{
        //    StringBuilder xml = new StringBuilder();
        //    using (StringWriter w = new StringWriter(xml))
        //    {
        //        _xmlSerializer.Serialize(w, this);
        //        return xml.ToString();
        //    }
        //}

        public void LoadXml(string xml)
        {
            using (StringReader reader = new StringReader(xml))
            {
                var appSettings = _xmlSerializer.Deserialize(reader) as ChoAppSettings;
                Copy(appSettings, this);
            }
        }

        public static void Copy(ChoAppSettings src, ChoAppSettings dest)
        {
            if (src == null || dest == null)
                return;

            foreach (var pi in _propInfos.Values)
            {
                SetPropertyValue(dest, pi, ChoType.GetPropertyValue(src, pi));
            }
        }
        private static readonly Dictionary<IntPtr, Action<object, object>> _setterCache = new Dictionary<IntPtr, Action<object, object>>();
        private static readonly object _padLock = new object();
        public static void SetPropertyValue(object target, PropertyInfo propertyInfo, object val)
        {
            ChoGuard.ArgumentNotNull(target, "Target");
            ChoGuard.ArgumentNotNull(propertyInfo, "PropertyInfo");

            if ((val == null || (val is string && ((string)val).IsEmpty())) && propertyInfo.PropertyType.IsValueType)
            {
                if (propertyInfo.PropertyType.IsNullableType())
                    val = null;
                else
                    val = propertyInfo.PropertyType.Default();
            }

            try
            {
                Action<object, object> setter;
                var mi = propertyInfo.GetSetMethod();
                if (mi != null)
                {
                    var key = mi.MethodHandle.Value;
                    if (!_setterCache.TryGetValue(key, out setter))
                    {
                        lock (_padLock)
                        {
                            if (!_setterCache.TryGetValue(key, out setter))
                                _setterCache.Add(key, setter = propertyInfo.CreateSetMethod());
                        }
                    }

                    setter(target, val);
                }
            }
            catch (TargetInvocationException ex)
            {
                throw new TargetInvocationException(String.Format("[Object: {0}, Member: {1}]:", target.GetType().FullName, propertyInfo.Name), ex.InnerException);
            }
        }

        public ChoAppSettings Clone()
        {
            var obj = new ChoAppSettings();
            Copy(this, obj);
            return obj;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }
    }

    //Custom editors that are used as attributes MUST implement the ITypeEditor interface.
    public class CopyFlagsEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoMultiSelectComboBox cmb = new ChoMultiSelectComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoMultiSelectComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoCopyFlags>(propertyItem.Value.ToNString());

            return cmb;
        }
    }

    //Custom editors that are used as attributes MUST implement the ITypeEditor interface.
    public class FileAttributesEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoMultiSelectComboBox cmb = new ChoMultiSelectComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoMultiSelectComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoFileAttributes>(propertyItem.Value.ToNString());

            return cmb;
        }
    }

    //Custom editors that are used as attributes MUST implement the ITypeEditor interface.
    public class FileSelectionAttributesEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoMultiSelectComboBox cmb = new ChoMultiSelectComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoMultiSelectComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoFileSelectionAttributes>(propertyItem.Value.ToNString());

            return cmb;
        }
    }

    public class FileMoveSelectionAttributesEditor : Xceed.Wpf.Toolkit.PropertyGrid.Editors.ITypeEditor
    {
        public FrameworkElement ResolveEditor(Xceed.Wpf.Toolkit.PropertyGrid.PropertyItem propertyItem)
        {
            ChoFileMoveComboBox cmb = new ChoFileMoveComboBox();
            cmb.HorizontalAlignment = HorizontalAlignment.Stretch;

            //create the binding from the bound property item to the editor
            var _binding = new Binding("Value"); //bind to the Value property of the PropertyItem
            _binding.Source = propertyItem;
            _binding.ValidatesOnExceptions = true;
            _binding.ValidatesOnDataErrors = true;
            _binding.Mode = propertyItem.IsReadOnly ? BindingMode.OneWay : BindingMode.TwoWay;
            BindingOperations.SetBinding(cmb, ChoFileMoveComboBox.TextProperty, _binding);

            cmb.ItemsSource = ChoEnum.AsNodeList<ChoFileMoveAttributes>(propertyItem.Value.ToNString()).Select(c => c.Title);
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => cmb.SelectedItem = propertyItem.Value.ToNString()));
            return cmb;
        }
    }

    public class ChoFileMoveComboBox : ComboBox
    {
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0)
            {
                if (e.AddedItems != null && e.AddedItems.Count > 0)
                {
                    var value = (ChoFileMoveAttributes)Enum.Parse(typeof(ChoFileMoveAttributes), e.AddedItems.OfType<string>().FirstOrDefault());
                    if (value == ChoFileMoveAttributes.MoveFilesOnly)
                    {
                        if (MessageBox.Show("在将副本转移到新位置后，是否要删除原始文件？", MainWindow.Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                    else if (value == ChoFileMoveAttributes.MoveDirectoriesAndFiles)
                    {
                        if (MessageBox.Show("在将副本转移到新位置后，是否要删除原始文件/文件夹？", MainWindow.Caption, MessageBoxButton.YesNo, MessageBoxImage.Stop) == MessageBoxResult.No)
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                }
            }
            base.OnSelectionChanged(e);
        }
    }
}
