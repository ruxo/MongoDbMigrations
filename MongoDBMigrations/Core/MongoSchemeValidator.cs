using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBMigrations.Document;
using System;
using System.Collections.Generic;
using System.Linq;


namespace MongoDBMigrations.Core
{
    public class MongoSchemeValidator
    {
        private readonly string[] LIST_OF_MONGO_COLLECTION_INTERACTIVE_METHODS = new string[]
        {
            "GetCollection",
            "CreateCollection",
            "CreateCollectionAsync",
            "DropCollection",
            "DropCollectionAsync"
        };

        public IDictionary<string, bool> Validate(IEnumerable<IMigration> migrations, bool isUp, string pathToMigrationProj, IMongoDatabase database)
        {
            if (string.IsNullOrEmpty(pathToMigrationProj))
                throw new ArgumentNullException(nameof(pathToMigrationProj));

            if (!migrations.Any())
                return new Dictionary<string, bool>(0);

            string methodName = isUp ? nameof(IMigration.Up) : nameof(IMigration.Down);

            var workspace = CreateRoslynWorkspace(pathToMigrationProj);
            var project = workspace.CurrentSolution.Projects.Single(prj => prj.FilePath == pathToMigrationProj);

            var compilation = project.GetCompilationAsync().Result;
            var allowedMigrationNames = migrations.Select(t => t.GetType().Name);
            var syntaxTreesForAnalyzing = new List<SyntaxTree>();
            foreach (var file in project.Documents)
            {
                var migrationClassDeclaration = file.GetSyntaxTreeAsync().Result
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .SingleOrDefault(x => allowedMigrationNames.Contains(x.Identifier.ValueText));

                if (migrationClassDeclaration == null)
                    continue;

                syntaxTreesForAnalyzing.Add(migrationClassDeclaration.SyntaxTree);
            }

            var collectionNames = new List<string>();
            foreach (var tree in syntaxTreesForAnalyzing)
            {
                var model = compilation.GetSemanticModel(tree);
                var method = tree
                    .GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single(x => x.Identifier.ValueText == methodName);

                collectionNames.AddRange(FindCollectionNames(model, method));
            }

            return Check(database, collectionNames);
        }

        private IDictionary<string, bool> Check(IMongoDatabase database, IEnumerable<string> names)
        {
            var result = new Dictionary<string, bool>();
            foreach (var name in names)
            {
                bool isFailed = false;
                var collection = database.GetCollection<BsonDocument>(name);

                if (collection == null || collection.CountDocuments(FilterDefinition<BsonDocument>.Empty) == 0)
                    continue;

                var doc = collection.Find(FilterDefinition<BsonDocument>.Empty)
                    .First();

                var refScheme = doc.Elements.ToDictionary(i => i.Name, i => i.Value.BsonType);

                var cursor = collection.Find(FilterDefinition<BsonDocument>.Empty).ToCursor();
                while (cursor.MoveNext())
                {
                    if (isFailed)
                        break;

                    IEnumerable<BsonDocument> batch = cursor.Current;
                    foreach(var document in batch)
                    {
                        isFailed = !document.Elements.ToDictionary(i => i.Name, i => i.Value.BsonType).SequenceEqual(refScheme);
                    }
                }

                result.Add(name, isFailed);
            }

            return result;
        }

        protected virtual IEnumerable<string> FindCollectionNames(SemanticModel semanticModel, SyntaxNode tree)
        {
            var arguments = tree
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(sn => LIST_OF_MONGO_COLLECTION_INTERACTIVE_METHODS.Contains(semanticModel.GetSymbolInfo(sn).Symbol.Name))
                .SelectMany(sn => sn
                    .DescendantNodes()
                    .OfType<LiteralExpressionSyntax>()
                    .Select(lit => lit.GetText().ToString()));

            return arguments.Select(x => x.Trim('"')).Distinct();
        }

        protected virtual Workspace CreateRoslynWorkspace(string projLocation)
        {
            var manager = new AnalyzerManager();
            var analyzer = manager.GetProject(projLocation);
            return analyzer.GetWorkspace();
        }
    }
}
