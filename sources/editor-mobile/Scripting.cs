using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace StrideStudio.Mobile.Scripting
{
    public static class RuntimeScriptCompiler
    {
        public static (bool Success, Type? ScriptType, string Errors) CompileCSharpScript(string sourceCode, string className)
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return (false, null, "Source code cannot be empty.");
            }

            try
            {
                // 1. I-parse ang code gamit ang pinakabagong C# Language Version
                var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
                var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, parseOptions);

                // 2. Kolektahin ang lahat ng kailangang Assemblies para sa Stride Engine at .NET Runtime
                var references = GetMetadataReferences();

                // 3. I-configure ang Roslyn Compilation Options
                var compilationOptions = new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Release,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
                );

                var compilation = CSharpCompilation.Create(
                    assemblyName: $"StrideUserScript_{Guid.NewGuid():N}",
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: compilationOptions
                );

                // 4. I-emit ang na-compile na binary diretso sa memory stream
                using var ms = new MemoryStream();
                var emitResult = compilation.Emit(ms);

                if (!emitResult.Success)
                {
                    var errors = emitResult.Diagnostics
                        .Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error)
                        .Select(d =>
                        {
                            var lineSpan = d.Location.GetLineSpan();
                            int line = lineSpan.IsValid ? lineSpan.StartLinePosition.Line + 1 : 0;
                            return $"Line {line}: {d.GetMessage()}";
                        });

                    return (false, null, string.Join("\n", errors));
                }

                // 5. I-load ang na-compile na DLL sa Android AppDomain Memory
                ms.Seek(0, SeekOrigin.Begin);
                var assembly = Assembly.Load(ms.ToArray());

                // 6. Hanapin ang klase na nag-i-inherit sa ScriptComponent (SyncScript, AsyncScript, StartupScript)
                var scriptType = assembly.GetTypes().FirstOrDefault(t => 
                    (t.Name == className || t.FullName == className) && 
                    typeof(ScriptComponent).IsAssignableFrom(t)
                ) ?? assembly.GetExportedTypes().FirstOrDefault(t => typeof(ScriptComponent).IsAssignableFrom(t));

                if (scriptType == null)
                {
                    return (false, null, $"Class '{className}' inheriting from ScriptComponent (SyncScript/AsyncScript) was not found.");
                }

                return (true, scriptType, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, null, $"Compiler Exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Ligtas na kinukuha ang MetadataReferences kahit walang file path sa Android disk (Memory fallback).
        /// </summary>
        private static List<MetadataReference> GetMetadataReferences()
        {
            var references = new List<MetadataReference>();
            var processedAssemblies = new HashSet<string>();

            // Tiyaking laging kasama ang mga kritikal na Stride at .NET Core assemblies
            var targetAssemblies = new List<Assembly>
            {
                typeof(object).Assembly,                           // System.Private.CoreLib
                typeof(Console).Assembly,                          // System.Console
                typeof(Enumerable).Assembly,                       // System.Linq
                typeof(List<>).Assembly,                           // System.Collections
                typeof(Entity).Assembly,                           // Stride.Engine
                typeof(Vector3).Assembly,                          // Stride.Core.Mathematics
                typeof(ScriptComponent).Assembly,                  // Stride.Engine.ScriptComponent
                typeof(SyncScript).Assembly,                       // Stride.Engine.SyncScript
                typeof(AsyncScript).Assembly,                      // Stride.Engine.AsyncScript
                typeof(StartupScript).Assembly                     // Stride.Engine.StartupScript
            };

            // Isama rin ang lahat ng loaded assemblies sa AppDomain
            targetAssemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

            foreach (var asm in targetAssemblies)
            {
                if (asm.IsDynamic || string.IsNullOrEmpty(asm.FullName) || processedAssemblies.Contains(asm.FullName))
                    continue;

                processedAssemblies.Add(asm.FullName);

                // Option A: Kung may disk location (Desktop/Standard .NET)
                if (!string.IsNullOrEmpty(asm.Location) && File.Exists(asm.Location))
                {
                    try
                    {
                        references.Add(MetadataReference.CreateFromFile(asm.Location));
                        continue;
                    }
                    catch
                    {
                        // Fallback sa Option B kapag nag-fail
                    }
                }

                // Option B: Para sa Android kung saan nasa memory lamang ang DLLs (Raw Metadata Pointer)
                unsafe
                {
                    if (asm.TryGetRawMetadata(out var blob, out var length))
                    {
                        var moduleMetadata = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
                        var assemblyMetadata = AssemblyMetadata.Create(moduleMetadata);
                        references.Add(assemblyMetadata.GetReference());
                    }
                }
            }

            return references;
        }
    }
}
