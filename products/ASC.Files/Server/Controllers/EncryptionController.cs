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
using System.Linq;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Users;
using ASC.Files.Core.Data;
using ASC.Web.Api.Routing;
using ASC.Web.Files.Core.Entries;
using ASC.Web.Studio.Core;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using Newtonsoft.Json;

namespace ASC.Api.Documents
{
    [DefaultRoute]
    [ApiController]
    public class EncryptionController : ControllerBase
    {
        public PermissionContext PermissionContext { get; }
        public AuthContext AuthContext { get; }
        public EncryptionLoginProvider EncryptionLoginProvider { get; }
        public EncryptionAddressHelper EncryptionAddressHelper { get; }
        public EncryptedDataDao EncryptedDataDao { get; }
        public ILog Log { get; }

        public EncryptionController(
            PermissionContext permissionContext,
            AuthContext authContext,
            EncryptionLoginProvider encryptionLoginProvider,
            IOptionsMonitor<ILog> monitor,
            EncryptionAddressHelper encryptionAddressHelper,
            EncryptedDataDao encryptedDataDao)
        {
            PermissionContext = permissionContext;
            AuthContext = authContext;
            EncryptionLoginProvider = encryptionLoginProvider;
            EncryptionAddressHelper = encryptionAddressHelper;
            EncryptedDataDao = encryptedDataDao;
            Log = monitor.Get("ASC.Api.Documents");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <visible>false</visible>
        [Update("address")]
        public object UpdateAddress(string address, string publicKey)
        {
            PermissionContext.DemandPermissions(new UserSecurityProvider(AuthContext.CurrentAccount.ID), Constants.Action_EditUser);

            if (string.IsNullOrEmpty(address)) throw new ArgumentNullException("address");
            if (string.IsNullOrEmpty(publicKey)) throw new ArgumentNullException("publicKey");

            var currentAddressString = EncryptionLoginProvider.GetAddress();
            if (!string.IsNullOrEmpty(currentAddressString))
            {
                var currentAddress = JsonConvert.DeserializeObject<EncryptionAddress>(currentAddressString);
                if (currentAddress != null
                    && !string.IsNullOrEmpty(currentAddress.PublicKey)
                    && currentAddress.PublicKey.Equals(publicKey))
                {
                    return new { isset = true };
                }

                Log.InfoFormat("User {0} updates address", AuthContext.CurrentAccount.ID);
            }

            var account = new EncryptionAddress { Address = address, PublicKey = publicKey };
            var accountString = JsonConvert.SerializeObject(account);
            EncryptionLoginProvider.UpdateAddress(accountString);

            return new
            {
                isset = !string.IsNullOrEmpty(EncryptionLoginProvider.GetAddress())
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <visible>false</visible>
        [Read("access/{fileId}")]
        public IEnumerable<EncryptionAddress> GetAddressesWithAccess(string fileId)
        {
            var accountsString = EncryptionAddressHelper.GetAddresses(fileId);

            var accounts = accountsString.Select(JsonConvert.DeserializeObject<EncryptionAddress>);
            return accounts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="fileHash"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <visible>false</visible>
        [Create("store")]
        public bool PutEncryptedData(string publicKey, string fileHash, string data)
        {
            if (string.IsNullOrEmpty(publicKey))
                throw new ArgumentNullException("Public Key require", publicKey);

            if (string.IsNullOrEmpty(fileHash))
                throw new ArgumentNullException("File hash require", fileHash);

            if (string.IsNullOrEmpty(data))
                throw new ArgumentNullException("Encrypted file password require", data);

            if (!string.IsNullOrEmpty(GetEncryptedData(publicKey, fileHash)))
                throw new Exception("Encrypted file password already exist for this public key");

            var encryptedDao = EncryptedDataDao;
            var encryptedData = new EncryptedData
            {
                FileHash = fileHash,
                PublicKey = publicKey,
                Data = data
            };

            return encryptedDao.SaveEncryptedData(new[] { encryptedData });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="fileHash"></param>
        /// <returns></returns>
        /// <visible>false</visible>
        [Read("store")]
        public string GetEncryptedData(string publicKey, string fileHash)
        {
            var encryptedDao = EncryptedDataDao;
            return encryptedDao.GetData(publicKey, fileHash);
        }
    }
    public static class EncryptionControllerExtention
    {
        public static DIHelper AddEncryptionControllerService(this DIHelper services)
        {
            return services
                .AddPermissionContextService()
                .AddAuthContextService()
                .AddEncryptionLoginProviderService()
                .AddEncryptionAddressHelperService()
                .AddEncryptedDataDaoService();
        }
    }
}