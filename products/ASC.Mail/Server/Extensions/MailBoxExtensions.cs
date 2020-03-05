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
using System.Security.Authentication;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Tenants;
using ASC.Core.Users;
using ASC.Mail.Models;
using ASC.Mail.Utils;

namespace ASC.Mail.Extensions
{
    public static class MailBoxExtensions
    {
        public static bool IsUserTerminated(this MailBoxData mailbox,
            TenantManager tenantManager, UserManager userManager)
        {
            try
            {
                tenantManager.SetCurrentTenant(mailbox.TenantId);

                var user = userManager.GetUsers(new Guid(mailbox.UserId));

                return user.Status == EmployeeStatus.Terminated;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsUserRemoved(this MailBoxData mailbox,
            TenantManager tenantManager, UserManager userManager)
        {
            try
            {
                tenantManager.SetCurrentTenant(mailbox.TenantId);

                if (!Guid.TryParse(mailbox.UserId, out Guid user))
                    return true;

                return !userManager.UserExists(user) || userManager.IsSystemUser(user);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static UserInfo GetUserInfo(this MailBoxData mailbox,
            TenantManager tenantManager, UserManager userManager)
        {
            try
            {
                tenantManager.SetCurrentTenant(mailbox.TenantId);
                var userInfo = userManager.GetUsers(new Guid(mailbox.UserId));

                return userInfo;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Defines.TariffType GetTenantStatus(this MailBoxData mailbox,
            TenantManager tenantManager, SecurityContext securityContext, ApiHelper apiHelper,
            int tenantOverdueDays, ILog log = null)
        {
            log = log ?? new NullLog();

            Defines.TariffType type;

            try
            {
                tenantManager.SetCurrentTenant(mailbox.TenantId);

                var tenantInfo = tenantManager.GetCurrentTenant();

                if (tenantInfo.Status == TenantStatus.RemovePending)
                    return Defines.TariffType.LongDead;

                try
                {
                    securityContext.AuthenticateMe(tenantInfo.OwnerId);
                }
                catch (InvalidCredentialException)
                {
                    securityContext.AuthenticateMe(new Guid(mailbox.UserId));
                }

                type = apiHelper.GetTenantTariff(tenantOverdueDays);
            }
            catch (Exception ex)
            {
                log.ErrorFormat("GetTenantStatus(Tenant={0}, User='{1}') Exception: {2}",
                    mailbox.TenantId, mailbox.UserId, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
                type = Defines.TariffType.Active;
            }

            return type;
        }

        //public static bool IsTenantQuotaEnded(this MailBoxData mailbox, long minBalance, ILog log = null)
        //{
        //    var quotaEnded = false;
        //    log = log ?? new NullLog();

        //    try
        //    {
        //        var quotaController = new TenantQuotaController(mailbox.TenantId);
        //        var quota = CoreContext.TenantManager.GetTenantQuota(mailbox.TenantId);
        //        var usedQuota = quotaController.QuotaCurrentGet();
        //        quotaEnded = quota.MaxTotalSize - usedQuota < minBalance;
        //        log.DebugFormat("IsTenantQuotaEnded: {0} Tenant = {1}. Tenant quota = {2}Mb ({3}), used quota = {4}Mb ({5}) ", 
        //            quotaEnded,
        //            mailbox.TenantId,
        //            MailUtil.BytesToMegabytes(quota.MaxTotalSize), quota.MaxTotalSize,
        //            MailUtil.BytesToMegabytes(usedQuota), usedQuota);
        //    }
        //    catch (Exception ex)
        //    {
        //        log.ErrorFormat("IsQuotaExhausted(Tenant={0}) Exception: {1}", mailbox.TenantId, ex.Message);
        //    }

        //    return quotaEnded;
        //}

        public static bool IsCrmAvailable(this MailBoxData mailbox,
            TenantManager tenantManager, SecurityContext securityContext, ApiHelper apiHelper,
            ILog log = null)
        {
            log = log ?? new NullLog();

            try
            {
                tenantManager.SetCurrentTenant(mailbox.TenantId);

                var tenantInfo = tenantManager.GetCurrentTenant();

                if (tenantInfo.Status == TenantStatus.RemovePending)
                    return false;

                securityContext.AuthenticateMe(new Guid(mailbox.UserId));

                return apiHelper.IsCrmModuleAvailable();
            }
            catch (Exception ex)
            {
                log.ErrorFormat("GetTenantStatus(Tenant={0}, User='{1}') Exception: {2}",
                    mailbox.TenantId, mailbox.UserId, ex.InnerException != null ? ex.InnerException.Message : ex.Message);
            }

            return true;
        }
    }
}
