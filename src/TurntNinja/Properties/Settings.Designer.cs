﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TurntNinja.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public float AudioCorrection {
            get {
                return ((float)(this["AudioCorrection"]));
            }
            set {
                this["AudioCorrection"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public float MaxAudioVolume {
            get {
                return ((float)(this["MaxAudioVolume"]));
            }
            set {
                this["MaxAudioVolume"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1280")]
        public int ResolutionX {
            get {
                return ((int)(this["ResolutionX"]));
            }
            set {
                this["ResolutionX"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("720")]
        public int ResolutionY {
            get {
                return ((int)(this["ResolutionY"]));
            }
            set {
                this["ResolutionY"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("4")]
        public int AntiAliasingSamples {
            get {
                return ((int)(this["AntiAliasingSamples"]));
            }
            set {
                this["AntiAliasingSamples"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool VSync {
            get {
                return ((bool)(this["VSync"]));
            }
            set {
                this["VSync"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Normal")]
        public global::OpenTK.WindowState WindowState {
            get {
                return ((global::OpenTK.WindowState)(this["WindowState"]));
            }
            set {
                this["WindowState"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute(".mp3,.flac,.wav,.m4a,.wma")]
        public string FileFilter {
            get {
                return ((string)(this["FileFilter"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("turnt-ninja")]
        public string AppDataFolderName {
            get {
                return ((string)(this["AppDataFolderName"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("9")]
        public float OnsetActivationThreshold {
            get {
                return ((float)(this["OnsetActivationThreshold"]));
            }
            set {
                this["OnsetActivationThreshold"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool OnsetAdaptiveWhitening {
            get {
                return ((bool)(this["OnsetAdaptiveWhitening"]));
            }
            set {
                this["OnsetAdaptiveWhitening"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("50")]
        public int MaxRecentSongCount {
            get {
                return ((int)(this["MaxRecentSongCount"]));
            }
            set {
                this["MaxRecentSongCount"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int DifficultyLevel {
            get {
                return ((int)(this["DifficultyLevel"]));
            }
            set {
                this["DifficultyLevel"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool OnsetOnline {
            get {
                return ((bool)(this["OnsetOnline"]));
            }
            set {
                this["OnsetOnline"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0.2")]
        public float OnsetSlicePaddingLength {
            get {
                return ((float)(this["OnsetSlicePaddingLength"]));
            }
            set {
                this["OnsetSlicePaddingLength"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public float OnsetSliceLength {
            get {
                return ((float)(this["OnsetSliceLength"]));
            }
            set {
                this["OnsetSliceLength"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool Debug {
            get {
                return ((bool)(this["Debug"]));
            }
            set {
                this["Debug"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UpgradeRequired {
            get {
                return ((bool)(this["UpgradeRequired"]));
            }
            set {
                this["UpgradeRequired"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("turnt-ninja.db")]
        public string DatabaseFile {
            get {
                return ((string)(this["DatabaseFile"]));
            }
            set {
                this["DatabaseFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("opcon")]
        public string PlayerName {
            get {
                return ((string)(this["PlayerName"]));
            }
            set {
                this["PlayerName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string UserID {
            get {
                return ((string)(this["UserID"]));
            }
            set {
                this["UserID"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("oeoywrZVrjYlcctPZZ3uTXFPp9ItVUkR3w28u+WDdd9SqYzE1PRkb+WPZsYQ+VvOfUjFQOlQ1tXc7oDXY" +
            "4Km7A==")]
        public string Piwik {
            get {
                return ((string)(this["Piwik"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("jA9OAo1hWfHPOoMipHAAaFxFf2N7iP8zuBEI9LVQq5ET0vc8yqnLsO/oNMqCcki0rvD4CQrkMz7fWHQ4r" +
            "WuHAyRfY+7nxGinxdbgIaceyVtKOJLsSnbBh4/NBq3OkBIAFG2GTxUhWqyJbxNhgzbu+0HM8jv43LY53" +
            "bxhwIALt+A=")]
        public string Sentry {
            get {
                return ((string)(this["Sentry"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool Analytics {
            get {
                return ((bool)(this["Analytics"]));
            }
            set {
                this["Analytics"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool FirstRun {
            get {
                return ((bool)(this["FirstRun"]));
            }
            set {
                this["FirstRun"] = value;
            }
        }
    }
}
