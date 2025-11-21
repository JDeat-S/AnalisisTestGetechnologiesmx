using log4net;

namespace AnalisisTest.Sample
{
    using System;
    using System.IO;
    using System.Linq;


    public class FileUpdater
    {
        private static readonly ILog Log = log4net.LogManager.GetLogger(typeof(FileUpdater));
        #region Constants
        private const string DeleteCommandExtension = ".del";
        private const string AddCommandExtension = ".add";
        private const string UpdateCommandExtension = ".upd";
        private const string XdtMergeCommandExtension = ".xmrg";
        private const string ExecuteCommandExtension = ".exc";
        private const string ExecuteCommandExtensionInitial = ".eini";
        private const string ExecuteCommandExtensionEnd = ".eend";
        private const string ExecuteCommandParamsExtension = ".params";
        private const string CannotTransformXdtMessage = "No se puede realizar la transformación del archivo .";

        #endregion


        #region Methods


        private static void BackupFile(string backupDir, string targetFolder, string originalFileName, string command)
        {
            var backupFileName = originalFileName;
            var targetRelativeDirectory = string.Empty;

            if (File.Exists(originalFileName))
            {
                if (command == DeleteCommandExtension)
                {
                    command = UpdateCommandExtension;
                    targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
                }

                if (command == ExecuteCommandExtensionInitial || command == ExecuteCommandExtension || command == ExecuteCommandExtensionEnd || command == ExecuteCommandParamsExtension)
                {
                    backupFileName = Path.Combine(Path.GetDirectoryName(originalFileName), Path.GetFileNameWithoutExtension(originalFileName));
                    targetRelativeDirectory = targetFolder;
                }
            }
            else if (command == DeleteCommandExtension)
            {
                if (!Directory.Exists(Path.GetDirectoryName(originalFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalFileName));
                }

                File.WriteAllText(originalFileName, string.Empty);
                targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
            }
            else if (command == UpdateCommandExtension)
            {
                command = DeleteCommandExtension;
                if (!Directory.Exists(Path.GetDirectoryName(originalFileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(originalFileName));
                }

                File.WriteAllText(originalFileName, string.Empty);
                targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(originalFileName, targetFolder));
            }
            else
            {
                return;
            }

            var folderToBackupFile = Path.Combine(backupDir, targetRelativeDirectory);

            if (!Directory.Exists(folderToBackupFile))
            {
                Directory.CreateDirectory(folderToBackupFile);
            }

            File.Copy(originalFileName, Path.Combine(folderToBackupFile, Path.GetFileName(backupFileName)) + command, true);
        }

        private static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);

            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }

            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public string UpdateFiles(string sourceFolder, string targetFolder, string backupDir = null)
        {
            var createBackup = !string.IsNullOrEmpty(backupDir);

            if (createBackup)
            {
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }
            }

            try
            {
                Directory.EnumerateFiles(sourceFolder, "*" + ExecuteCommandExtensionInitial, SearchOption.AllDirectories).ToList().ForEach(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    this.ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir, targetRelativeDirectory), ExecuteCommandExtensionInitial);
                });

                foreach (var file in Directory.EnumerateFiles(sourceFolder, "*.*", SearchOption.AllDirectories).Where(s => Path.GetExtension(s) != ExecuteCommandExtensionInitial && Path.GetExtension(s) != ExecuteCommandExtensionEnd).ToList())
                {
                    var fileExtension = Path.GetExtension(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    var targetFileName = Path.Combine(targetFolder, targetRelativeDirectory ?? string.Empty, fileName ?? string.Empty);

                    switch (fileExtension.ToLower())
                    {
                        case AddCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, DeleteCommandExtension);
                            }

                            this.CopyFile(file, targetFileName);
                            break;
                        case XdtMergeCommandExtension:
                            if (createBackup && File.Exists(targetFileName))
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, UpdateCommandExtension);
                            }

                            this.MergeXDT(file, targetFileName);
                            break;
                        case UpdateCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, UpdateCommandExtension);
                            }

                            this.CopyFile(file, targetFileName);
                            break;
                        case DeleteCommandExtension:
                            if (createBackup)
                            {
                                BackupFile(Path.Combine(backupDir, targetRelativeDirectory ?? string.Empty), targetFolder, targetFileName, AddCommandExtension);
                            }

                            this.RemoveFile(targetFileName);
                            break;
                        case ExecuteCommandExtension:
                            this.ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir ?? string.Empty, targetRelativeDirectory ?? string.Empty), ExecuteCommandExtension);
                            break;
                        default:
                            // Ignorar archivos sin extensión especial
                            break;
                    }
                }

                Directory.EnumerateFiles(sourceFolder, "*" + ExecuteCommandExtensionEnd, SearchOption.AllDirectories).ToList().ForEach(file =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var targetRelativeDirectory = Path.GetDirectoryName(GetRelativePath(file, sourceFolder));
                    this.ExecuteBats(file, fileName, targetRelativeDirectory, createBackup, Path.Combine(backupDir ?? string.Empty, targetRelativeDirectory ?? string.Empty), ExecuteCommandExtensionEnd);
                });
            }
            catch (Exception ex)
            {
                if (createBackup)
                {
                    var errorMessage = ex.ToString();
                    var rollbackError = this.UpdateFiles(backupDir, targetFolder);
                    return errorMessage + (string.IsNullOrEmpty(rollbackError) ? string.Empty : Environment.NewLine + "Rollback Error =>" + Environment.NewLine + rollbackError);
                }

                return ex.ToString();
            }

            return string.Empty;
        }

        private void ExecuteBats(string file, string fileName, string targetRelativeDirectory, bool createBackup, string backupDir, string extension)
        {
            // Dummy: log execution of bat/script
            Log.Info($"ExecuteBats: {file} (ext {extension}) targetRel: {targetRelativeDirectory}");
            // En la práctica: leer el script y ejecutarlo; aquí no hacemos nada real.
        }

        private void CopyFile(string sourceFile, string targetFile)
        {
            var dir = Path.GetDirectoryName(targetFile);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            Log.Info($"CopyFile: {sourceFile} => {targetFile}");
            File.Copy(sourceFile, targetFile, true);
        }

        private void RemoveFile(string targetFile)
        {
            Log.Info($"RemoveFile: {targetFile}");
            try
            {
                if (File.Exists(targetFile)) File.Delete(targetFile);
            }
            catch (Exception ex)
            {
                Log.Info("No se pudo eliminar archivo: " + ex.Message);
            }
        }

        private void MergeXDT(string sourceFile, string targetFile)
        {
            // En esta versión de prueba no aplicamos XDT real.
            // Simplemente copiamos el archivo de origen sobre el destino (comportamiento simplificado).
            Log.Info($"MergeXDT (simplificado): {sourceFile} -> {targetFile}");
            var dir = Path.GetDirectoryName(targetFile);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.Copy(sourceFile, targetFile, true);
        }

        #endregion
    }
}