using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Stride.Engine;

namespace StrideStudio.Mobile.Scripting
{
    public static class RuntimeScriptCompiler
    {
        public static (bool Success, Type? ScriptType, string Errors) CompileCSharpScript(string sourceCode, string className)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);

            // Kolektahin ang mga references mula sa kasalukuyang AppDomain (Stride Assemblies)
            var references = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location))
                .Cast<MetadataReference>()
                .ToList();

            var compilation = CSharpCompilation.Create(
                assemblyName: $"UserScript_{Guid.NewGuid():N}",
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                var errors = string.Join("\n", emitResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => $"L{d.Location.GetLineSpan().StartLinePosition.Line + 1}: {d.GetMessage()}"));
                return (false, null, errors);
            }

            ms.Seek(0, SeekOrigin.Begin);
            var assembly = Assembly.Load(ms.ToArray());
            var scriptType = assembly.GetTypes().FirstOrDefault(t => t.Name == className && typeof(ScriptComponent).IsAssignableFrom(t));

            return (true, scriptType, string.Empty);
        }
    }
}
