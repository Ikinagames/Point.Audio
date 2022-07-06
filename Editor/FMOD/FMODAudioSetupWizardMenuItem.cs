// Copyright 2022 Ikina Games
// Author : Seung Ha Kim (Syadeu)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Point.Collections.Editor;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Point.Audio.FMODEditor
{
    internal sealed class FMODAudioSetupWizardMenuItem : SetupWizardMenuItem
    {
        const string c_FMODPluginDllPath = "Assets/Plugins/FMOD/platforms/{0}/lib/{1}";
        const string c_OriginalDllPath = "Assets/Plugins/Point/Point.Audio/.Point.Audio.Native/{0}/Release";

        const string c_PluginName = "Point.Audio.FMOD.Native.dll";

        public enum platform
        {
            android,
            html5,
            ios,
            linux,
            mac,
            tvos,
            uwp,
            win,
        }
        public enum archtect
        {
            x86,
            x86_64
        }
        public static string GetDllPath(platform platform, archtect archtect)
        {
            return string.Format(c_FMODPluginDllPath, platform, archtect);
        }
        public static string GetOriginalDllPath(archtect target)
        {
            return string.Format(c_OriginalDllPath,
                target == archtect.x86_64 ? "x64" : "x86");
        }

        public override string Name => "FMOD Audio";
        public override int Order => 0;

        public override bool Predicate()
        {
            return true;
        }

        private FMODUnity.Platform m_DefaultPlatform;
        private bool m_HasDynamicPluginAdded, m_HasX86Win, m_HasX86_64Win;

        public FMODAudioSetupWizardMenuItem()
        {
            string path = GetDllPath(platform.win, archtect.x86);
            m_HasX86Win = File.Exists(Path.Combine(path, c_PluginName));
            path = GetDllPath(platform.win, archtect.x86_64);
            m_HasX86_64Win = File.Exists(Path.Combine(path, c_PluginName));

            m_DefaultPlatform = FMODUnity.EditorSettings.Instance.RuntimeSettings.DefaultPlatform;
            m_HasDynamicPluginAdded = m_DefaultPlatform.Plugins.Contains(Path.GetFileNameWithoutExtension(c_PluginName));
        }
        public override void OnGUI()
        {
            using (new CoreGUI.BoxBlock(Color.black))
            {
                if (m_HasDynamicPluginAdded)
                {
                    EditorGUILayout.HelpBox("All plugins has been registered", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Require registration", MessageType.Info);
                }
                using (new EditorGUI.DisabledGroupScope(m_HasDynamicPluginAdded))
                {
                    if (GUILayout.Button("Register Dynamic Plugin"))
                    {
                        m_DefaultPlatform.Plugins.Add(Path.GetFileNameWithoutExtension(c_PluginName));
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                //using (new EditorGUI.DisabledGroupScope(m_HasX86Win))
                {
                    if (GUILayout.Button("Copy x86"))
                    {
                        string path = Path.Combine(GetOriginalDllPath(archtect.x86), c_PluginName);
                        string targetPath = Path.Combine(GetDllPath(platform.win, archtect.x86), c_PluginName);
                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                        }
                        File.Copy(path, targetPath);
                    }
                }
                //using (new EditorGUI.DisabledGroupScope(m_HasX86_64Win))
                {
                    if (GUILayout.Button("Copy x64"))
                    {
                        string path = Path.Combine(GetOriginalDllPath(archtect.x86_64), c_PluginName);
                        string targetPath = Path.Combine(GetDllPath(platform.win, archtect.x86_64), c_PluginName);
                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                        }
                        File.Copy(path, targetPath);
                    }
                }
            }
        }
    }
}
