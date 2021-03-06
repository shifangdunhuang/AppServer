/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ASC.Common;
using ASC.Common.Threading;
using ASC.Core;
using ASC.Files.Resources;

namespace ASC.Web.Files.Services.WCFService.FileOperations
{
    public class FileOperationsManager
    {
        private readonly DistributedTaskQueue tasks;

        public IServiceProvider ServiceProvider { get; }

        public FileOperationsManager(DistributedTaskCacheNotify distributedTaskCacheNotify, IServiceProvider serviceProvider)
        {
            tasks = new DistributedTaskQueue(distributedTaskCacheNotify, "fileOperations", 10);
            ServiceProvider = serviceProvider;
        }

        public ItemList<FileOperationResult> GetOperationResults(AuthContext authContext)
        {
            var operations = tasks.GetTasks();
            var processlist = Process.GetProcesses();

            foreach (var o in operations.Where(o => string.IsNullOrEmpty(o.InstanceId)
                                                    || processlist.All(p => p.Id != int.Parse(o.InstanceId))))
            {
                o.SetProperty(FileOperation.PROGRESS, 100);
                tasks.RemoveTask(o.Id);
            }

            operations = operations.Where(t => t.GetProperty<Guid>(FileOperation.OWNER) == authContext.CurrentAccount.ID);
            foreach (var o in operations.Where(o => o.Status > DistributedTaskStatus.Running))
            {
                o.SetProperty(FileOperation.PROGRESS, 100);
                tasks.RemoveTask(o.Id);
            }

            var results = operations
                .Where(o => o.GetProperty<bool>(FileOperation.HOLD) || o.GetProperty<int>(FileOperation.PROGRESS) != 100)
                .Select(o => new FileOperationResult
                {
                    Id = o.Id,
                    OperationType = o.GetProperty<FileOperationType>(FileOperation.OPERATION_TYPE),
                    Source = o.GetProperty<string>(FileOperation.SOURCE),
                    Progress = o.GetProperty<int>(FileOperation.PROGRESS),
                    Processed = o.GetProperty<int>(FileOperation.PROCESSED).ToString(),
                    Result = o.GetProperty<string>(FileOperation.RESULT),
                    Error = o.GetProperty<string>(FileOperation.ERROR),
                    Finished = o.GetProperty<bool>(FileOperation.FINISHED),
                })
                .ToList();

            return new ItemList<FileOperationResult>(results);
        }

        public ItemList<FileOperationResult> CancelOperations(AuthContext authContext)
        {
            var operations = tasks.GetTasks()
                .Where(t => t.GetProperty<Guid>(FileOperation.OWNER) == authContext.CurrentAccount.ID);

            foreach (var o in operations)
            {
                tasks.CancelTask(o.Id);
            }

            return GetOperationResults(authContext);
        }


        public ItemList<FileOperationResult> MarkAsRead(AuthContext authContext, TenantManager tenantManager, IEnumerable<object> folderIds, IEnumerable<object> fileIds)
        {
            var tenant = tenantManager.GetCurrentTenant();
            var op1 = new FileMarkAsReadOperation<int>(ServiceProvider, new FileMarkAsReadOperationData<int>(folderIds.OfType<long>().Select(Convert.ToInt32), fileIds.OfType<long>().Select(Convert.ToInt32), tenant));
            var op2 = new FileMarkAsReadOperation<string>(ServiceProvider, new FileMarkAsReadOperationData<string>(folderIds, fileIds, tenant));
            var op = new FileMarkAsReadOperation(ServiceProvider, op2, op1);
            return QueueTask(authContext, op);
        }

        public ItemList<FileOperationResult> Download(AuthContext authContext, TenantManager tenantManager, Dictionary<object, string> folders, Dictionary<object, string> files, Dictionary<string, string> headers)
        {
            var operations = tasks.GetTasks()
                .Where(t => t.GetProperty<Guid>(FileOperation.OWNER) == authContext.CurrentAccount.ID)
                .Where(t => t.GetProperty<FileOperationType>(FileOperation.OPERATION_TYPE) == FileOperationType.Download);

            if (operations.Any(o => o.Status <= DistributedTaskStatus.Running))
            {
                throw new InvalidOperationException(FilesCommonResource.ErrorMassage_ManyDownloads);
            }

            var tenant = tenantManager.GetCurrentTenant();
            var op1 = new FileDownloadOperation<int>(ServiceProvider, new FileDownloadOperationData<int>(folders.Where(r => r.Key is long).ToDictionary(r => Convert.ToInt32(r.Key), r => r.Value), files.Where(r => r.Key is long).ToDictionary(r => Convert.ToInt32(r.Key), r => r.Value), tenant, headers), false);
            var op2 = new FileDownloadOperation<string>(ServiceProvider, new FileDownloadOperationData<string>(folders.Where(r => r.Key is string).ToDictionary(r => r.Key.ToString(), r => r.Value), files.Where(r => r.Key is string).ToDictionary(r => r.Key.ToString(), r => r.Value), tenant, headers), false);
            var op = new FileDownloadOperation(ServiceProvider, op2, op1);

            return QueueTask(authContext, op);
        }

        public ItemList<FileOperationResult> MoveOrCopy(AuthContext authContext, TenantManager tenantManager, IEnumerable<object> folders, IEnumerable<object> files, object destFolderId, bool copy, FileConflictResolveType resolveType, bool holdResult, Dictionary<string, string> headers)
        {
            var tenant = tenantManager.GetCurrentTenant();
            var op1 = new FileMoveCopyOperation<int>(ServiceProvider, new FileMoveCopyOperationData<int>(folders.OfType<long>().Select(Convert.ToInt32), files.OfType<long>().Select(Convert.ToInt32), tenant, destFolderId, copy, resolveType, holdResult, headers));
            var op2 = new FileMoveCopyOperation<string>(ServiceProvider, new FileMoveCopyOperationData<string>(folders, files, tenant, destFolderId, copy, resolveType, holdResult, headers));
            var op = new FileMoveCopyOperation(ServiceProvider, op2, op1);

            return QueueTask(authContext, op);
        }

        public ItemList<FileOperationResult> Delete<T>(AuthContext authContext, TenantManager tenantManager, IEnumerable<T> folders, IEnumerable<T> files, bool ignoreException, bool holdResult, bool immediately, Dictionary<string, string> headers)
        {
            var op = new FileDeleteOperation<T>(ServiceProvider, new FileDeleteOperationData<T>(folders, files, tenantManager.GetCurrentTenant(), holdResult, ignoreException, immediately, headers));
            return QueueTask(authContext, op);
        }

        public ItemList<FileOperationResult> Delete(AuthContext authContext, TenantManager tenantManager, IEnumerable<object> folders, IEnumerable<object> files, bool ignoreException, bool holdResult, bool immediately, Dictionary<string, string> headers)
        {
            var op1 = new FileDeleteOperation<int>(ServiceProvider, new FileDeleteOperationData<int>(folders.OfType<long>().Select(Convert.ToInt32), files.OfType<long>().Select(Convert.ToInt32), tenantManager.GetCurrentTenant(), holdResult, ignoreException, immediately, headers));
            var op2 = new FileDeleteOperation<string>(ServiceProvider, new FileDeleteOperationData<string>(folders, files, tenantManager.GetCurrentTenant(), holdResult, ignoreException, immediately, headers));
            var op = new FileDeleteOperation(ServiceProvider, op2, op1);

            return QueueTask(authContext, op);
        }


        private ItemList<FileOperationResult> QueueTask(AuthContext authContext, FileOperation op)
        {
            tasks.QueueTask(op.RunJob, op.GetDistributedTask());
            return GetOperationResults(authContext);
        }

        private ItemList<FileOperationResult> QueueTask<T, TId>(AuthContext authContext, FileOperation<T, TId> op) where T : FileOperationData<TId>
        {
            tasks.QueueTask(op.RunJob, op.GetDistributedTask());
            return GetOperationResults(authContext);
        }
    }

    public class FileOperationsManagerHelper
    {
        public FileOperationsManager FileOperationsManager { get; }
        public AuthContext AuthContext { get; }
        public TenantManager TenantManager { get; }

        public FileOperationsManagerHelper(
            FileOperationsManager fileOperationsManager,
            AuthContext authContext,
            TenantManager tenantManager)
        {
            FileOperationsManager = fileOperationsManager;
            AuthContext = authContext;
            TenantManager = tenantManager;
        }

        public ItemList<FileOperationResult> GetOperationResults()
        {
            return FileOperationsManager.GetOperationResults(AuthContext);
        }

        public ItemList<FileOperationResult> CancelOperations()
        {
            return FileOperationsManager.CancelOperations(AuthContext);
        }

        public ItemList<FileOperationResult> MarkAsRead(IEnumerable<object> folderIds, IEnumerable<object> fileIds)
        {
            return FileOperationsManager.MarkAsRead(AuthContext, TenantManager, folderIds, fileIds);
        }

        public ItemList<FileOperationResult> Download(Dictionary<object, string> folders, Dictionary<object, string> files, Dictionary<string, string> headers)
        {
            return FileOperationsManager.Download(AuthContext, TenantManager, folders, files, headers);
        }

        public ItemList<FileOperationResult> MoveOrCopy(IEnumerable<object> folders, IEnumerable<object> files, object destFolderId, bool copy, FileConflictResolveType resolveType, bool holdResult, Dictionary<string, string> headers)
        {
            return FileOperationsManager.MoveOrCopy(AuthContext, TenantManager, folders, files, destFolderId, copy, resolveType, holdResult, headers);
        }

        public ItemList<FileOperationResult> Delete(List<object> folders, List<object> files, bool ignoreException, bool holdResult, bool immediately, Dictionary<string, string> headers)
        {
            return FileOperationsManager.Delete(AuthContext, TenantManager, folders, files, ignoreException, holdResult, immediately, headers);
        }

        public ItemList<FileOperationResult> Delete<T>(List<T> folders, List<T> files, bool ignoreException, bool holdResult, bool immediately, Dictionary<string, string> headers)
        {
            return FileOperationsManager.Delete(AuthContext, TenantManager, folders, files, ignoreException, holdResult, immediately, headers);
        }

        public ItemList<FileOperationResult> DeleteFile<T>(T file, bool ignoreException, bool holdResult, bool immediately, Dictionary<string, string> headers)
        {
            return Delete(new List<T>(), new List<T>() { file }, ignoreException, holdResult, immediately, headers);
        }

        public ItemList<FileOperationResult> DeleteFolder<T>(T folder, bool ignoreException, bool holdResult, bool immediately, Dictionary<string, string> headers)
        {
            return Delete(new List<T>() { folder }, new List<T>(), ignoreException, holdResult, immediately, headers);
        }
    }

    public static class FileOperationsManagerHelperExtention
    {
        public static DIHelper AddFileOperationsManagerHelperService(this DIHelper services)
        {
            services.TryAddSingleton<DistributedTaskCacheNotify>();
            services.TryAddSingleton<FileOperationsManager>();
            services.TryAddScoped<FileOperationsManagerHelper>();

            return services
                .AddAuthContextService()
                .AddTenantManagerService()
                ;
        }
    }
}