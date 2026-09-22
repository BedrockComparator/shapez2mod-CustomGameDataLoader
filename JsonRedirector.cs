using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Core;

namespace CustomGameDataLoader
{
    internal class JsonRedirector : IDisposable
    {
        private readonly Hook HookInclude;

        public JsonRedirector()
        {
            MethodInfo method = typeof(IncludePreProcessorSolver).GetMethod(
                "GetTextContentFromPath", BindingFlags.Static | BindingFlags.NonPublic);

            HookInclude = new Hook(method, new Func<Func<string, string>, string, string>(
                (original, path) => Resolve(path, original)));
        }

        public void Dispose() => HookInclude.Dispose();

        private static string Resolve(string path, Func<string, string> original)
        {
            int at = path.IndexOf('@');

            if (at < 0)
                return original(path);

            string modClassName = path.Substring(0, at);
            string relativePath = path.Substring(at + 1);

            Type modType = FindModType(modClassName);
            if (modType == null)
                throw new Exception(
                    $"Mod class '{modClassName}' not found in any loaded assembly (include '{path}').");

            string location = modType.Assembly.Location;
            if (string.IsNullOrEmpty(location))
                throw new Exception(
                    $"Assembly for mod class '{modClassName}' has no file location (include '{path}').");

            string modDirectory = Path.GetDirectoryName(location)!;
            string filePath = Path.Combine(modDirectory, relativePath);

            if (!File.Exists(filePath))
                throw new Exception($"File not found: '{filePath}' (from include '{path}').");

            return File.ReadAllText(filePath);
        }

        private static Type FindModType(string className)
        {
            Type found = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (type.Name != className || !typeof(IMod).IsAssignableFrom(type))
                        continue;

                    if (found != null)
                        throw new Exception(
                            $"Multiple mod classes named '{className}' found: " +
                            $"'{found.FullName}' and '{type.FullName}'. Use a unique name.");

                    found = type;
                }
            }

            return found;
        }

        /// <summary>
        /// A sibling manifest.json marks a mod package, which keeps the game and Unity
        /// assemblies out of the scan. Partial type loads keep their loadable types.
        /// </summary>
        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            // IsDynamic never throws, Assembly.Location does.
            if (assembly.IsDynamic)
                return Enumerable.Empty<Type>();

            string location = assembly.Location;
            string modDirectory = Path.GetDirectoryName(location)!;
            if (!File.Exists(Path.Combine(modDirectory, "manifest.json")))
                return Enumerable.Empty<Type>();

            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null);
            }
        }
    }
}
