using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

        /// <summary>
        /// List of method names in which the collection name is used. 
        /// They were taken from the IMongoDatabase interface.
        /// </summary>
        public List<string> MethodMarkers { get; } = new List<string>
        {
            "GetCollection", //IMongoDatabase
            "CreateCollection",
            "CreateCollectionAsync",
            "DropCollection",
            "DropCollectionAsync",
            "ListCollectionNames",
            "ListCollectionNamesAsync",
            "ListCollections",
            "ListCollectionsAsync",
            "RenameCollection",
            "RenameCollectionAsync",
            "GetCollectionNames", //MongoDatabase
        };

        /// <summary>
        /// Add new method name to MethodMarkers collection, it will be added if it not exist.
        /// </summary>
        /// <param name="methodName">Method name</param>
        public void RegisterMethodMarker(string methodName)
        {
            if (MethodMarkers.Any(i => i.Equals(methodName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return;
            }

            MethodMarkers.Add(methodName);
        }

        /// <summary>
        /// This method check documents which will be affected by migration. For successful result all
        /// documents in collection must have the same scheme otherwise validation will be failed.
        /// </summary>
        /// <param name="migrations">List of migration that preparing for applying</param>
        /// <param name="isUp">Migration direction</param>
        /// <param name="pathToMigrationProj">Absolute path to *.csproj file with migration</param>
        /// <param name="database">Instance of mongo database</param>
        /// <returns></returns>
        public SchemeValidationResult Validate(IEnumerable<IMigration> migrations, bool isUp, string pathToMigrationProj, IMongoDatabase database)
        {
            if (string.IsNullOrEmpty(pathToMigrationProj))
                throw new ArgumentNullException(nameof(pathToMigrationProj));

            if (database == null)
                throw new ArgumentNullException(nameof(database));

            if (migrations == null)
                throw new ArgumentNullException(nameof(migrations));

            if (!migrations.Any())
                return new SchemeValidationResult();

            string methodName = isUp ? nameof(IMigration.Up) : nameof(IMigration.Down);
            var allowedMigrationNames = migrations.Select(t => t.GetType().Name);

            var workspace = CreateRoslynWorkspace(pathToMigrationProj);
            var project = workspace.CurrentSolution.Projects.Single(prj => prj.FilePath == pathToMigrationProj);

            var compilation = project.GetCompilationAsync().Result;

            var finder = new MigrationMethodsFinder(allowedMigrationNames, methodName);
            var collectionNames = new List<string>();
            foreach (var file in project.Documents)
            {
                var tree = file.GetSyntaxTreeAsync().Result;
                var methods = finder.FindMethods(tree.GetRoot());
                if (methods.Any())
                {
                    var model = compilation.GetSemanticModel(tree);
                    collectionNames.AddRange(methods.SelectMany(item => FindCollectionNames(model, item)));
                }
            }
            return Check(database, collectionNames);
        }

        /// <summary>
        /// Search collection names in migration method taking into account the list of method markers.
        /// </summary>
        /// <param name="semanticModel">Semantic model of migration class</param>
        /// <param name="node">Syntax node of migration method (Up or Down)</param>
        /// <returns></returns>
        protected virtual IEnumerable<string> FindCollectionNames(SemanticModel semanticModel, SyntaxNode node)
        {
            var arguments = node
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(sn => MethodMarkers.Contains(semanticModel.GetSymbolInfo(sn).Symbol.Name))
                .SelectMany(sn => sn
                    .DescendantNodes()
                    .OfType<LiteralExpressionSyntax>()
                    .Where(lit => lit.IsKind(SyntaxKind.StringLiteralExpression))
                    .Select(lit => lit.GetText().ToString()));

            return arguments.Distinct().Select(x => x.Trim('"'));
        }

        /// <summary>
        /// Create the Roslyn workspace for you project
        /// </summary>
        /// <param name="projLocation">Absolute path to the *.csproj or project.json file</param>
        /// <returns>Roslyn workspace</returns>
        protected virtual Workspace CreateRoslynWorkspace(string projLocation)
        {
            var manager = new AnalyzerManager();
            var analyzer = manager.GetProject(projLocation);
            return analyzer.GetWorkspace();
        }

        private SchemeValidationResult Check(IMongoDatabase database, IEnumerable<string> names)
        {
            var result = new SchemeValidationResult();
            foreach (var name in names.Distinct())
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
                    foreach (var document in batch)
                    {
                        isFailed = !document.Elements.ToDictionary(i => i.Name, i => i.Value.BsonType).SequenceEqual(refScheme);
                    }
                }

                result.Add(name, isFailed);
            }

            return result;
        }
    }
}
