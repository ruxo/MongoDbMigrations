using System.Collections.Generic;
using System.Linq;

namespace MongoDBMigrations.Document
{
    public class SchemeValidationResult
    {
        private readonly IList<string> _validCollections;
        private readonly IList<string> _invalidCollections;

        public IEnumerable<string> ValidCollections => _validCollections.Distinct();
        public IEnumerable<string> FailedCollections => _invalidCollections.Distinct();

        public SchemeValidationResult()
        {
            _validCollections = new List<string>();
            _invalidCollections = new List<string>();
        }

        public void Add(string name, bool isFailed)
        {
            if (isFailed)
                _invalidCollections.Add(name);
            else
                _validCollections.Add(name);
        }
    }
}
