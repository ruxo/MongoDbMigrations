using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MongoDBMigrations.Core
{
    public class OpsSessionItem
    {
        public CommandStartedEvent Event { get; set; }
        public long Timestamp { get; set; }
        public int Step { get; set; }
    }

    public class OpsSession : IDisposable
    {
        public enum OpsCommand
        {
            insert, //Delete
            update, //Update
            delete, //Insert
            cloneCollection, //Drop
            create, // Drop
            drop, //Create & bulk insert
            createIndexes, // Drop indexes 
            dropIndexes, // Create Indexes
            renameCollection, // Rename Collection
        }

        private IList<OpsSessionItem> _ops = new List<OpsSessionItem>();
        private bool _isRecording = false;
        private int _step = 0;

        public void StartOpsRecording(int step = 0)
        {
            _isRecording = true;
            _step = step;
        }

        public void StopOpsRecording()
        {
            _isRecording = false;
        }

        public void RevertOps()
        {
            // Restore database and call all succeed commands
        }

        internal void SaveOp(CommandStartedEvent e)
        {
            // TODO we should save only accepted commands

            var t = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if(_isRecording && Enum.GetNames(typeof(OpsCommand)).Contains(e.CommandName))
            {
                _ops.Add(new OpsSessionItem
                {
                    Event = e,
                    Timestamp = t,
                    Step = _step
                });
            }
        }

        public void Dispose()
        {

            Dispose(true);
            GC.SuppressFinalize(this);

        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //TODO: dispose all managed resources
            }
            //TODO: dispose all unmanaged resources
            //Remove dump
        }
    }

    public class OperationsLogManager
    {
        private readonly string _mongoFolderPath;
        private const string _dumpFolderName = "temp-dump";

        public OperationsLogManager(string mongoFolderPath = "C:\\Program Files\\MongoDB\\Server\\4.2\\bin")
        {
            if(string.IsNullOrWhiteSpace(mongoFolderPath))
            {
                throw new ArgumentNullException(mongoFolderPath);
            }

            _mongoFolderPath = mongoFolderPath;
        }

        public OpsSession StartSession(IMongoDatabase db)
        {
            if(db == null)
            {
                throw new ArgumentNullException(nameof(db));
            }

            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), _dumpFolderName);
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            var host = db.Client.Settings.Server.Host;
            var port = db.Client.Settings.Server.Port;
            //Flush all pending commands and lock database

            OpsUtilities.BuildCommandProcess(_mongoFolderPath, host, port, "mongo.exe", "db.fsyncLock()").Start();

            //Create database dump
            OpsUtilities.BuildShellProcess(_mongoFolderPath, host, port, "mongodump.exe",
                $"--db {db.DatabaseNamespace.DatabaseName} --out {_dumpFolderName}").Start();

            //Unlock database
            OpsUtilities.BuildCommandProcess(_mongoFolderPath, host, port, "mongo.exe", "db.fsyncUnlock()").Start();

            var session = new OpsSession();

            // ATTENTION: Nobody can change settigns after creating a MongoClient
            db.Client.Settings.ClusterConfigurator = cc =>
            {
                cc.Subscribe<CommandStartedEvent>(e => session.SaveOp(e));
            };
            return session;
        }
    }

    public static class OpsUtilities
    {
        public static Process BuildCommandProcess(string path, string host, int port, string shell, string command)
        {
            var _mongoShellProcess = new Process();
            _mongoShellProcess.StartInfo.FileName = Path.Combine(path, shell);
            _mongoShellProcess.StartInfo.Arguments = $"--host {host} --port {port} --quiet --eval \"{command}\"";
            _mongoShellProcess.StartInfo.UseShellExecute = false;
            _mongoShellProcess.StartInfo.RedirectStandardOutput = true;
            _mongoShellProcess.StartInfo.CreateNoWindow = true;

            return _mongoShellProcess;
        }

        public static Process BuildShellProcess(string path, string host, int port, string shell, string args)
        {
            var _mongoShellProcess = new Process();
            _mongoShellProcess.StartInfo.FileName = Path.Combine(path, shell);
            _mongoShellProcess.StartInfo.Arguments = $"--host {host} --port {port} --quiet {args}";
            _mongoShellProcess.StartInfo.UseShellExecute = false;
            _mongoShellProcess.StartInfo.RedirectStandardOutput = true;
            _mongoShellProcess.StartInfo.CreateNoWindow = true;

            return _mongoShellProcess;
        }
    }
    
}
