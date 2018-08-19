using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MongoDBMigrations.Core
{
    public class MigrationMethodsFinder : CSharpSyntaxWalker
    {
        private IEnumerable<string> _allowedMigrationNames;
        private string _methodName;
        private List<ClassDeclarationSyntax> _classes = new List<ClassDeclarationSyntax>();
        private List<MethodDeclarationSyntax> _methods = new List<MethodDeclarationSyntax>();

        public MigrationMethodsFinder(IEnumerable<string> allowedMigrationNames, string methodName)
        {
            _allowedMigrationNames = allowedMigrationNames;
            _methodName = methodName;
        }

        public List<MethodDeclarationSyntax> FindMethods(SyntaxNode node)
        {
            base.Visit(node);
            var result = new List<MethodDeclarationSyntax>(_methods);
            _classes.Clear();
            _methods.Clear();
            return result;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (_allowedMigrationNames.Contains(node.Identifier.ValueText))
            {
                _classes.Add(node);
            }
            base.VisitClassDeclaration(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (!(node.Parent is ClassDeclarationSyntax classNode) 
                || _classes.Any(cn => cn.Identifier.ValueText != classNode.Identifier.ValueText))
            {
                return;
            }

            if (node.Identifier.ValueText == _methodName)
            {
                _methods.Add(node);
            }
            base.VisitMethodDeclaration(node);
        }
    }
}
