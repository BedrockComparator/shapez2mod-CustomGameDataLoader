using MonoMod.RuntimeDetour;
using ShapezShifter.SharpDetour;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Unity.Core;

namespace CustomGameDataLoader
{
    internal class JsonRedirector : IDisposable
    {
        private const char ModPathSeparator = '@';
        private Hook HK_IncludePreProcessorSolver;
        public JsonRedirector()
        {
            HK_IncludePreProcessorSolver = DetourHelper.StaticReplace(
                original: path => IncludePreProcessorSolver.GetTextContentFromPath(path),
                replacement: (string path) => ResolveIncludePath(path)
            );
        }
        public void Dispose()
        {
            HK_IncludePreProcessorSolver.Dispose();
        }
        private static string ResolveIncludePath(string path)
        {
            int separatorIndex = path.IndexOf(ModPathSeparator);
            if (separatorIndex > 0)
            {
                string modClassName = path.Substring(0, separatorIndex);
                string relativePath = path.Substring(separatorIndex + 1);
                return LoadFromModDirectory(modClassName, relativePath);
            }
            //vanilla behavior
            TextAsset textAsset = Resources.Load<TextAsset>(path);
            if (textAsset == null)
            {
                throw new Exception("Could not resolve include: " + path);
            }
            return textAsset.text;
        }

        private static string LoadFromModDirectory(string modClassName, string relativePath)
        {
            Type modType = FindModType(modClassName);
            if (modType == null)
            {
                throw new Exception(
                    $"Mod class '{modClassName}' not found in any loaded assembly. " +
                    "Make sure the mod is loaded and the class name matches the mod's IMod implementation.");
            }

            string modDirectory = Directory.GetParent(modType.Assembly.Location)?.FullName;
            if (modDirectory == null)
            {
                throw new Exception($"Could not determine directory for mod '{modClassName}'.");
            }

            string filePath = Path.Combine(modDirectory, relativePath);
            if (!File.Exists(filePath))
            {
                throw new Exception(
                    $"File not found: '{filePath}' (resolved from mod class '{modClassName}').");
            }

            return File.ReadAllText(filePath);
        }

        private static Type FindModType(string className)
        {
            Type found = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type.Name != className)
                        continue;
                    if (!typeof(IMod).IsAssignableFrom(type))
                        continue;

                    if (found != null)
                    {
                        throw new Exception(
                            $"Multiple mod classes named '{className}' found: " +
                            $"'{found.FullName}' and '{type.FullName}'. " +
                            "Use a unique mod class name to identify the target mod.");
                    }

                    found = type;
                }
            }

            return found;
        }
    }
}
