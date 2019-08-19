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

using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.Notify.Recipients;

namespace ASC.Core.Notify
{
    public class RecipientProviderImpl : IRecipientProvider
    {
        public virtual IRecipient GetRecipient(int tenantId, string id)
        {
            Guid recID;
            if (TryParseGuid(id, out recID))
            {
                var user = CoreContext.UserManager.GetUsers(tenantId, recID);
                if (user.ID != Constants.LostUser.ID) return new DirectRecipient(user.ID.ToString(), user.ToString());

                var group = CoreContext.UserManager.GetGroupInfo(tenantId, recID);
                if (group.ID != Constants.LostGroupInfo.ID) return new RecipientsGroup(group.ID.ToString(), group.Name);
            }
            return null;
        }

        public virtual IRecipient[] GetGroupEntries(Tenant tenant, IRecipientsGroup group)
        {
            if (group == null) throw new ArgumentNullException("group");

            var result = new List<IRecipient>();
            var groupID = Guid.Empty;
            if (TryParseGuid(group.ID, out groupID))
            {
                var coreGroup = CoreContext.UserManager.GetGroupInfo(tenant.TenantId, groupID);
                if (coreGroup.ID != Constants.LostGroupInfo.ID)
                {
                    var users = CoreContext.UserManager.GetUsersByGroup(tenant, coreGroup.ID);
                    Array.ForEach(users, u => result.Add(new DirectRecipient(u.ID.ToString(), u.ToString())));
                }
            }
            return result.ToArray();
        }

        public virtual IRecipientsGroup[] GetGroups(Tenant tenant, IRecipient recipient)
        {
            if (recipient == null) throw new ArgumentNullException("recipient");

            var result = new List<IRecipientsGroup>();
            Guid recID;
            if (TryParseGuid(recipient.ID, out recID))
            {
                if (recipient is IRecipientsGroup)
                {
                    var group = CoreContext.UserManager.GetGroupInfo(tenant.TenantId, recID);
                    while (group != null && group.Parent != null)
                    {
                        result.Add(new RecipientsGroup(group.Parent.ID.ToString(), group.Parent.Name));
                        group = group.Parent;
                    }
                }
                else if (recipient is IDirectRecipient)
                {
                    foreach (var group in CoreContext.UserManager.GetUserGroups(tenant, recID, IncludeType.Distinct))
                    {
                        result.Add(new RecipientsGroup(group.ID.ToString(), group.Name));
                    }
                }
            }
            return result.ToArray();
        }

        public virtual string[] GetRecipientAddresses(int tenantId, IDirectRecipient recipient, string senderName)
        {
            if (recipient == null) throw new ArgumentNullException("recipient");

            if (TryParseGuid(recipient.ID, out var userID))
            {
                var user = CoreContext.UserManager.GetUsers(tenantId, userID);
                if (user.ID != Constants.LostUser.ID)
                {
                    if (senderName == ASC.Core.Configuration.Constants.NotifyEMailSenderSysName) return new[] { user.Email };
                    if (senderName == ASC.Core.Configuration.Constants.NotifyMessengerSenderSysName) return new[] { user.UserName };
                    if (senderName == ASC.Core.Configuration.Constants.NotifyPushSenderSysName) return new[] { user.UserName };
                }
            }
            return new string[0];
        }

        /// <summary>
        /// Check if user with this email is activated
        /// </summary>
        /// <param name="recipient"></param>
        /// <returns></returns>
        public IDirectRecipient FilterRecipientAddresses(int tenantId, IDirectRecipient recipient)
        {
            //Check activation
            if (recipient.CheckActivation)
            {
                //It's direct email
                if (recipient.Addresses != null && recipient.Addresses.Any())
                {
                    //Filtering only missing users and users who activated already
                    var filteredAddresses = from address in recipient.Addresses
                                            let user = CoreContext.UserManager.GetUserByEmail(tenantId, address)
                                            where user.ID == Constants.LostUser.ID || (user.IsActive && (user.Status & EmployeeStatus.Default) == user.Status)
                                            select address;

                    return new DirectRecipient(recipient.ID, recipient.Name, filteredAddresses.ToArray(), false);
                }
            }
            return recipient;
        }


        private bool TryParseGuid(string id, out Guid guid)
        {
            guid = Guid.Empty;
            if (!string.IsNullOrEmpty(id))
            {
                try
                {
                    guid = new Guid(id);
                    return true;
                }
                catch (FormatException) { }
                catch (OverflowException) { }
            }
            return false;
        }
    }
}
