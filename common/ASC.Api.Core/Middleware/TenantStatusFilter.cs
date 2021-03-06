﻿using System.Net;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Tenants;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ASC.Api.Core.Middleware
{
    public class TenantStatusFilter : IResourceFilter
    {
        private readonly ILog log;

        public TenantStatusFilter(IOptionsMonitor<ILog> options, TenantManager tenantManager)
        {
            log = options.CurrentValue;
            TenantManager = tenantManager;
        }

        public TenantManager TenantManager { get; }

        public void OnResourceExecuted(ResourceExecutedContext context)
        {
        }

        public void OnResourceExecuting(ResourceExecutingContext context)
        {
            var tenant = TenantManager.GetCurrentTenant(false);
            if (tenant == null)
            {
                context.Result = new StatusCodeResult((int)HttpStatusCode.NotFound);
                log.WarnFormat("Tenant {0} not found", tenant.TenantId);
                return;
            }

            if (tenant.Status == TenantStatus.RemovePending || tenant.Status == TenantStatus.Suspended)
            {
                context.Result = new StatusCodeResult((int)HttpStatusCode.NotFound);
                log.WarnFormat("Tenant {0} is not removed or suspended", tenant.TenantId);
                return;
            }
        }
    }

    public static class TenantStatusFilterExtension
    {
        public static DIHelper AddTenantStatusFilter(this DIHelper services)
        {
            return services
                .AddTenantManagerService();
        }
    }
}
