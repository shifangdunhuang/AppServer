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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;

using ASC.Api.Core;
using ASC.Api.Utils;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.Configuration;
using ASC.Core.Users;
using ASC.FederatedLogin.Helpers;
using ASC.FederatedLogin.LoginProviders;
using ASC.Files.Core;
using ASC.Files.Helpers;
using ASC.Files.Model;
using ASC.MessagingSystem;
using ASC.Web.Api.Routing;
using ASC.Web.Core;
using ASC.Web.Core.Files;
using ASC.Web.Files.Classes;
using ASC.Web.Files.Configuration;
using ASC.Web.Files.Helpers;
using ASC.Web.Files.Services.DocumentService;
using ASC.Web.Files.Services.WCFService;
using ASC.Web.Files.Services.WCFService.FileOperations;
using ASC.Web.Files.Utils;
using ASC.Web.Studio.Utility;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

using Newtonsoft.Json.Linq;

using FileShare = ASC.Files.Core.Security.FileShare;
using SortedByType = ASC.Files.Core.SortedByType;

namespace ASC.Api.Documents
{
    /// <summary>
    /// Provides access to documents
    /// </summary>
    [DefaultRoute]
    [ApiController]
    public class FilesController : ControllerBase
    {
        private readonly ApiContext ApiContext;
        private readonly FileStorageService<string> FileStorageService;

        public FilesControllerHelper<string> FilesControllerHelperString { get; }
        public FilesControllerHelper<int> FilesControllerHelperInt { get; }
        public FileStorageService<int> FileStorageServiceInt { get; }
        public GlobalFolderHelper GlobalFolderHelper { get; }
        public FilesSettingsHelper FilesSettingsHelper { get; }
        public FilesLinkUtility FilesLinkUtility { get; }
        public SecurityContext SecurityContext { get; }
        public FolderWrapperHelper FolderWrapperHelper { get; }
        public FileOperationWraperHelper FileOperationWraperHelper { get; }
        public EntryManager EntryManager { get; }
        public UserManager UserManager { get; }
        public WebItemSecurity WebItemSecurity { get; }
        public CoreBaseSettings CoreBaseSettings { get; }
        public ThirdpartyConfiguration ThirdpartyConfiguration { get; }
        public BoxLoginProvider BoxLoginProvider { get; }
        public DropboxLoginProvider DropboxLoginProvider { get; }
        public GoogleLoginProvider GoogleLoginProvider { get; }
        public OneDriveLoginProvider OneDriveLoginProvider { get; }
        public MessageService MessageService { get; }
        public CommonLinkUtility CommonLinkUtility { get; }
        public DocumentServiceConnector DocumentServiceConnector { get; }
        public FolderContentWrapperHelper FolderContentWrapperHelper { get; }
        public WordpressToken WordpressToken { get; }
        public WordpressHelper WordpressHelper { get; }
        public ConsumerFactory ConsumerFactory { get; }
        public EasyBibHelper EasyBibHelper { get; }
        public ProductEntryPoint ProductEntryPoint { get; }

        /// <summary>
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fileStorageService"></param>
        public FilesController(
            ApiContext context,
            FilesControllerHelper<string> filesControllerHelperString,
            FilesControllerHelper<int> filesControllerHelperInt,
            FileStorageService<string> fileStorageService,
            FileStorageService<int> fileStorageServiceInt,
            GlobalFolderHelper globalFolderHelper,
            FilesSettingsHelper filesSettingsHelper,
            FilesLinkUtility filesLinkUtility,
            SecurityContext securityContext,
            FolderWrapperHelper folderWrapperHelper,
            FileOperationWraperHelper fileOperationWraperHelper,
            EntryManager entryManager,
            UserManager userManager,
            WebItemSecurity webItemSecurity,
            CoreBaseSettings coreBaseSettings,
            ThirdpartyConfiguration thirdpartyConfiguration,
            MessageService messageService,
            CommonLinkUtility commonLinkUtility,
            DocumentServiceConnector documentServiceConnector,
            FolderContentWrapperHelper folderContentWrapperHelper,
            WordpressToken wordpressToken,
            WordpressHelper wordpressHelper,
            ConsumerFactory consumerFactory,
            EasyBibHelper easyBibHelper,
            ProductEntryPoint productEntryPoint)
        {
            ApiContext = context;
            FilesControllerHelperString = filesControllerHelperString;
            FilesControllerHelperInt = filesControllerHelperInt;
            FileStorageService = fileStorageService;
            FileStorageServiceInt = fileStorageServiceInt;
            GlobalFolderHelper = globalFolderHelper;
            FilesSettingsHelper = filesSettingsHelper;
            FilesLinkUtility = filesLinkUtility;
            SecurityContext = securityContext;
            FolderWrapperHelper = folderWrapperHelper;
            FileOperationWraperHelper = fileOperationWraperHelper;
            EntryManager = entryManager;
            UserManager = userManager;
            WebItemSecurity = webItemSecurity;
            CoreBaseSettings = coreBaseSettings;
            ThirdpartyConfiguration = thirdpartyConfiguration;
            ConsumerFactory = consumerFactory;
            BoxLoginProvider = ConsumerFactory.Get<BoxLoginProvider>();
            DropboxLoginProvider = ConsumerFactory.Get<DropboxLoginProvider>();
            GoogleLoginProvider = ConsumerFactory.Get<GoogleLoginProvider>();
            OneDriveLoginProvider = ConsumerFactory.Get<OneDriveLoginProvider>();
            MessageService = messageService;
            CommonLinkUtility = commonLinkUtility;
            DocumentServiceConnector = documentServiceConnector;
            FolderContentWrapperHelper = folderContentWrapperHelper;
            WordpressToken = wordpressToken;
            WordpressHelper = wordpressHelper;
            EasyBibHelper = easyBibHelper;
            ProductEntryPoint = productEntryPoint;
        }

        [Read("info")]
        public Module GetModule()
        {
            ProductEntryPoint.Init();
            return new Module(ProductEntryPoint, true);
        }

        /// <summary>
        /// Returns the detailed list of files and folders located in the current user 'My Documents' section
        /// </summary>
        /// <short>
        /// My folder
        /// </short>
        /// <category>Folders</category>
        /// <returns>My folder contents</returns>
        [Read("@my")]
        public FolderContentWrapper<int> GetMyFolder(Guid userIdOrGroupId, FilterType filterType, bool withsubfolders)
        {
            return ToFolderContentWrapper(GlobalFolderHelper.FolderMy, userIdOrGroupId, filterType, withsubfolders);
        }

        /// <summary>
        /// Returns the detailed list of files and folders located in the current user 'Projects Documents' section
        /// </summary>
        /// <short>
        /// Projects folder
        /// </short>
        /// <category>Folders</category>
        /// <returns>Projects folder contents</returns>
        [Read("@projects")]
        public FolderContentWrapper<string> GetProjectsFolder(Guid userIdOrGroupId, FilterType filterType, bool withsubfolders)
        {
            return ToFolderContentWrapper(GlobalFolderHelper.GetFolderProjects<string>(), userIdOrGroupId, filterType, withsubfolders);
        }


        /// <summary>
        /// Returns the detailed list of files and folders located in the 'Common Documents' section
        /// </summary>
        /// <short>
        /// Common folder
        /// </short>
        /// <category>Folders</category>
        /// <returns>Common folder contents</returns>
        [Read("@common")]
        public FolderContentWrapper<int> GetCommonFolder(Guid userIdOrGroupId, FilterType filterType, bool withsubfolders)
        {
            return ToFolderContentWrapper(GlobalFolderHelper.FolderCommon, userIdOrGroupId, filterType, withsubfolders);
        }

        /// <summary>
        /// Returns the detailed list of files and folders located in the 'Shared with Me' section
        /// </summary>
        /// <short>
        /// Shared folder
        /// </short>
        /// <category>Folders</category>
        /// <returns>Shared folder contents</returns>
        [Read("@share")]
        public FolderContentWrapper<int> GetShareFolder(Guid userIdOrGroupId, FilterType filterType, bool withsubfolders)
        {
            return ToFolderContentWrapper(GlobalFolderHelper.FolderShare, userIdOrGroupId, filterType, withsubfolders);
        }

        /// <summary>
        /// Returns the detailed list of files and folders located in the 'Recycle Bin' section
        /// </summary>
        /// <short>
        /// Trash folder
        /// </short>
        /// <category>Folders</category>
        /// <returns>Trash folder contents</returns>
        [Read("@trash")]
        public FolderContentWrapper<int> GetTrashFolder(Guid userIdOrGroupId, FilterType filterType, bool withsubfolders)
        {
            return ToFolderContentWrapper(Convert.ToInt32(GlobalFolderHelper.FolderTrash), userIdOrGroupId, filterType, withsubfolders);
        }

        /// <summary>
        /// Returns the detailed list of files and folders located in the folder with the ID specified in the request
        /// </summary>
        /// <short>
        /// Folder by ID
        /// </short>
        /// <category>Folders</category>
        /// <param name="folderId">Folder ID</param>
        /// <param name="userIdOrGroupId" optional="true">User or group ID</param>
        /// <param name="filterType" optional="true" remark="Allowed values: None (0), FilesOnly (1), FoldersOnly (2), DocumentsOnly (3), PresentationsOnly (4), SpreadsheetsOnly (5) or ImagesOnly (7)">Filter type</param>
        /// <returns>Folder contents</returns>
        [Read("{folderId}", order: int.MaxValue, DisableFormat = true)]
        public FolderContentWrapper<string> GetFolder(string folderId, Guid userIdOrGroupId, FilterType filterType, bool withsubfolders)
        {
            return FilesControllerHelperString.GetFolder(folderId, userIdOrGroupId, filterType, withsubfolders).NotFoundIfNull();
        }

        [Read("{folderId:int}", order: int.MaxValue - 1)]
        public FolderContentWrapper<int> GetFolder(int folderId, Guid userIdOrGroupId, FilterType filterType, bool withsubfolders)
        {
            return FilesControllerHelperInt.GetFolder(folderId, userIdOrGroupId, filterType, withsubfolders);
        }

        /// <summary>
        /// Uploads the file specified with single file upload or standart multipart/form-data method to 'My Documents' section
        /// </summary>
        /// <short>Upload to My</short>
        /// <category>Uploads</category>
        /// <remarks>
        /// <![CDATA[
        ///  Upload can be done in 2 different ways:
        ///  <ol>
        /// <li>Single file upload. You should set Content-Type &amp; Content-Disposition header to specify filename and content type, and send file in request body</li>
        /// <li>Using standart multipart/form-data method</li>
        /// </ol>]]>
        /// </remarks>
        /// <param name="file" visible="false">Request Input stream</param>
        /// <param name="contentType" visible="false">Content-Type Header</param>
        /// <param name="contentDisposition" visible="false">Content-Disposition Header</param>
        /// <param name="files" visible="false">List of files when posted as multipart/form-data</param>
        /// <returns>Uploaded file</returns>
        [Create("@my/upload")]
        public List<FileWrapper<int>> UploadFileToMy(UploadModel uploadModel)
        {
            uploadModel.CreateNewIfExist = false;
            return UploadFile(GlobalFolderHelper.FolderMy, uploadModel);
        }

        /// <summary>
        /// Uploads the file specified with single file upload or standart multipart/form-data method to 'Common Documents' section
        /// </summary>
        /// <short>Upload to Common</short>
        /// <category>Uploads</category>
        /// <remarks>
        /// <![CDATA[
        ///  Upload can be done in 2 different ways:
        ///  <ol>
        /// <li>Single file upload. You should set Content-Type &amp; Content-Disposition header to specify filename and content type, and send file in request body</li>
        /// <li>Using standart multipart/form-data method</li>
        /// </ol>]]>
        /// </remarks>
        /// <param name="file" visible="false">Request Input stream</param>
        /// <param name="contentType" visible="false">Content-Type Header</param>
        /// <param name="contentDisposition" visible="false">Content-Disposition Header</param>
        /// <param name="files" visible="false">List of files when posted as multipart/form-data</param>
        /// <returns>Uploaded file</returns>
        [Create("@common/upload")]
        public List<FileWrapper<int>> UploadFileToCommon(UploadModel uploadModel)
        {
            uploadModel.CreateNewIfExist = false;
            return UploadFile(GlobalFolderHelper.FolderCommon, uploadModel);
        }


        /// <summary>
        /// Uploads the file specified with single file upload or standart multipart/form-data method to the selected folder
        /// </summary>
        /// <short>Upload to folder</short>
        /// <category>Uploads</category>
        /// <remarks>
        /// <![CDATA[
        ///  Upload can be done in 2 different ways:
        ///  <ol>
        /// <li>Single file upload. You should set Content-Type &amp; Content-Disposition header to specify filename and content type, and send file in request body</li>
        /// <li>Using standart multipart/form-data method</li>
        /// </ol>]]>
        /// </remarks>
        /// <param name="folderId">Folder ID to upload to</param>
        /// <param name="file" visible="false">Request Input stream</param>
        /// <param name="contentType" visible="false">Content-Type Header</param>
        /// <param name="contentDisposition" visible="false">Content-Disposition Header</param>
        /// <param name="files" visible="false">List of files when posted as multipart/form-data</param>
        /// <param name="createNewIfExist" visible="false">Create New If Exist</param>
        /// <param name="storeOriginalFileFlag" visible="false">If True, upload documents in original formats as well</param>
        /// <param name="keepConvertStatus" visible="false">Keep status conversation after finishing</param>
        /// <returns>Uploaded file</returns>
        [Create("{folderId}/upload", DisableFormat = true)]
        public List<FileWrapper<string>> UploadFile(string folderId, UploadModel uploadModel)
        {
            return FilesControllerHelperString.UploadFile(folderId, uploadModel);
        }

        [Create("{folderId:int}/upload")]
        public List<FileWrapper<int>> UploadFile(int folderId, UploadModel uploadModel)
        {
            return FilesControllerHelperInt.UploadFile(folderId, uploadModel);
        }

        /// <summary>
        /// Uploads the file specified with single file upload to 'Common Documents' section
        /// </summary>
        /// <param name="file" visible="false">Request Input stream</param>
        /// <param name="title">Name of file which has to be uploaded</param>
        /// <param name="createNewIfExist" visible="false">Create New If Exist</param>
        /// <param name="keepConvertStatus" visible="false">Keep status conversation after finishing</param>
        /// <category>Uploads</category>
        /// <returns></returns>
        [Create("@my/insert")]
        public FileWrapper<int> InsertFileToMy(Stream file, string title, bool? createNewIfExist, bool keepConvertStatus = false)
        {
            return InsertFile(GlobalFolderHelper.FolderMy, file, title, createNewIfExist, keepConvertStatus);
        }

        /// <summary>
        /// Uploads the file specified with single file upload to 'Common Documents' section
        /// </summary>
        /// <param name="file" visible="false">Request Input stream</param>
        /// <param name="title">Name of file which has to be uploaded</param>
        /// <param name="createNewIfExist" visible="false">Create New If Exist</param>
        /// <param name="keepConvertStatus" visible="false">Keep status conversation after finishing</param>
        /// <category>Uploads</category>
        /// <returns></returns>
        [Create("@common/insert")]
        public FileWrapper<int> InsertFileToCommon(Stream file, string title, bool? createNewIfExist, bool keepConvertStatus = false)
        {
            return InsertFile(GlobalFolderHelper.FolderCommon, file, title, createNewIfExist, keepConvertStatus);
        }

        /// <summary>
        /// Uploads the file specified with single file upload
        /// </summary>
        /// <param name="folderId">Folder ID to upload to</param>
        /// <param name="file" visible="false">Request Input stream</param>
        /// <param name="title">Name of file which has to be uploaded</param>
        /// <param name="createNewIfExist" visible="false">Create New If Exist</param>
        /// <param name="keepConvertStatus" visible="false">Keep status conversation after finishing</param>
        /// <category>Uploads</category>
        /// <returns></returns>
        [Create("{folderId}/insert", DisableFormat = true)]
        public FileWrapper<string> InsertFile(string folderId, Stream file, string title, bool? createNewIfExist, bool keepConvertStatus = false)
        {
            return FilesControllerHelperString.InsertFile(folderId, file, title, createNewIfExist, keepConvertStatus);
        }

        [Create("{folderId:int}/insert")]
        public FileWrapper<int> InsertFile(int folderId, Stream file, string title, bool? createNewIfExist, bool keepConvertStatus = false)
        {
            return FilesControllerHelperInt.InsertFile(folderId, file, title, createNewIfExist, keepConvertStatus);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileId"></param>
        /// <param name="encrypted"></param>
        /// <returns></returns>
        /// <visible>false</visible>
        [Update("{fileId}/update", DisableFormat = true)]
        public FileWrapper<string> UpdateFileStream(Stream file, string fileId, bool encrypted = false)
        {
            return FilesControllerHelperString.UpdateFileStream(file, fileId, encrypted);
        }

        [Update("{fileId:int}/update")]
        public FileWrapper<int> UpdateFileStream(Stream file, int fileId, bool encrypted = false)
        {
            return FilesControllerHelperInt.UpdateFileStream(file, fileId, encrypted);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="fileExtension"></param>
        /// <param name="downloadUri"></param>
        /// <param name="stream"></param>
        /// <param name="doc"></param>
        /// <param name="forcesave"></param>
        /// <category>Files</category>
        /// <returns></returns>
        [Update("file/{fileId}/saveediting", DisableFormat = true)]
        public FileWrapper<string> SaveEditing(string fileId, string fileExtension, string downloadUri, Stream stream, string doc, bool forcesave)
        {
            return FilesControllerHelperString.SaveEditing(fileId, fileExtension, downloadUri, stream, doc, forcesave);
        }

        [Update("file/{fileId:int}/saveediting")]
        public FileWrapper<int> SaveEditing(int fileId, string fileExtension, string downloadUri, Stream stream, string doc, bool forcesave)
        {
            return FilesControllerHelperInt.SaveEditing(fileId, fileExtension, downloadUri, stream, doc, forcesave);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="editingAlone"></param>
        /// <param name="doc"></param>
        /// <category>Files</category>
        /// <returns></returns>
        [Create("file/{fileId}/startedit", DisableFormat = true)]
        public string StartEdit(string fileId, bool editingAlone, string doc)
        {
            return FilesControllerHelperString.StartEdit(fileId, editingAlone, doc);
        }

        [Create("file/{fileId:int}/startedit")]
        public string StartEdit(int fileId, bool editingAlone, string doc)
        {
            return FilesControllerHelperInt.StartEdit(fileId, editingAlone, doc);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="tabId"></param>
        /// <param name="docKeyForTrack"></param>
        /// <param name="doc"></param>
        /// <param name="isFinish"></param>
        /// <category>Files</category>
        /// <returns></returns>
        [Read("file/{fileId}/trackeditfile", DisableFormat = true)]
        public KeyValuePair<bool, string> TrackEditFile(string fileId, Guid tabId, string docKeyForTrack, string doc, bool isFinish)
        {
            return FilesControllerHelperString.TrackEditFile(fileId, tabId, docKeyForTrack, doc, isFinish);
        }

        [Read("file/{fileId:int}/trackeditfile")]
        public KeyValuePair<bool, string> TrackEditFile(int fileId, Guid tabId, string docKeyForTrack, string doc, bool isFinish)
        {
            return FilesControllerHelperInt.TrackEditFile(fileId, tabId, docKeyForTrack, doc, isFinish);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="version"></param>
        /// <param name="doc"></param>
        /// <category>Files</category>
        /// <returns></returns>
        [Read("file/{fileId}/openedit", DisableFormat = true)]
        public Configuration<string> OpenEdit(string fileId, int version, string doc)
        {
            return FilesControllerHelperString.OpenEdit(fileId, version, doc);
        }

        [Read("file/{fileId:int}/openedit")]
        public Configuration<int> OpenEdit(int fileId, int version, string doc)
        {
            return FilesControllerHelperInt.OpenEdit(fileId, version, doc);
        }


        /// <summary>
        /// Creates session to upload large files in multiple chunks.
        /// </summary>
        /// <short>Chunked upload</short>
        /// <category>Uploads</category>
        /// <param name="folderId">Id of the folder in which file will be uploaded</param>
        /// <param name="fileName">Name of file which has to be uploaded</param>
        /// <param name="fileSize">Length in bytes of file which has to be uploaded</param>
        /// <param name="relativePath">Relative folder from folderId</param>
        /// <param name="encrypted" visible="false"></param>
        /// <remarks>
        /// <![CDATA[
        /// Each chunk can have different length but its important what length is multiple of <b>512</b> and greater or equal than <b>5 mb</b>. Last chunk can have any size.
        /// After initial request respond with status 200 OK you must obtain value of 'location' field from the response. Send all your chunks to that location.
        /// Each chunk must be sent in strict order in which chunks appears in file.
        /// After receiving each chunk if no errors occured server will respond with current information about upload session.
        /// When number of uploaded bytes equal to the number of bytes you send in initial request server will respond with 201 Created and will send you info about uploaded file.
        /// ]]>
        /// </remarks>
        /// <returns>
        /// <![CDATA[
        /// Information about created session. Which includes:
        /// <ul>
        /// <li><b>id:</b> unique id of this upload session</li>
        /// <li><b>created:</b> UTC time when session was created</li>
        /// <li><b>expired:</b> UTC time when session will be expired if no chunks will be sent until that time</li>
        /// <li><b>location:</b> URL to which you must send your next chunk</li>
        /// <li><b>bytes_uploaded:</b> If exists contains number of bytes uploaded for specific upload id</li>
        /// <li><b>bytes_total:</b> Number of bytes which has to be uploaded</li>
        /// </ul>
        /// ]]>
        /// </returns>
        [Create("{folderId}/upload/create_session", DisableFormat = true)]
        public object CreateUploadSession(string folderId, string fileName, long fileSize, string relativePath, bool encrypted)
        {
            return FilesControllerHelperString.CreateUploadSession(folderId, fileName, fileSize, relativePath, encrypted);
        }

        [Create("{folderId:int}/upload/create_session")]
        public object CreateUploadSession(int folderId, string fileName, long fileSize, string relativePath, bool encrypted)
        {
            return FilesControllerHelperInt.CreateUploadSession(folderId, fileName, fileSize, relativePath, encrypted);
        }

        /// <summary>
        /// Creates a text (.txt) file in 'My Documents' section with the title and contents sent in the request
        /// </summary>
        /// <short>Create txt in 'My'</short>
        /// <category>File Creation</category>
        /// <param name="title">File title</param>
        /// <param name="content">File contents</param>
        /// <returns>Folder contents</returns>
        [Create("@my/text")]
        public FileWrapper<int> CreateTextFileInMy(string title, string content)
        {
            return CreateTextFile(GlobalFolderHelper.FolderMy, title, content);
        }

        /// <summary>
        /// Creates a text (.txt) file in 'Common Documents' section with the title and contents sent in the request
        /// </summary>
        /// <short>Create txt in 'Common'</short>
        /// <category>File Creation</category>
        /// <param name="title">File title</param>
        /// <param name="content">File contents</param>
        /// <returns>Folder contents</returns>
        [Create("@common/text")]
        public FileWrapper<int> CreateTextFileInCommon(string title, string content)
        {
            return CreateTextFile(GlobalFolderHelper.FolderCommon, title, content);
        }

        /// <summary>
        /// Creates a text (.txt) file in the selected folder with the title and contents sent in the request
        /// </summary>
        /// <short>Create txt</short>
        /// <category>File Creation</category>
        /// <param name="folderId">Folder ID</param>
        /// <param name="title">File title</param>
        /// <param name="content">File contents</param>
        /// <returns>Folder contents</returns>
        [Create("{folderId}/text", DisableFormat = true)]
        public FileWrapper<string> CreateTextFile(string folderId, string title, string content)
        {
            return FilesControllerHelperString.CreateTextFile(folderId, title, content);
        }

        [Create("{folderId:int}/text")]
        public FileWrapper<int> CreateTextFile(int folderId, string title, string content)
        {
            return FilesControllerHelperInt.CreateTextFile(folderId, title, content);
        }

        /// <summary>
        /// Creates an html (.html) file in the selected folder with the title and contents sent in the request
        /// </summary>
        /// <short>Create html</short>
        /// <category>File Creation</category>
        /// <param name="folderId">Folder ID</param>
        /// <param name="title">File title</param>
        /// <param name="content">File contents</param>
        /// <returns>Folder contents</returns>
        [Create("{folderId}/html", DisableFormat = true)]
        public FileWrapper<string> CreateHtmlFile(string folderId, string title, string content)
        {
            return FilesControllerHelperString.CreateHtmlFile(folderId, title, content);
        }

        [Create("{folderId:int}/html")]
        public FileWrapper<int> CreateHtmlFile(int folderId, string title, string content)
        {
            return FilesControllerHelperInt.CreateHtmlFile(folderId, title, content);
        }

        /// <summary>
        /// Creates an html (.html) file in 'My Documents' section with the title and contents sent in the request
        /// </summary>
        /// <short>Create html in 'My'</short>
        /// <category>File Creation</category>
        /// <param name="title">File title</param>
        /// <param name="content">File contents</param>
        /// <returns>Folder contents</returns>
        [Create("@my/html")]
        public FileWrapper<int> CreateHtmlFileInMy(string title, string content)
        {
            return CreateHtmlFile(GlobalFolderHelper.FolderMy, title, content);
        }


        /// <summary>
        /// Creates an html (.html) file in 'Common Documents' section with the title and contents sent in the request
        /// </summary>
        /// <short>Create html in 'Common'</short>
        /// <category>File Creation</category>
        /// <param name="title">File title</param>
        /// <param name="content">File contents</param>
        /// <returns>Folder contents</returns>        
        [Create("@common/html")]
        public FileWrapper<int> CreateHtmlFileInCommon(string title, string content)
        {
            return CreateHtmlFile(GlobalFolderHelper.FolderCommon, title, content);
        }


        /// <summary>
        /// Creates a new folder with the title sent in the request. The ID of a parent folder can be also specified.
        /// </summary>
        /// <short>
        /// New folder
        /// </short>
        /// <category>Folders</category>
        /// <param name="folderId">Parent folder ID</param>
        /// <param name="title">Title of new folder</param>
        /// <returns>New folder contents</returns>
        [Create("folder/{folderId}", DisableFormat = true)]
        public FolderWrapper<string> CreateFolder(string folderId, string title)
        {
            return FilesControllerHelperString.CreateFolder(folderId, title);
        }

        [Create("folder/{folderId:int}")]
        public FolderWrapper<int> CreateFolder(int folderId, string title)
        {
            return FilesControllerHelperInt.CreateFolder(folderId, title);
        }


        /// <summary>
        /// Creates a new file in the 'My Documents' section with the title sent in the request
        /// </summary>
        /// <short>Create file</short>
        /// <category>File Creation</category>
        /// <param name="title" remark="Allowed values: the file must have one of the following extensions: DOCX, XLSX, PPTX">File title</param>
        /// <remarks>In case the extension for the file title differs from DOCX/XLSX/PPTX and belongs to one of the known text, spreadsheet or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not set or is unknown, the DOCX extension will be added to the file title.</remarks>
        /// <returns>New file info</returns>
        [Create("@my/file")]
        public FileWrapper<int> CreateFile(string title)
        {
            return CreateFile(GlobalFolderHelper.FolderMy, title);
        }

        /// <summary>
        /// Creates a new file in the specified folder with the title sent in the request
        /// </summary>
        /// <short>Create file</short>
        /// <category>File Creation</category>
        /// <param name="folderId">Folder ID</param>
        /// <param name="title" remark="Allowed values: the file must have one of the following extensions: DOCX, XLSX, PPTX">File title</param>
        /// <remarks>In case the extension for the file title differs from DOCX/XLSX/PPTX and belongs to one of the known text, spreadsheet or presentation formats, it will be changed to DOCX/XLSX/PPTX accordingly. If the file extension is not set or is unknown, the DOCX extension will be added to the file title.</remarks>
        /// <returns>New file info</returns>
        [Create("{folderId}/file", DisableFormat = true)]
        public FileWrapper<string> CreateFile(string folderId, string title)
        {
            return FilesControllerHelperString.CreateFile(folderId, title);
        }

        [Create("{folderId:int}/file")]
        public FileWrapper<int> CreateFile(int folderId, string title)
        {
            return FilesControllerHelperInt.CreateFile(folderId, title);
        }

        /// <summary>
        /// Renames the selected folder to the new title specified in the request
        /// </summary>
        /// <short>
        /// Rename folder
        /// </short>
        /// <category>Folders</category>
        /// <param name="folderId">Folder ID</param>
        /// <param name="title">New title</param>
        /// <returns>Folder contents</returns>
        [Update("folder/{folderId}", DisableFormat = true)]
        public FolderWrapper<string> RenameFolder(string folderId, string title)
        {
            return FilesControllerHelperString.RenameFolder(folderId, title);
        }

        [Update("folder/{folderId:int}")]
        public FolderWrapper<int> RenameFolder(int folderId, string title)
        {
            return FilesControllerHelperInt.RenameFolder(folderId, title);
        }

        /// <summary>
        /// Returns a detailed information about the folder with the ID specified in the request
        /// </summary>
        /// <short>Folder information</short>
        /// <category>Folders</category>
        /// <returns>Folder info</returns>
        [Read("folder/{folderId}", DisableFormat = true)]
        public FolderWrapper<string> GetFolderInfo(string folderId)
        {
            return FilesControllerHelperString.GetFolderInfo(folderId);
        }

        [Read("folder/{folderId:int}")]
        public FolderWrapper<int> GetFolderInfo(int folderId)
        {
            return FilesControllerHelperInt.GetFolderInfo(folderId);
        }

        /// <summary>
        /// Returns parent folders
        /// </summary>
        /// <param name="folderId"></param>
        /// <category>Folders</category>
        /// <returns>Parent folders</returns>
        [Read("folder/{folderId}/path", DisableFormat = true)]
        public IEnumerable<FolderWrapper<string>> GetFolderPath(string folderId)
        {
            return FilesControllerHelperString.GetFolderPath(folderId);
        }

        [Read("folder/{folderId:int}/path")]
        public IEnumerable<FolderWrapper<int>> GetFolderPath(int folderId)
        {
            return FilesControllerHelperInt.GetFolderPath(folderId);
        }

        /// <summary>
        /// Returns a detailed information about the file with the ID specified in the request
        /// </summary>
        /// <short>File information</short>
        /// <category>Files</category>
        /// <returns>File info</returns>
        [Read("file/{fileId}", DisableFormat = true)]
        public FileWrapper<string> GetFileInfo(string fileId, int version = -1)
        {
            return FilesControllerHelperString.GetFileInfo(fileId, version);
        }

        [Read("file/{fileId:int}", DisableFormat = true)]
        public FileWrapper<int> GetFileInfo(int fileId, int version = -1)
        {
            return FilesControllerHelperInt.GetFileInfo(fileId, version);
        }

        /// <summary>
        ///     Updates the information of the selected file with the parameters specified in the request
        /// </summary>
        /// <short>Update file info</short>
        /// <category>Files</category>
        /// <param name="fileId">File ID</param>
        /// <param name="title">New title</param>
        /// <param name="lastVersion">File last version number</param>
        /// <returns>File info</returns>
        [Update("file/{fileId}", DisableFormat = true)]
        public FileWrapper<string> UpdateFile(string fileId, string title, int lastVersion)
        {
            return FilesControllerHelperString.UpdateFile(fileId, title, lastVersion);
        }

        [Update("file/{fileId:int}")]
        public FileWrapper<int> UpdateFile(int fileId, string title, int lastVersion)
        {
            return FilesControllerHelperInt.UpdateFile(fileId, title, lastVersion);
        }

        /// <summary>
        /// Deletes the file with the ID specified in the request
        /// </summary>
        /// <short>Delete file</short>
        /// <category>Files</category>
        /// <param name="fileId">File ID</param>
        /// <param name="deleteAfter">Delete after finished</param>
        /// <param name="immediately">Don't move to the Recycle Bin</param>
        /// <returns>Operation result</returns>
        [Delete("file/{fileId}", DisableFormat = true)]
        public IEnumerable<FileOperationWraper> DeleteFile(string fileId, bool deleteAfter, bool immediately)
        {
            return FilesControllerHelperString.DeleteFile(fileId, deleteAfter, immediately);
        }

        [Delete("file/{fileId:int}")]
        public IEnumerable<FileOperationWraper> DeleteFile(int fileId, bool deleteAfter, bool immediately)
        {
            return FilesControllerHelperInt.DeleteFile(fileId, deleteAfter, immediately);
        }

        /// <summary>
        ///  Start conversion
        /// </summary>
        /// <short>Convert</short>
        /// <category>File operations</category>
        /// <param name="fileId"></param>
        /// <returns>Operation result</returns>
        [Update("file/{fileId}/checkconversion", DisableFormat = true)]
        public IEnumerable<ConversationResult<string>> StartConversion(string fileId)
        {
            return FilesControllerHelperString.StartConversion(fileId);
        }

        [Update("file/{fileId:int}/checkconversion")]
        public IEnumerable<ConversationResult<int>> StartConversion(int fileId)
        {
            return FilesControllerHelperInt.StartConversion(fileId);
        }

        /// <summary>
        ///  Check conversion status
        /// </summary>
        /// <short>Convert</short>
        /// <category>File operations</category>
        /// <param name="fileId"></param>
        /// <param name="start"></param>
        /// <returns>Operation result</returns>
        [Read("file/{fileId}/checkconversion", DisableFormat = true)]
        public IEnumerable<ConversationResult<string>> CheckConversion(string fileId, bool start)
        {
            return FilesControllerHelperString.CheckConversion(fileId, start);
        }

        [Read("file/{fileId:int}/checkconversion")]
        public IEnumerable<ConversationResult<int>> CheckConversion(int fileId, bool start)
        {
            return FilesControllerHelperInt.CheckConversion(fileId, start);
        }

        /// <summary>
        /// Deletes the folder with the ID specified in the request
        /// </summary>
        /// <short>Delete folder</short>
        /// <category>Folders</category>
        /// <param name="folderId">Folder ID</param>
        /// <param name="deleteAfter">Delete after finished</param>
        /// <param name="immediately">Don't move to the Recycle Bin</param>
        /// <returns>Operation result</returns>
        [Delete("folder/{folderId}", DisableFormat = true)]
        public IEnumerable<FileOperationWraper> DeleteFolder(string folderId, bool deleteAfter, bool immediately)
        {
            return FilesControllerHelperString.DeleteFolder(folderId, deleteAfter, immediately);
        }

        [Delete("folder/{folderId:int}")]
        public IEnumerable<FileOperationWraper> DeleteFolder(int folderId, bool deleteAfter, bool immediately)
        {
            return FilesControllerHelperInt.DeleteFolder(folderId, deleteAfter, immediately);
        }

        /// <summary>
        /// Checking for conflicts
        /// </summary>
        /// <category>File operations</category>
        /// <param name="destFolderId">Destination folder ID</param>
        /// <param name="folderIds">Folder ID list</param>
        /// <param name="fileIds">File ID list</param>
        /// <returns>Conflicts file ids</returns>
        [Read("fileops/move")]
        public IEnumerable<FileEntryWrapper> MoveOrCopyBatchCheck(BatchModel batchModel)
        {
            return FilesControllerHelperString.MoveOrCopyBatchCheck(batchModel);
        }

        /// <summary>
        ///   Moves all the selected files and folders to the folder with the ID specified in the request
        /// </summary>
        /// <short>Move to folder</short>
        /// <category>File operations</category>
        /// <param name="destFolderId">Destination folder ID</param>
        /// <param name="folderIds">Folder ID list</param>
        /// <param name="fileIds">File ID list</param>
        /// <param name="conflictResolveType">Overwriting behavior: skip(0), overwrite(1) or duplicate(2)</param>
        /// <param name="deleteAfter">Delete after finished</param>
        /// <returns>Operation result</returns>
        [Update("fileops/move")]
        public IEnumerable<FileOperationWraper> MoveBatchItems(BatchModel batchModel)
        {
            return FilesControllerHelperString.MoveBatchItems(batchModel);
        }

        /// <summary>
        ///   Copies all the selected files and folders to the folder with the ID specified in the request
        /// </summary>
        /// <short>Copy to folder</short>
        /// <category>File operations</category>
        /// <param name="destFolderId">Destination folder ID</param>
        /// <param name="folderIds">Folder ID list</param>
        /// <param name="fileIds">File ID list</param>
        /// <param name="conflictResolveType">Overwriting behavior: skip(0), overwrite(1) or duplicate(2)</param>
        /// <param name="deleteAfter">Delete after finished</param>
        /// <returns>Operation result</returns>
        [Update("fileops/copy")]
        public IEnumerable<FileOperationWraper> CopyBatchItems(BatchModel batchModel)
        {
            return FilesControllerHelperString.CopyBatchItems(batchModel);
        }

        /// <summary>
        ///   Marks all files and folders as read
        /// </summary>
        /// <short>Mark as read</short>
        /// <category>File operations</category>
        /// <returns>Operation result</returns>
        [Update("fileops/markasread")]
        public IEnumerable<FileOperationWraper> MarkAsRead(BaseBatchModel<object> model)
        {
            return FilesControllerHelperString.MarkAsRead(model);
        }

        /// <summary>
        ///  Finishes all the active file operations
        /// </summary>
        /// <short>Finish all</short>
        /// <category>File operations</category>
        /// <returns>Operation result</returns>
        [Update("fileops/terminate")]
        public IEnumerable<FileOperationWraper> TerminateTasks()
        {
            return FileStorageService.TerminateTasks().Select(FileOperationWraperHelper.Get);
        }


        /// <summary>
        ///  Returns the list of all active file operations
        /// </summary>
        /// <short>Get file operations list</short>
        /// <category>File operations</category>
        /// <returns>Operation result</returns>
        [Read("fileops")]
        public IEnumerable<FileOperationWraper> GetOperationStatuses()
        {
            return FileStorageService.GetTasksStatuses().Select(FileOperationWraperHelper.Get);
        }

        /// <summary>
        /// Start downlaod process of files and folders with ID
        /// </summary>
        /// <short>Finish file operations</short>
        /// <param name="fileConvertIds" visible="false">File ID list for download with convert to format</param>
        /// <param name="fileIds">File ID list</param>
        /// <param name="folderIds">Folder ID list</param>
        /// <category>File operations</category>
        /// <returns>Operation result</returns>
        [Update("fileops/bulkdownload")]
        public IEnumerable<FileOperationWraper> BulkDownload(DownloadModel model)
        {
            return FilesControllerHelperString.BulkDownload(model);
        }

        /// <summary>
        ///   Deletes the files and folders with the IDs specified in the request
        /// </summary>
        /// <param name="folderIds">Folder ID list</param>
        /// <param name="fileIds">File ID list</param>
        /// <param name="deleteAfter">Delete after finished</param>
        /// <param name="immediately">Don't move to the Recycle Bin</param>
        /// <short>Delete files and folders</short>
        /// <category>File operations</category>
        /// <returns>Operation result</returns>
        [Update("fileops/delete")]
        public IEnumerable<FileOperationWraper> DeleteBatchItems(DeleteBatchModel batch)
        {
            return FileStorageService.DeleteItems("delete", batch.FileIds.ToList(), batch.FolderIds.ToList(), false, batch.DeleteAfter, batch.Immediately)
                .Select(FileOperationWraperHelper.Get);
        }

        /// <summary>
        ///   Deletes all files and folders from the recycle bin
        /// </summary>
        /// <short>Clear recycle bin</short>
        /// <category>File operations</category>
        /// <returns>Operation result</returns>
        [Update("fileops/emptytrash")]
        public IEnumerable<FileOperationWraper> EmptyTrash()
        {
            return FilesControllerHelperInt.EmptyTrash();
        }

        /// <summary>
        /// Returns the detailed information about all the available file versions with the ID specified in the request
        /// </summary>
        /// <short>File versions</short>
        /// <category>Files</category>
        /// <param name="fileId">File ID</param>
        /// <returns>File information</returns>
        [Read("file/{fileId}/history", DisableFormat = true)]
        public IEnumerable<FileWrapper<string>> GetFileVersionInfo(string fileId)
        {
            return FilesControllerHelperString.GetFileVersionInfo(fileId);
        }

        [Read("file/{fileId:int}/history")]
        public IEnumerable<FileWrapper<int>> GetFileVersionInfo(int fileId)
        {
            return FilesControllerHelperInt.GetFileVersionInfo(fileId);
        }

        /// <summary>
        /// Change version history
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="version">Version of history</param>
        /// <param name="continueVersion">Mark as version or revision</param>
        /// <category>Files</category>
        /// <returns></returns>
        [Update("file/{fileId}/history", DisableFormat = true)]
        public IEnumerable<FileWrapper<string>> ChangeHistory(string fileId, int version, bool continueVersion)
        {
            return FilesControllerHelperString.ChangeHistory(fileId, version, continueVersion);
        }
        [Update("file/{fileId:int}/history")]
        public IEnumerable<FileWrapper<int>> ChangeHistory(int fileId, int version, bool continueVersion)
        {
            return FilesControllerHelperInt.ChangeHistory(fileId, version, continueVersion);
        }

        /// <summary>
        /// Returns the detailed information about shared file with the ID specified in the request
        /// </summary>
        /// <short>File sharing</short>
        /// <category>Sharing</category>
        /// <param name="fileId">File ID</param>
        /// <returns>Shared file information</returns>
        [Read("file/{fileId}/share", DisableFormat = true)]
        public IEnumerable<FileShareWrapper> GetFileSecurityInfo(string fileId)
        {
            return FilesControllerHelperString.GetFileSecurityInfo(fileId);
        }

        [Read("file/{fileId:int}/share")]
        public IEnumerable<FileShareWrapper> GetFileSecurityInfo(int fileId)
        {
            return FilesControllerHelperInt.GetFileSecurityInfo(fileId);
        }

        /// <summary>
        /// Returns the detailed information about shared folder with the ID specified in the request
        /// </summary>
        /// <short>Folder sharing</short>
        /// <param name="folderId">Folder ID</param>
        /// <category>Sharing</category>
        /// <returns>Shared folder information</returns>
        [Read("folder/{folderId}/share", DisableFormat = true)]
        public IEnumerable<FileShareWrapper> GetFolderSecurityInfo(string folderId)
        {
            return FilesControllerHelperString.GetFolderSecurityInfo(folderId);
        }

        [Read("folder/{folderId:int}/share")]
        public IEnumerable<FileShareWrapper> GetFolderSecurityInfo(int folderId)
        {
            return FilesControllerHelperInt.GetFolderSecurityInfo(folderId);
        }

        /// <summary>
        /// Sets sharing settings for the file with the ID specified in the request
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="share">Collection of sharing rights</param>
        /// <param name="notify">Should notify people</param>
        /// <param name="sharingMessage">Sharing message to send when notifying</param>
        /// <short>Share file</short>
        /// <category>Sharing</category>
        /// <remarks>
        /// Each of the FileShareParams must contain two parameters: 'ShareTo' - ID of the user with whom we want to share and 'Access' - access type which we want to grant to the user (Read, ReadWrite, etc) 
        /// </remarks>
        /// <returns>Shared file information</returns>
        [Update("file/{fileId}/share", DisableFormat = true)]
        public IEnumerable<FileShareWrapper> SetFileSecurityInfo(string fileId, IEnumerable<FileShareParams> share, bool notify, string sharingMessage)
        {
            return FilesControllerHelperString.SetFileSecurityInfo(fileId, share, notify, sharingMessage);
        }

        [Update("file/{fileId:int}/share")]
        public IEnumerable<FileShareWrapper> SetFileSecurityInfo(int fileId, IEnumerable<FileShareParams> share, bool notify, string sharingMessage)
        {
            return FilesControllerHelperInt.SetFileSecurityInfo(fileId, share, notify, sharingMessage);
        }

        /// <summary>
        /// Sets sharing settings for the folder with the ID specified in the request
        /// </summary>
        /// <short>Share folder</short>
        /// <param name="folderId">Folder ID</param>
        /// <param name="share">Collection of sharing rights</param>
        /// <param name="notify">Should notify people</param>
        /// <param name="sharingMessage">Sharing message to send when notifying</param>
        /// <remarks>
        /// Each of the FileShareParams must contain two parameters: 'ShareTo' - ID of the user with whom we want to share and 'Access' - access type which we want to grant to the user (Read, ReadWrite, etc) 
        /// </remarks>
        /// <category>Sharing</category>
        /// <returns>Shared folder information</returns>
        [Update("folder/{folderId}/share", DisableFormat = true)]
        public IEnumerable<FileShareWrapper> SetFolderSecurityInfo(string folderId, IEnumerable<FileShareParams> share, bool notify, string sharingMessage)
        {
            return FilesControllerHelperString.SetFolderSecurityInfo(folderId, share, notify, sharingMessage);
        }
        [Update("folder/{folderId:int}/share")]
        public IEnumerable<FileShareWrapper> SetFolderSecurityInfo(int folderId, IEnumerable<FileShareParams> share, bool notify, string sharingMessage)
        {
            return FilesControllerHelperInt.SetFolderSecurityInfo(folderId, share, notify, sharingMessage);
        }

        /// <summary>
        ///   Removes sharing rights for the group with the ID specified in the request
        /// </summary>
        /// <param name="folderIds">Folders ID</param>
        /// <param name="fileIds">Files ID</param>
        /// <short>Remove group sharing rights</short>
        /// <category>Sharing</category>
        /// <returns>Shared file information</returns>
        [Delete("share")]
        public bool RemoveSecurityInfo(BaseBatchModel<int> model)
        {
            var itemList = new ItemList<string>();

            itemList.AddRange((model.FolderIds ?? new List<int>()).Select(x => "folder_" + x));
            itemList.AddRange((model.FileIds ?? new List<int>()).Select(x => "file_" + x));

            FileStorageService.RemoveAce(itemList);

            return true;
        }

        /// <summary>
        ///   Returns the external link to the shared file with the ID specified in the request
        /// </summary>
        /// <summary>
        ///   File external link
        /// </summary>
        /// <param name="fileId">File ID</param>
        /// <param name="share">Access right</param>
        /// <category>Files</category>
        /// <returns>Shared file link</returns>
        [Update("{fileId}/sharedlink", DisableFormat = true)]
        public string GenerateSharedLink(string fileId, FileShare share)
        {
            return FilesControllerHelperString.GenerateSharedLink(fileId, share);
        }

        [Update("{fileId:int}/sharedlink")]
        public string GenerateSharedLink(int fileId, FileShare share)
        {
            return FilesControllerHelperInt.GenerateSharedLink(fileId, share);
        }

        /// <summary>
        ///   Get a list of available providers
        /// </summary>
        /// <category>Third-Party Integration</category>
        /// <returns>List of provider key</returns>
        /// <remarks>List of provider key: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive</remarks>
        /// <returns></returns>
        [Read("thirdparty/capabilities")]
        public List<List<string>> Capabilities()
        {
            var result = new List<List<string>>();

            if (UserManager.GetUsers(SecurityContext.CurrentAccount.ID).IsVisitor(UserManager)
                || (!UserManager.IsUserInGroup(SecurityContext.CurrentAccount.ID, Constants.GroupAdmin.ID)
                    && !WebItemSecurity.IsProductAdministrator(ProductEntryPoint.ID, SecurityContext.CurrentAccount.ID)
                    && !FilesSettingsHelper.EnableThirdParty
                    && !CoreBaseSettings.Personal))
            {
                return result;
            }

            if (ThirdpartyConfiguration.SupportBoxInclusion)
            {
                result.Add(new List<string> { "Box", BoxLoginProvider.ClientID, BoxLoginProvider.RedirectUri });
            }
            if (ThirdpartyConfiguration.SupportDropboxInclusion)
            {
                result.Add(new List<string> { "DropboxV2", DropboxLoginProvider.ClientID, DropboxLoginProvider.RedirectUri });
            }
            if (ThirdpartyConfiguration.SupportGoogleDriveInclusion)
            {
                result.Add(new List<string> { "GoogleDrive", GoogleLoginProvider.ClientID, GoogleLoginProvider.RedirectUri });
            }
            if (ThirdpartyConfiguration.SupportOneDriveInclusion)
            {
                result.Add(new List<string> { "OneDrive", OneDriveLoginProvider.ClientID, OneDriveLoginProvider.RedirectUri });
            }
            if (ThirdpartyConfiguration.SupportSharePointInclusion)
            {
                result.Add(new List<string> { "SharePoint" });
            }
            if (ThirdpartyConfiguration.SupportYandexInclusion)
            {
                result.Add(new List<string> { "Yandex" });
            }
            if (ThirdpartyConfiguration.SupportWebDavInclusion)
            {
                result.Add(new List<string> { "WebDav" });
            }

            //Obsolete BoxNet, DropBox, Google, SkyDrive,

            return result;
        }

        /// <summary>
        ///   Saves the third party file storage service account
        /// </summary>
        /// <short>Save third party account</short>
        /// <param name="url">Connection url for SharePoint</param>
        /// <param name="login">Login</param>
        /// <param name="password">Password</param>
        /// <param name="token">Authentication token</param>
        /// <param name="isCorporate"></param>
        /// <param name="customerTitle">Title</param>
        /// <param name="providerKey">Provider Key</param>
        /// <param name="providerId">Provider ID</param>
        /// <category>Third-Party Integration</category>
        /// <returns>Folder contents</returns>
        /// <remarks>List of provider key: DropboxV2, Box, WebDav, Yandex, OneDrive, SharePoint, GoogleDrive</remarks>
        /// <exception cref="ArgumentException"></exception>
        [Create("thirdparty")]
        public FolderWrapper<string> SaveThirdParty(
            string url,
            string login,
            string password,
            string token,
            bool isCorporate,
            string customerTitle,
            string providerKey,
            string providerId)
        {
            var thirdPartyParams = new ThirdPartyParams
            {
                AuthData = new AuthData(url, login, password, token),
                Corporate = isCorporate,
                CustomerTitle = customerTitle,
                ProviderId = providerId,
                ProviderKey = providerKey,
            };

            var folder = FileStorageService.SaveThirdParty(thirdPartyParams);

            return FolderWrapperHelper.Get(folder);
        }

        /// <summary>
        ///    Returns the list of all connected third party services
        /// </summary>
        /// <category>Third-Party Integration</category>
        /// <short>Get third party list</short>
        /// <returns>Connected providers</returns>
        [Read("thirdparty")]
        public IEnumerable<ThirdPartyParams> GetThirdPartyAccounts()
        {
            return FileStorageService.GetThirdParty();
        }

        /// <summary>
        ///    Returns the list of third party services connected in the 'Common Documents' section
        /// </summary>
        /// <category>Third-Party Integration</category>
        /// <short>Get third party folder</short>
        /// <returns>Connected providers folder</returns>
        [Read("thirdparty/common")]
        public IEnumerable<Folder<string>> GetCommonThirdPartyFolders()
        {
            var parent = FileStorageServiceInt.GetFolder(GlobalFolderHelper.FolderCommon);
            return EntryManager.GetThirpartyFolders(parent);
        }

        /// <summary>
        ///   Removes the third party file storage service account with the ID specified in the request
        /// </summary>
        /// <param name="providerId">Provider ID. Provider id is part of folder id.
        /// Example, folder id is "sbox-123", then provider id is "123"
        /// </param>
        /// <short>Remove third party account</short>
        /// <category>Third-Party Integration</category>
        /// <returns>Folder id</returns>
        ///<exception cref="ArgumentException"></exception>
        [Delete("thirdparty/{providerId:int}")]
        public object DeleteThirdParty(int providerId)
        {
            return FileStorageService.DeleteThirdParty(providerId.ToString(CultureInfo.InvariantCulture));

        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="query"></param>
        ///// <returns></returns>
        //[Read(@"@search/{query}")]
        //public IEnumerable<FileEntryWrapper> Search(string query)
        //{
        //    var searcher = new SearchHandler();
        //    var files = searcher.SearchFiles(query).Select(r => (FileEntryWrapper)FileWrapperHelper.Get(r));
        //    var folders = searcher.SearchFolders(query).Select(f => (FileEntryWrapper)FolderWrapperHelper.Get(f));

        //    return files.Concat(folders);
        //}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        [Update(@"storeoriginal")]
        public bool StoreOriginal(bool set)
        {
            return FileStorageService.StoreOriginal(set);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="save"></param>
        /// <visible>false</visible>
        /// <returns></returns>
        [Update(@"hideconfirmconvert")]
        public bool HideConfirmConvert(bool save)
        {
            return FileStorageService.HideConfirmConvert(save);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="set"></param>
        /// <returns></returns>
        [Update(@"updateifexist")]
        public bool UpdateIfExist(bool set)
        {
            return FileStorageService.UpdateIfExist(set);
        }

        /// <summary>
        ///  Checking document service location
        /// </summary>
        /// <param name="docServiceUrl">Document editing service Domain</param>
        /// <param name="docServiceUrlInternal">Document command service Domain</param>
        /// <param name="docServiceUrlPortal">Community Server Address</param>
        /// <returns></returns>
        [Update("docservice")]
        public IEnumerable<string> CheckDocServiceUrl(string docServiceUrl, string docServiceUrlInternal, string docServiceUrlPortal)
        {
            FilesLinkUtility.DocServiceUrl = docServiceUrl;
            FilesLinkUtility.DocServiceUrlInternal = docServiceUrlInternal;
            FilesLinkUtility.DocServicePortalUrl = docServiceUrlPortal;

            MessageService.Send(MessageAction.DocumentServiceLocationSetting);

            var https = new Regex(@"^https://", RegexOptions.IgnoreCase);
            var http = new Regex(@"^http://", RegexOptions.IgnoreCase);
            if (https.IsMatch(CommonLinkUtility.GetFullAbsolutePath("")) && http.IsMatch(FilesLinkUtility.DocServiceUrl))
            {
                throw new Exception("Mixed Active Content is not allowed. HTTPS address for Document Server is required.");
            }

            DocumentServiceConnector.CheckDocServiceUrl();

            return new[]
                {
                    FilesLinkUtility.DocServiceUrl,
                    FilesLinkUtility.DocServiceUrlInternal,
                    FilesLinkUtility.DocServicePortalUrl
                };
        }

        /// <visible>false</visible>
        [Read("docservice")]
        public object GetDocServiceUrl(bool version)
        {
            var url = CommonLinkUtility.GetFullAbsolutePath(FilesLinkUtility.DocServiceApiUrl);
            if (!version)
            {
                return url;
            }

            var dsVersion = DocumentServiceConnector.GetVersion();

            return new
            {
                version = dsVersion,
                docServiceUrlApi = url,
            };
        }


        private FolderContentWrapper<string> ToFolderContentWrapper(string folderId, Guid userIdOrGroupId, FilterType filterType, bool withsubfolders)
        {
            if (!Enum.TryParse(ApiContext.SortBy, true, out SortedByType sortBy))
            {
                sortBy = SortedByType.AZ;
            }

            var startIndex = Convert.ToInt32(ApiContext.StartIndex);
            return FolderContentWrapperHelper.Get(FileStorageService.GetFolderItems(folderId.ToString(),
                                                                               startIndex,
                                                                               Convert.ToInt32(ApiContext.Count), //NOTE: last value: Convert.ToInt32(ApiContext.Count) - 1; in ApiContext +1
                                                                               filterType,
                                                                               filterType == FilterType.ByUser,
                                                                               userIdOrGroupId.ToString(),
                                                                               ApiContext.FilterValue,
                                                                               false,
                                                                               withsubfolders,
                                                                               new OrderBy(sortBy, !ApiContext.SortDescending)),
                                            startIndex);
        }

        private FolderContentWrapper<int> ToFolderContentWrapper(int folderId, Guid userIdOrGroupId, FilterType filterType, bool withsubfolders)
        {
            if (!Enum.TryParse(ApiContext.SortBy, true, out SortedByType sortBy))
            {
                sortBy = SortedByType.AZ;
            }

            var startIndex = Convert.ToInt32(ApiContext.StartIndex);
            var items = FileStorageServiceInt.GetFolderItems(
                folderId,
                startIndex,
                Convert.ToInt32(ApiContext.Count) - 1, //NOTE: in ApiContext +1
                filterType,
                filterType == FilterType.ByUser,
                userIdOrGroupId.ToString(),
                ApiContext.FilterValue,
                false,
                false,
                new OrderBy(sortBy, !ApiContext.SortDescending));

            return FolderContentWrapperHelper.Get(items, startIndex);
        }

        #region wordpress

        /// <visible>false</visible>
        [Read("wordpress-info")]
        public object GetWordpressInfo()
        {
            var token = WordpressToken.GetToken();
            if (token != null)
            {
                var meInfo = WordpressHelper.GetWordpressMeInfo(token.AccessToken);
                var blogId = JObject.Parse(meInfo).Value<string>("token_site_id");
                var wordpressUserName = JObject.Parse(meInfo).Value<string>("username");

                var blogInfo = RequestHelper.PerformRequest(WordpressLoginProvider.WordpressSites + blogId, "", "GET", "");
                var jsonBlogInfo = JObject.Parse(blogInfo);
                jsonBlogInfo.Add("username", wordpressUserName);

                blogInfo = jsonBlogInfo.ToString();
                return new
                {
                    success = true,
                    data = blogInfo
                };
            }
            return new
            {
                success = false
            };
        }

        /// <visible>false</visible>
        [Read("wordpress-delete")]
        public object DeleteWordpressInfo()
        {
            var token = WordpressToken.GetToken();
            if (token != null)
            {
                WordpressToken.DeleteToken(token);
                return new
                {
                    success = true
                };
            }
            return new
            {
                success = false
            };
        }

        /// <visible>false</visible>
        [Create("wordpress-save")]
        public object WordpressSave(string code)
        {
            if (code == "")
            {
                return new
                {
                    success = false
                };
            }
            try
            {
                var token = OAuth20TokenHelper.GetAccessToken<WordpressLoginProvider>(ConsumerFactory, code);
                WordpressToken.SaveToken(token);
                var meInfo = WordpressHelper.GetWordpressMeInfo(token.AccessToken);
                var blogId = JObject.Parse(meInfo).Value<string>("token_site_id");

                var wordpressUserName = JObject.Parse(meInfo).Value<string>("username");

                var blogInfo = RequestHelper.PerformRequest(WordpressLoginProvider.WordpressSites + blogId, "", "GET", "");
                var jsonBlogInfo = JObject.Parse(blogInfo);
                jsonBlogInfo.Add("username", wordpressUserName);

                blogInfo = jsonBlogInfo.ToString();
                return new
                {
                    success = true,
                    data = blogInfo
                };
            }
            catch (Exception)
            {
                return new
                {
                    success = false
                };
            }
        }

        /// <visible>false</visible>
        [Create("wordpress")]
        public bool CreateWordpressPost(string code, string title, string content, int status)
        {
            try
            {
                var token = WordpressToken.GetToken();
                var meInfo = WordpressHelper.GetWordpressMeInfo(token.AccessToken);
                var parser = JObject.Parse(meInfo);
                if (parser == null) return false;
                var blogId = parser.Value<string>("token_site_id");

                if (blogId != null)
                {
                    var createPost = WordpressHelper.CreateWordpressPost(title, content, status, blogId, token);
                    return createPost;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region easybib

        /// <visible>false</visible>
        [Read("easybib-citation-list")]
        public object GetEasybibCitationList(int source, string data)
        {
            try
            {
                var citationList = EasyBibHelper.GetEasyBibCitationsList(source, data);
                return new
                {
                    success = true,
                    citations = citationList
                };
            }
            catch (Exception)
            {
                return new
                {
                    success = false
                };
            }

        }

        /// <visible>false</visible>
        [Read("easybib-styles")]
        public object GetEasybibStyles()
        {
            try
            {
                var data = EasyBibHelper.GetEasyBibStyles();
                return new
                {
                    success = true,
                    styles = data
                };
            }
            catch (Exception)
            {
                return new
                {
                    success = false
                };
            }
        }

        /// <visible>false</visible>
        [Create("easybib-citation")]
        public object EasyBibCitationBook(string citationData)
        {
            try
            {
                var citat = EasyBibHelper.GetEasyBibCitation(citationData);
                if (citat != null)
                {
                    return new
                    {
                        success = true,
                        citation = citat
                    };
                }
                else
                {
                    return new
                    {
                        success = false
                    };
                }

            }
            catch (Exception)
            {
                return new
                {
                    success = false
                };
            }
        }

        #endregion

        /// <summary>
        /// Result of file conversation operation.
        /// </summary>
        [DataContract(Name = "operation_result", Namespace = "")]
        public class ConversationResult<T>
        {
            /// <summary>
            /// Operation Id.
            /// </summary>
            [DataMember(Name = "id")]
            public string Id { get; set; }

            /// <summary>
            /// Operation type.
            /// </summary>
            [DataMember(Name = "operation")]
            public FileOperationType OperationType { get; set; }

            /// <summary>
            /// Operation progress.
            /// </summary>
            [DataMember(Name = "progress")]
            public int Progress { get; set; }

            /// <summary>
            /// Source files for operation.
            /// </summary>
            [DataMember(Name = "source")]
            public string Source { get; set; }

            /// <summary>
            /// Result file of operation.
            /// </summary>
            [DataMember(Name = "result")]
            public FileWrapper<T> File { get; set; }

            /// <summary>
            /// Error during conversation.
            /// </summary>
            [DataMember(Name = "error")]
            public string Error { get; set; }

            /// <summary>
            /// Is operation processed.
            /// </summary>
            [DataMember(Name = "processed")]
            public string Processed { get; set; }
        }
    }

    public static class DocumentsControllerExtention
    {
        public static DIHelper AddDocumentsControllerService(this DIHelper services)
        {
            return services.AddFilesControllerHelperService();
        }
    }

    public class BodySpecificAttribute : Attribute, IActionConstraint
    {
        public BodySpecificAttribute()
        {
        }

        public int Order
        {
            get
            {
                return 0;
            }
        }

        public bool Accept(ActionConstraintContext context)
        {
            var options = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            };

            try
            {
                context.RouteContext.HttpContext.Request.EnableBuffering();
                _ = JsonSerializer.DeserializeAsync<BaseBatchModel<int>>(context.RouteContext.HttpContext.Request.Body, options).Result;
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                context.RouteContext.HttpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            }

        }
    }
}