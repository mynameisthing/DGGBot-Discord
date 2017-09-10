﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DGGBot.Services.Eval
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class BlacklistedTypesAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "MOD0001";
        private const string Category = "Discord";
        private static readonly LocalizableString Title = "Prohibited API";
        private static readonly LocalizableString MessageFormat = "Usage of this API is prohibited";

        private static readonly List<string> _symbols =
            new List<string> {"Environment", "Process", "File", "Directory", "FileInfo", "DirectoryInfo"};

        internal static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSemanticModelAction(AnalyzeSymbol);
        }

        private static void AnalyzeSymbol(SemanticModelAnalysisContext context)
        {
            var model = context.SemanticModel;

            var tree = model.SyntaxTree;
            var nodes = tree.GetRoot()
                .DescendantNodes(n => true)
                .Where(n => n is IdentifierNameSyntax || n is ExpressionSyntax);

            foreach (var node in nodes)
            {
                var symbol = node is IdentifierNameSyntax
                    ? model.GetSymbolInfo(node).Symbol
                    : model.GetTypeInfo(node).Type;

                if (symbol is INamedTypeSymbol namedSymbol &&
                    _symbols.Contains(namedSymbol.Name))
                    context.ReportDiagnostic(Diagnostic.Create(Rule, node.GetLocation()));
            }
        }
    }
}